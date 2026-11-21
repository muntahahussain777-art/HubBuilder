// =========================
// RealEstatebuilders.View.frmAllocatePlotFormView  (FINAL STABLE)
// =========================
using HubBuilders.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer; // resolve Timer ambiguity

namespace HubBuilders.View
{
    public partial class frmAllocatePlotFormView : SampleView
    {
        private const string T_ALLOC = "customer_plots";
        private const string CUST_MOBILE_COL = "mobile";

        private readonly Color VipStart = Color.FromArgb(255, 215, 0);
        private readonly Color VipEnd = Color.FromArgb(255, 170, 0);
        private readonly Color VipText = Color.White;

        public int CreatedAllocationId { get; private set; } = 0;

        // Allocation picker guards (Ctrl+A)
        private int _selectAllocOpenFlag = 0;
        private bool _selectAllocDismissed = false;

        // HARD FIX for Ctrl+A auto-reopen
        private int _lastOpenTick = 0;                 // Environment.TickCount (wrap-safe subtraction)
        private bool _suppressNextCtrlA = false;       // latch to ignore immediate re-fire after close
        private readonly WinFormsTimer _ctrlADebounceTimer; // short cooldown timer to clear latch
        private readonly WinFormsTimer _searchDebounceTimer;
        private bool _searchPending;

        // dgvedit / dgvdel double-open guards
        private int _editOpenFlag = 0; // Interlocked guard
        private int _lastEditTick = 0; // debounce (ms)
        private int _delOpenFlag = 0;
        private int _lastDelTick = 0;

        public frmAllocatePlotFormView()
        {
            InitializeComponent();

            // Timer setup (UI thread timer)
            _ctrlADebounceTimer = new WinFormsTimer();
            _ctrlADebounceTimer.Interval = 300; // 300 ms cooldown after dialog closes
            _ctrlADebounceTimer.Tick += (s, e) =>
            {
                _suppressNextCtrlA = false; // clear latch after short delay
                _ctrlADebounceTimer.Stop();
            };

            _searchDebounceTimer = new WinFormsTimer();
            _searchDebounceTimer.Interval = 250;
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                if (!_searchPending) return;
                _searchPending = false;
                try { GetData(); }
                catch (Exception ex) { ShowError("Search update failed.\n\n" + ex.Message); }
            };
        }

        private Label _lblAllocCount;
        private bool _allocGridTuned;
        private ComboBox _cmbSocietyFilter;
        private Label _lblSocietyFilter;
        private bool _societyFilterBusy;

        private void frmAllocatePlotFormView_Load(object sender, EventArgs e)
        {
            try
            {
                try { if (txtSearch != null) txtSearch.Text = ""; } catch { }
                EnsureSocietyFilter();
                EnsureAllocCountLabel();
                TuneAllocGridForSpeed();

                guna2DataGridView1.DataBindingComplete += (s, ev) =>
                {
                    try
                    {
                        EnsureRuntimeColumns();
                        ReorderActionColumns();
                        BumpCustomerFont();
                        UpdateAllocCountLabelFromGrid();
                    }
                    catch (Exception ex2) { ShowError("Post-bind failed.\n\n" + ex2.Message); }
                };

                guna2DataGridView1.CellPainting += Grid_CellPainting_VipWhatsApp;
                guna2DataGridView1.CellPainting += Grid_CellPainting_Cancel;
                guna2DataGridView1.CellPainting += Grid_CellPainting_Transfer;

                // Clicked-row id (CurrentRow Guna2 pe late/galat). Image=CellClick, Button=CellContentClick — dono, debounce guard double-fire rokta hai.
                guna2DataGridView1.CellClick -= guna2DataGridView1_CellClick;
                guna2DataGridView1.CellContentClick -= guna2DataGridView1_CellClick;
                guna2DataGridView1.CellClick += guna2DataGridView1_CellClick;
                guna2DataGridView1.CellContentClick += guna2DataGridView1_CellClick;

                // Pehle list dikhao (fast open) — schema/repair baad mein background
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        LoadSocietyFilterItems();
                        GetData();
                    }
                    catch (Exception ex) { ShowError("Form load failed.\n\n" + ex.Message); }

                    try
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                MainClass.EnsureAllocationSchema();
                                try { MainClass.RepairAllocatedPlotStatuses(); } catch { }
                            }
                            catch { }
                        });
                    }
                    catch
                    {
                        try
                        {
                            MainClass.EnsureAllocationSchema();
                            try { MainClass.RepairAllocatedPlotStatuses(); } catch { }
                        }
                        catch { }
                    }
                }));
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        /// <summary>No UI change — sirf paint/layout lighter taake slow PC pe hang na ho.</summary>
        private void TuneAllocGridForSpeed()
        {
            if (_allocGridTuned) return;
            try
            {
                var gv = guna2DataGridView1;
                if (gv == null) return;
                try
                {
                    typeof(System.Windows.Forms.Control)
                        .GetProperty("DoubleBuffered",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.SetValue(gv, true, null);
                }
                catch { }
                try { gv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; } catch { }
                try { gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; } catch { }
                _allocGridTuned = true;
            }
            catch { }
        }

        // ================== HARDENED HOTKEY HANDLER ==================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (keyData == (Keys.Control | Keys.F))
                {
                    txtSearch?.Focus();
                    txtSearch?.SelectAll();
                    return true;
                }

                if (keyData == (Keys.Control | Keys.A))
                {
                    // latch: if just closed dialog, ignore this repeat
                    if (_suppressNextCtrlA) return true;

                    // time debounce (wrap-safe)
                    int now = Environment.TickCount;
                    if (unchecked(now - _lastOpenTick) < 800) return true; // 800 ms
                    _lastOpenTick = now;

                    OpenSelectAllocation(); // guarded by Interlocked
                    return true;            // consume event
                }
            }
            catch (Exception ex)
            {
                ShowError("Hotkey handling failed.\n\n" + ex.Message);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ================== GUARDED PICKER OPEN ==================
        private void OpenSelectAllocation()
        {
            if (Interlocked.Exchange(ref _selectAllocOpenFlag, 1) == 1) return; // already opening/open
            try
            {
                if (!ShowAllocationPickerWithSearch(out int id, out string disp)) return;

                var nameOnly = disp.Split('|')[0].Trim(); // "Customer | Plot ..." → "Customer"
                txtSearch.Text = nameOnly;
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Allocation picker failed.\n\n" + ex.Message);
            }
            finally
            {
                // Set latch and short cooldown so Ctrl+A auto-repeat after close is suppressed
                _suppressNextCtrlA = true;
                _ctrlADebounceTimer.Stop();
                _ctrlADebounceTimer.Start();

                // Release Interlocked guard
                Interlocked.Exchange(ref _selectAllocOpenFlag, 0);
            }
        }

        /// <summary>Society filter near Search — existing UI same, sirf filter add.</summary>
        private void EnsureSocietyFilter()
        {
            try
            {
                if (_cmbSocietyFilter != null && !_cmbSocietyFilter.IsDisposed) return;
                var host = panel1 ?? (Control)this;
                int top = txtSearch != null ? txtSearch.Top : 34;
                int left = txtSearch != null ? txtSearch.Right + 12 : 250;

                _lblSocietyFilter = new Label
                {
                    AutoSize = true,
                    Text = "Society",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    BackColor = Color.Transparent,
                    Location = new Point(left, Math.Max(6, top - 22)),
                    Name = "lblSocietyFilter"
                };
                _cmbSocietyFilter = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    Location = new Point(left, top),
                    Size = new Size(220, 28),
                    Name = "cmbSocietyFilter",
                    FlatStyle = FlatStyle.Flat
                };
                try
                {
                    _cmbSocietyFilter.BackColor = Color.FromArgb(37, 41, 74);
                    _cmbSocietyFilter.ForeColor = Color.White;
                }
                catch { }

                _cmbSocietyFilter.SelectedIndexChanged += (s, e) =>
                {
                    if (_societyFilterBusy) return;
                    try { GetData(); }
                    catch (Exception ex) { ShowError("Society filter failed.\n\n" + ex.Message); }
                };

                host.Controls.Add(_lblSocietyFilter);
                host.Controls.Add(_cmbSocietyFilter);
                _cmbSocietyFilter.BringToFront();
                _lblSocietyFilter.BringToFront();
            }
            catch { }
        }

        private void LoadSocietyFilterItems()
        {
            try
            {
                EnsureSocietyFilter();
                if (_cmbSocietyFilter == null) return;
                _societyFilterBusy = true;
                int keepId = GetSelectedSocietyFilterId();
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Rows.Add(0, "All Societies");
                var src = MainClass.ExecuteSelectQuery(
                    "SELECT id, name FROM societies ORDER BY name COLLATE NOCASE;", null);
                if (src != null)
                {
                    foreach (DataRow r in src.Rows)
                    {
                        int id = 0;
                        try { id = Convert.ToInt32(r["id"]); } catch { continue; }
                        string name = Convert.ToString(r["name"] ?? "");
                        if (string.IsNullOrWhiteSpace(name)) name = "Society #" + id;
                        dt.Rows.Add(id, name);
                    }
                }
                _cmbSocietyFilter.DisplayMember = "name";
                _cmbSocietyFilter.ValueMember = "id";
                _cmbSocietyFilter.DataSource = dt;
                try { _cmbSocietyFilter.SelectedValue = keepId; }
                catch { _cmbSocietyFilter.SelectedIndex = 0; }
                if (_cmbSocietyFilter.SelectedIndex < 0 && _cmbSocietyFilter.Items.Count > 0)
                    _cmbSocietyFilter.SelectedIndex = 0;
            }
            catch { }
            finally { _societyFilterBusy = false; }
        }

        private int GetSelectedSocietyFilterId()
        {
            try
            {
                if (_cmbSocietyFilter == null || _cmbSocietyFilter.SelectedValue == null) return 0;
                return Convert.ToInt32(_cmbSocietyFilter.SelectedValue);
            }
            catch { return 0; }
        }

        // ------------------ DATA LOAD ------------------
        public void GetData()
        {
            try
            {
                // Schema/repair sirf Load pe — har refresh pe UPDATE hang avoid
                string search = (txtSearch?.Text ?? "").Trim();
                int societyId = GetSelectedSocietyFilterId();

                // Society filter + search (All Societies = 0)
                string qry = $@"
SELECT 
    a.id                AS id,
    c.name              AS Customer,
    c.{CUST_MOBILE_COL} AS Phone,
    s.name              AS Society,
    b.name              AS Block,
    pt.code             AS Type,
    p.plot_no           AS PlotNo,
    a.price             AS Price,
    a.booking_date      AS BookingDate,
    COALESCE(NULLIF(TRIM(a.deal_type),''),'Installment') AS DealType,
    a.status            AS Status
FROM {T_ALLOC} a
JOIN customers   c  ON c.id  = a.customer_id
JOIN plots       p  ON p.id  = a.plot_id
JOIN societies   s  ON s.id  = p.society_id
JOIN blocks      b  ON b.id  = p.block_id
JOIN plot_types  pt ON pt.id = p.plot_type_id
WHERE IFNULL(a.status,'') <> 'Cancelled'
  AND (@sid = 0 OR s.id = @sid)
  AND (
    COALESCE(@q,'') = ''
    OR LOWER(c.name) LIKE '%' || LOWER(@q) || '%'
    OR c.{CUST_MOBILE_COL} LIKE '%' || @q || '%'
    OR LOWER(p.plot_no) LIKE '%' || LOWER(@q) || '%'
    OR LOWER(s.name) LIKE '%' || LOWER(@q) || '%'
    OR LOWER(b.name) LIKE '%' || LOWER(@q) || '%'
    OR LOWER(COALESCE(a.deal_type,'')) LIKE '%' || LOWER(@q) || '%'
  )
ORDER BY a.id DESC;";

                EnsureRuntimeColumns();

                ListBox lb = new ListBox();
                lb.Items.Add(dgvid);
                lb.Items.Add(dgvCustomer);
                lb.Items.Add(dgvPhone);
                lb.Items.Add(dgvSociety);
                lb.Items.Add(dgvBlock);
                lb.Items.Add(dgvType);
                lb.Items.Add(dgvPlotNo);
                lb.Items.Add(dgvPrice);
                lb.Items.Add(dgvDate);
                if (guna2DataGridView1.Columns.Contains("dgvDealType"))
                    lb.Items.Add(guna2DataGridView1.Columns["dgvDealType"]);
                lb.Items.Add(dgvStatus);

                var prms = new Dictionary<string, object>
                {
                    { "@q", search },
                    { "@sid", societyId }
                };
                var gv = guna2DataGridView1;
                try { gv?.SuspendLayout(); } catch { }
                try
                {
                    MainClass.LoadData1(qry, gv, lb, prms);
                }
                finally
                {
                    try { gv?.ResumeLayout(); } catch { }
                }
                // columns/count: DataBindingComplete pe ek dafa
            }
            catch (Exception ex)
            {
                ShowError("Fetching allocations failed.\n\n" + ex.Message);
            }
        }

        private void EnsureAllocCountLabel()
        {
            if (_lblAllocCount != null && !_lblAllocCount.IsDisposed) return;
            _lblAllocCount = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(
                    Math.Max(20, (_cmbSocietyFilter != null ? _cmbSocietyFilter.Right : (txtSearch?.Right ?? 400)) + 16),
                    Math.Max(12, (txtSearch?.Top ?? 20))),
                Name = "lblAllocCount",
                Text = "Kul: —"
            };
            Controls.Add(_lblAllocCount);
            _lblAllocCount.BringToFront();
        }

        /// <summary>Report jaisa clear count — Installment + Reserve + Sold = Kul (Allocate) + Total Amount.</summary>
        private void UpdateAllocCountLabel()
        {
            UpdateAllocCountLabelFromGrid();
        }

        /// <summary>Grid se count — extra SQL nahi (slow PC pe open tez).</summary>
        private void UpdateAllocCountLabelFromGrid()
        {
            try
            {
                EnsureAllocCountLabel();
                int installment = 0, reserve = 0, sold = 0, kul = 0;
                decimal totalAmount = 0m;
                var gv = guna2DataGridView1;
                if (gv != null)
                {
                    bool hasDeal = gv.Columns.Contains("dgvDealType");
                    bool hasPrice = gv.Columns.Contains("dgvPrice");
                    foreach (DataGridViewRow row in gv.Rows)
                    {
                        if (row.IsNewRow) continue;
                        kul++;
                        string deal = hasDeal ? Convert.ToString(row.Cells["dgvDealType"]?.Value) ?? "" : "";
                        deal = deal.Trim().ToLowerInvariant();
                        if (deal == "sold") sold++;
                        else if (deal == "reserve") reserve++;
                        else installment++;
                        if (hasPrice)
                        {
                            try { totalAmount += Convert.ToDecimal(row.Cells["dgvPrice"]?.Value ?? 0); }
                            catch { }
                        }
                    }
                }
                _lblAllocCount.Text =
                    "Kul " + kul +
                    "  =  Installment " + installment +
                    "  +  Reserve " + reserve +
                    "  +  Sold " + sold +
                    "   |   List " + kul +
                    "   |   Total Amount: " + totalAmount.ToString("N2");
                if (!string.IsNullOrWhiteSpace(txtSearch?.Text))
                    _lblAllocCount.Text += "  (search filter on — clear search for full Kul)";
            }
            catch { }
        }

        // ------------------ Toolbar ------------------
        public override void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (var add = new frmAllocatePlotFormAdd())
                {
                    MainClass.BlurBackground(add); // modal
                    CreatedAllocationId = add.CreatedAllocationId;
                }

                GetData();

                int newId = CreatedAllocationId;
                if (newId > 0)
                {
                    var dt = MainClass.ExecuteSelectQuery(@"
SELECT c.name AS Customer, c.{CUST_MOBILE_COL} AS Phone, s.name AS Society, b.name AS Block,
       pt.code AS Type, p.plot_no AS PlotNo, a.price, a.booking_date, a.status
FROM customer_plots a
JOIN customers c ON c.id=a.customer_id
JOIN plots p ON p.id=a.plot_id
JOIN societies s ON s.id=p.society_id
JOIN blocks b ON b.id=p.block_id
JOIN plot_types pt ON pt.id=p.plot_type_id
WHERE a.id=@id;",
                        new Hashtable { { "@id", newId } });

                    if (dt != null && dt.Rows.Count == 1)
                    {
                        var r = dt.Rows[0];
                        string digits = NormalizePhoneToDigits(Convert.ToString(r["Phone"]));
                        if (!string.IsNullOrWhiteSpace(digits))
                        {
                            string msg = BuildWhatsAppMessage(
                                Convert.ToString(r["Customer"]),
                                Convert.ToString(r["Society"]),
                                Convert.ToString(r["Block"]),
                                Convert.ToString(r["Type"]),
                                Convert.ToString(r["PlotNo"]),
                                Convert.ToString(r["price"]),
                                Convert.ToString(r["booking_date"]),
                                Convert.ToString(r["status"])
                            );
                            OpenWhatsAppWithPrefill(digits, msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Open add dialog failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _searchPending = true;
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
            catch (Exception ex) { ShowError("Search update failed.\n\n" + ex.Message); }
        }

        // ------------------ GRID CLICK ------------------
        private void guna2DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var colName = guna2DataGridView1.Columns[e.ColumnIndex].Name;

                // ===== dgvedit: single-open hardening =====
                if (colName == "dgvedit")
                {
                    // debounce: accidental double-fire / mouse auto-repeat
                    int now = Environment.TickCount;
                    if (unchecked(now - _lastEditTick) < 600) return; // 600 ms
                    _lastEditTick = now;

                    // re-entrancy guard
                    if (Interlocked.Exchange(ref _editOpenFlag, 1) == 1) return;

                    try
                    {
                        if (!TryGetIdFromRow(e.RowIndex, out int allocId))
                        {
                            ShowError("Invalid selection. ID missing.");
                            return;
                        }

                        // optional: block further clicks while dialog open
                        bool prevEnabled = guna2DataGridView1.Enabled;
                        guna2DataGridView1.Enabled = false;
                        try
                        {
                            EditAllocation(allocId); // modal
                        }
                        finally
                        {
                            guna2DataGridView1.Enabled = prevEnabled;
                        }
                    }
                    finally
                    {
                        ReleaseGridActionGuardSoon(() =>
                        {
                            _lastEditTick = Environment.TickCount;
                            Interlocked.Exchange(ref _editOpenFlag, 0);
                        });
                    }
                    return;
                }

                // ===== dgvdel =====
                if (colName == "dgvdel")
                {
                    int nowDel = Environment.TickCount;
                    if (unchecked(nowDel - _lastDelTick) < 600) return;
                    _lastDelTick = nowDel;
                    if (Interlocked.Exchange(ref _delOpenFlag, 1) == 1) return;

                    try
                    {
                        if (!TryGetIdFromRow(e.RowIndex, out int allocId))
                        {
                            ShowError("Invalid selection. ID missing.");
                            return;
                        }

                        var dt = MainClass.ExecuteSelectQuery(
                            $"SELECT plot_id, status FROM {T_ALLOC} WHERE id=@id;",
                            new Hashtable { { "@id", allocId } });

                        if (dt == null || dt.Rows.Count == 0)
                        {
                            ShowError("Allocation record nahi mila.");
                            return;
                        }

                        int plotId = Convert.ToInt32(dt.Rows[0]["plot_id"]);
                        string aStatus = Convert.ToString(dt.Rows[0]["status"]);

                        // Completed wipe receipts + leave Sold plot — block (cancel path jaisa)
                        if (string.Equals(aStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowInfo("Completed allocation delete nahi ho sakti.");
                            return;
                        }

                        if (!AskYesNo("Delete this allocation?", "Delete Confirmation")) return;

                        try
                        {
                            var cascade = HubSyncService.CollectAllocationCascade(allocId);

                            MainClass.AcquireSharedConnection();
                            SQLiteTransaction tx = null;
                            try
                            {
                                tx = MainClass.con.BeginTransaction();
                                // pending_delete + local deletes EK transaction; SoftMark HTTP bahar
                                HubSyncService.QueuePendingDeletesForCascade(MainClass.con, tx, cascade);

                                using (var cmd = new SQLiteCommand(
                                    "DELETE FROM partner_money WHERE customer_plot_id=@id;", MainClass.con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", allocId);
                                    try { cmd.ExecuteNonQuery(); } catch { }
                                }
                                using (var cmd = new SQLiteCommand(
                                    "DELETE FROM receipts WHERE customer_plot_id=@id;", MainClass.con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", allocId);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand(
                                    "DELETE FROM ledger WHERE customer_plot_id=@id;", MainClass.con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", allocId);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand(
                                    "DELETE FROM customer_plot_partners WHERE customer_plot_id=@id;", MainClass.con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", allocId);
                                    try { cmd.ExecuteNonQuery(); } catch { }
                                }

                                int del;
                                using (var cmd = new SQLiteCommand(
                                    $"DELETE FROM {T_ALLOC} WHERE id=@id;", MainClass.con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", allocId);
                                    del = cmd.ExecuteNonQuery();
                                }

                                if (del <= 0)
                                {
                                    tx.Rollback();
                                    tx = null;
                                    ShowError("Delete failed ya record missing.");
                                    return;
                                }

                                if (string.Equals(aStatus, "Active", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Hold ya Sold (deal_type Sold) — edit/cancel jaisa free
                                    using (var cmd = new SQLiteCommand(
                                        "UPDATE plots SET status='Available' WHERE id=@pid AND status IN ('Hold','Sold');",
                                        MainClass.con, tx))
                                    {
                                        cmd.Parameters.AddWithValue("@pid", plotId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                tx.Commit();
                                tx = null;
                                try { HubSyncService.SoftMarkQueuedCascade(cascade); } catch { }
                                try
                                {
                                    if (string.Equals(aStatus, "Active", StringComparison.OrdinalIgnoreCase))
                                        HubSyncService.NotifyLocalWrite("plots", plotId);
                                    else
                                        HubSyncService.NotifyLocalWrite();
                                }
                                catch { try { HubSyncService.NotifyLocalWrite(); } catch { } }
                                ShowInfo("Allocation delete ho gaya.");
                                GetData();
                            }
                            catch (Exception exDel)
                            {
                                try { tx?.Rollback(); } catch { }
                                ShowError("Delete failed.\n\n" + exDel.Message);
                            }
                            finally
                            {
                                MainClass.ReleaseSharedConnection();
                            }
                        }
                        catch (Exception exDel)
                        {
                            ShowError("Delete failed.\n\n" + exDel.Message);
                        }
                    }
                    finally
                    {
                        ReleaseGridActionGuardSoon(() =>
                        {
                            _lastDelTick = Environment.TickCount;
                            Interlocked.Exchange(ref _delOpenFlag, 0);
                        });
                    }
                    return;
                }
                // ===== dgvcancel =====
                if (colName == "dgvcancel")
                {
                    if (!TryGetIdFromRow(e.RowIndex, out int allocId))
                    {
                        ShowError("Invalid selection. ID missing.");
                        return;
                    }

                    var dt = MainClass.ExecuteSelectQuery(
                        $"SELECT status FROM {T_ALLOC} WHERE id=@id;",
                        new Hashtable { { "@id", allocId } });

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        ShowError("Allocation record nahi mila.");
                        return;
                    }

                    string aStatus = Convert.ToString(dt.Rows[0]["status"]);

                    if (string.Equals(aStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowInfo("Ye allocation pehle hi cancelled hai.");
                        return;
                    }

                    if (string.Equals(aStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowInfo("Completed allocation cancel nahi ho sakti.");
                        return;
                    }

                    bool prevEnabled = guna2DataGridView1.Enabled;
                    guna2DataGridView1.Enabled = false;

                    try
                    {
                        using (var dlg = new frmAllocationCancelAdd())
                        {
                            dlg.CustomerPlotId = allocId;

                            var res = dlg.ShowDialog(GetDialogOwner());
                            if (res == DialogResult.OK)
                                GetData();
                        }
                    }
                    catch (Exception exCancel)
                    {
                        ShowError("Cancel dialog failed.\n\n" + exCancel.Message);
                    }
                    finally
                    {
                        guna2DataGridView1.Enabled = prevEnabled;
                    }

                    return;
                }

                // ===== dgvtransfer — owner change (same alloc sync_id; optional plot/pay) =====
                if (colName == "dgvtransfer")
                {
                    int nowTr = Environment.TickCount;
                    if (unchecked(nowTr - _lastEditTick) < 600) return;
                    _lastEditTick = nowTr;
                    if (Interlocked.Exchange(ref _editOpenFlag, 1) == 1) return;

                    try
                    {
                        if (!TryGetIdFromRow(e.RowIndex, out int allocId))
                        {
                            ShowError("Invalid selection. ID missing.");
                            return;
                        }

                        var dt = MainClass.ExecuteSelectQuery(
                            $"SELECT status FROM {T_ALLOC} WHERE id=@id;",
                            new Hashtable { { "@id", allocId } });

                        if (dt == null || dt.Rows.Count == 0)
                        {
                            ShowError("Allocation record nahi mila.");
                            return;
                        }

                        string aStatus = Convert.ToString(dt.Rows[0]["status"]);

                        if (!string.Equals(aStatus, "Active", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowInfo("Sirf Active allocation ka plot transfer ho sakta hai.");
                            return;
                        }

                        bool prevEnabled = guna2DataGridView1.Enabled;
                        guna2DataGridView1.Enabled = false;
                        try
                        {
                            TransferPlot(allocId);
                        }
                        finally
                        {
                            guna2DataGridView1.Enabled = prevEnabled;
                        }
                    }
                    finally
                    {
                        ReleaseGridActionGuardSoon(() =>
                        {
                            _lastEditTick = Environment.TickCount;
                            Interlocked.Exchange(ref _editOpenFlag, 0);
                        });
                    }
                    return;
                }

                // ===== dgvwa =====
                if (colName == "dgvwa")
                {
                    var row = guna2DataGridView1.Rows[e.RowIndex];

                    string name = row.Cells["dgvCustomer"].Value?.ToString()?.Trim() ?? "";
                    string phone = row.Cells["dgvPhone"].Value?.ToString()?.Trim() ?? "";
                    string soc = row.Cells["dgvSociety"].Value?.ToString() ?? "";
                    string block = row.Cells["dgvBlock"].Value?.ToString() ?? "";
                    string ptype = row.Cells["dgvType"].Value?.ToString() ?? "";
                    string plot = row.Cells["dgvPlotNo"].Value?.ToString() ?? "";
                    string price = row.Cells["dgvPrice"].Value?.ToString() ?? "";
                    string date = row.Cells["dgvDate"].Value?.ToString() ?? "";
                    string status = row.Cells["dgvStatus"].Value?.ToString() ?? "";

                    string digits = NormalizePhoneToDigits(phone);
                    if (string.IsNullOrWhiteSpace(digits))
                    {
                        ShowInfo("Customer mobile invalid hai (country code ke sath, digits-only).");
                        return;
                    }

                    string msg = BuildWhatsAppMessage(name, soc, block, ptype, plot, price, date, status);
                    OpenWhatsAppWithPrefill(digits, msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowError("Grid operation failed.\n\n" + ex.Message);
            }
        }

        private void ReleaseGridActionGuardSoon(Action release)
        {
            void doRelease()
            {
                try { release?.Invoke(); } catch { }
            }

            try
            {
                if (!IsHandleCreated)
                {
                    doRelease();
                    return;
                }

                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        var t = new WinFormsTimer { Interval = 350 };
                        t.Tick += (s, ev) =>
                        {
                            try { t.Stop(); t.Dispose(); } catch { }
                            doRelease();
                        };
                        t.Start();
                    }
                    catch
                    {
                        doRelease();
                    }
                }));
            }
            catch
            {
                doRelease();
            }
        }

        // ------------------ EDIT ------------------
        private void EditAllocation(int allocId)
        {
            try
            {
                var dt = MainClass.ExecuteSelectQuery(@"
SELECT a.id, a.customer_id, a.plot_id, a.price, a.booking_date, a.notes, a.status,
       a.broker_id, a.broker_commission,
       COALESCE(a.monthly_installment, 0) AS monthly_installment,
       COALESCE(NULLIF(TRIM(a.deal_type),''),'Installment') AS deal_type,
       p.society_id, p.block_id, p.plot_type_id
FROM customer_plots a
JOIN plots p ON p.id = a.plot_id
WHERE a.id=@id;",
                    new Hashtable { { "@id", allocId } });

                if (dt == null || dt.Rows.Count != 1)
                {
                    ShowError("Allocation record nahi mila.");
                    return;
                }

                var r = dt.Rows[0];
                if (!TryToInt(r["customer_id"], out int curCustomerId)
                    || !TryToInt(r["plot_id"], out int curPlotId)
                    || !TryToInt(r["society_id"], out int curSocId)
                    || !TryToInt(r["block_id"], out int curBlockId)
                    || !TryToInt(r["plot_type_id"], out int curTypeId))
                {
                    ShowError("Allocation data incomplete / invalid.");
                    return;
                }
                string curStatus = Convert.ToString(r["status"]);
                decimal curPrice = 0m;
                try { if (r["price"] != DBNull.Value) curPrice = Convert.ToDecimal(r["price"]); } catch { }
                DateTime curDate = DateTime.TryParse(Convert.ToString(r["booking_date"]), out var d) ? d : DateTime.Today;
                string curNotes = Convert.ToString(r["notes"]);
                int curBrokerId = 0;
                TryToInt(r["broker_id"], out curBrokerId);
                decimal curBrokerComm = 0m;
                try { if (r["broker_commission"] != DBNull.Value) curBrokerComm = Convert.ToDecimal(r["broker_commission"]); } catch { }
                decimal curMonthly = 0m;
                try { if (r.Table.Columns.Contains("monthly_installment") && r["monthly_installment"] != DBNull.Value) curMonthly = Convert.ToDecimal(r["monthly_installment"]); } catch { }
                string curDealType = "Installment";
                try
                {
                    string dtRaw = Convert.ToString(r["deal_type"] ?? "").Trim();
                    if (string.Equals(dtRaw, "Reserve", StringComparison.OrdinalIgnoreCase)) curDealType = "Reserve";
                    else if (string.Equals(dtRaw, "Sold", StringComparison.OrdinalIgnoreCase)) curDealType = "Sold";
                    else curDealType = "Installment";
                }
                catch { }
                string curBpKey = curBrokerId > 0 ? ("B:" + curBrokerId) : null;
                if (string.IsNullOrEmpty(curBpKey))
                {
                    var pid = MainClass.GetAllocationPartnerId(allocId);
                    if (pid.HasValue && pid.Value > 0) curBpKey = "P:" + pid.Value;
                }

                Form dlg = BuildEditDialog(
                    out ComboBox cbCustomer, out ComboBox cbSoc, out ComboBox cbBlock, out ComboBox cbType, out ComboBox cbPlot,
                    out NumericUpDown numPrice, out DateTimePicker dtpBook, out TextBox txtNotes, out ComboBox cbStatus,
                    out ComboBox cbBroker, out NumericUpDown numBrokerComm, out ComboBox cbDealType,
                    out NumericUpDown numMonthly);

                LoadCustomers(cbCustomer, curCustomerId);
                LoadSocieties(cbSoc, curSocId);
                LoadBlocks(cbBlock, curSocId, curBlockId);
                LoadPlotTypes(cbType, curSocId, curTypeId);
                LoadPlots(cbPlot, curSocId, curBlockId, curTypeId, curPlotId);
                LoadBrokers(cbBroker, curBpKey);

                cbSoc.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid))
                    {
                        LoadBlocks(cbBlock, sid, 0);
                        LoadPlotTypes(cbType, sid, 0);
                        cbPlot.DataSource = null;
                    }
                };
                cbBlock.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid) && TryGetId(cbBlock, out int bid) && TryGetId(cbType, out int ptid))
                        LoadPlots(cbPlot, sid, bid, ptid, curPlotId);
                };
                cbType.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid) && TryGetId(cbBlock, out int bid) && TryGetId(cbType, out int ptid))
                        LoadPlots(cbPlot, sid, bid, ptid, curPlotId);
                };

                numPrice.Value = curPrice > numPrice.Maximum ? numPrice.Maximum : curPrice;
                dtpBook.Value = curDate;
                txtNotes.Text = curNotes;
                cbStatus.Items.Clear();
                cbStatus.Items.AddRange(new object[] { "Active", "Cancelled", "Completed" });
                cbStatus.SelectedItem = curStatus;

                numBrokerComm.Value = (curBrokerComm < numBrokerComm.Minimum) ? numBrokerComm.Minimum :
                                      (curBrokerComm > numBrokerComm.Maximum) ? numBrokerComm.Maximum :
                                      curBrokerComm;

                try { cbDealType.SelectedItem = curDealType; } catch { cbDealType.SelectedIndex = 0; }
                numMonthly.Value = (curMonthly < numMonthly.Minimum) ? numMonthly.Minimum :
                                   (curMonthly > numMonthly.Maximum) ? numMonthly.Maximum : curMonthly;

                if (dlg.ShowDialog(GetDialogOwner()) != DialogResult.OK) { dlg.Dispose(); return; }

                if (!TryGetId(cbCustomer, out int newCustId)) { ShowInfo("Customer required."); return; }
                if (!TryGetId(cbSoc, out int newSid)) { ShowInfo("Society required."); return; }
                if (!TryGetId(cbBlock, out int newBid)) { ShowInfo("Block required."); return; }
                if (!TryGetId(cbType, out int newPtid)) { ShowInfo("Plot Type required."); return; }
                if (!TryGetId(cbPlot, out int newPlotId)) { ShowInfo("Plot required."); return; }

                // Broker / price / commission optional (0 allowed) — old backup compatible
                string bpKind = "";
                int bpId = 0;
                MainClass.TryParseBrokerPartnerKey(cbBroker.SelectedValue, out bpKind, out bpId);
                int newBrokerId = (bpKind == "B" && bpId > 0) ? bpId : 0;
                decimal newBrokerComm = numBrokerComm.Value;
                if (newBrokerComm < 0) newBrokerComm = 0;

                string newStatus = Convert.ToString(cbStatus.SelectedItem ?? curStatus);
                decimal newPrice = numPrice.Value;
                if (newPrice < 0) newPrice = 0;
                string newDate = dtpBook.Value.ToString("yyyy-MM-dd");
                string newNotes = (txtNotes.Text ?? "").Trim();
                string newDealType = "Installment";
                try
                {
                    string s = Convert.ToString(cbDealType.SelectedItem ?? "").Trim();
                    if (string.Equals(s, "Reserve", StringComparison.OrdinalIgnoreCase)) newDealType = "Reserve";
                    else if (string.Equals(s, "Sold", StringComparison.OrdinalIgnoreCase)) newDealType = "Sold";
                }
                catch { }

                if (string.Equals(curStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
                    (newStatus != "Completed" || newPlotId != curPlotId))
                {
                    ShowError("Completed allocation ka plot/status change allow nahi hai.");
                    return;
                }

                string err = UpdateAllocationWithStatuses(
                    allocId, curPlotId, newPlotId,
                    curStatus, newStatus,
                    newCustId, newPrice, newDate, newNotes,
                    newBrokerId, newBrokerComm, newDealType, numMonthly.Value);

                if (err == null)
                {
                    try
                    {
                        if (bpKind == "P" && bpId > 0)
                        {
                            MainClass.SyncAllocationPartnerCommission(allocId, bpId, newBrokerComm, newDate);
                        }
                        else
                        {
                            // No partner: clear any previous partner-commission entry
                            MainClass.SyncAllocationPartnerCommission(allocId, 0, 0, newDate);
                        }
                    }
                    catch { }
                    try
                    {
                        var syncRows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>();
                        syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("customer_plots", allocId));
                        if (newPlotId > 0)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("plots", newPlotId));
                        if (curPlotId > 0 && curPlotId != newPlotId)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("plots", curPlotId));
                        HubSyncService.NotifyLocalWrites(syncRows);
                    }
                    catch { try { HubSyncService.NotifyLocalWrite(); } catch { } }
                    ShowInfo("Allocation update ho gaya.");
                    GetData();
                }
                else ShowError(err);

                dlg.Dispose();
            }
            catch (Exception ex)
            {
                ShowError("Edit allocation failed.\n\n" + ex.Message);
            }
        }

        // ------------------ TRANSFER OWNER (same alloc id/sync_id; receipts FK stay) ------------------
        private void TransferPlot(int allocId)
        {
            try
            {
                MainClass.EnsureAllocationSchema();

                var dt = MainClass.ExecuteSelectQuery(@"
SELECT a.id, a.customer_id, a.plot_id, a.price, a.notes, a.status,
       p.society_id, p.block_id, p.plot_type_id, p.plot_no AS old_plot_no,
       c.name AS customer_name,
       (SELECT COUNT(1) FROM receipts r WHERE r.customer_plot_id=a.id) AS receipt_count,
       (SELECT COALESCE(SUM(amount),0) FROM receipts r WHERE r.customer_plot_id=a.id) AS receipt_total
FROM customer_plots a
JOIN plots p ON p.id = a.plot_id
JOIN customers c ON c.id = a.customer_id
WHERE a.id=@id;",
                    new Hashtable { { "@id", allocId } });

                if (dt == null || dt.Rows.Count != 1)
                {
                    ShowError("Allocation record nahi mila.");
                    return;
                }

                var r = dt.Rows[0];
                if (!TryToInt(r["customer_id"], out int curCustomerId)
                    || !TryToInt(r["plot_id"], out int curPlotId)
                    || !TryToInt(r["society_id"], out int curSocId)
                    || !TryToInt(r["block_id"], out int curBlockId)
                    || !TryToInt(r["plot_type_id"], out int curTypeId))
                {
                    ShowError("Allocation data incomplete / invalid.");
                    return;
                }
                string oldPlotNo = Convert.ToString(r["old_plot_no"]);
                string oldCustomerName = Convert.ToString(r["customer_name"]);
                decimal curPrice = 0m;
                try { if (r["price"] != DBNull.Value) curPrice = Convert.ToDecimal(r["price"]); } catch { }
                int receiptCount = 0;
                TryToInt(r["receipt_count"], out receiptCount);
                decimal receiptTotal = 0m;
                try { if (r["receipt_total"] != DBNull.Value) receiptTotal = Convert.ToDecimal(r["receipt_total"]); } catch { }
                string curNotes = Convert.ToString(r["notes"] ?? "");

                Form dlg = BuildTransferDialog(
                    out Label lblInfo,
                    out ComboBox cbNewCustomer,
                    out ComboBox cbSoc, out ComboBox cbBlock, out ComboBox cbType, out ComboBox cbPlot,
                    out NumericUpDown numPrice, out NumericUpDown numPayAmount, out ComboBox cbPayMode,
                    out TextBox txtNotes);

                lblInfo.Text =
                    "Current owner: " + oldCustomerName + "\n" +
                    "Plot: " + oldPlotNo + "\n" +
                    "Existing receipts: " + receiptCount + "  |  Paid: " + receiptTotal.ToString("N2") + "\n" +
                    "Naya owner select karo — same allocation (sync_id) pe ownership + purani receipts move.";

                LoadCustomers(cbNewCustomer, 0);
                try { cbNewCustomer.SelectedIndex = -1; } catch { }
                LoadSocieties(cbSoc, curSocId);
                LoadBlocks(cbBlock, curSocId, curBlockId);
                LoadPlotTypes(cbType, curSocId, curTypeId);
                // Current plot allowed (owner change); Available also for optional plot move
                LoadPlots(cbPlot, curSocId, curBlockId, curTypeId, curPlotId);

                cbSoc.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid))
                    {
                        LoadBlocks(cbBlock, sid, 0);
                        LoadPlotTypes(cbType, sid, 0);
                        cbPlot.DataSource = null;
                    }
                };
                cbBlock.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid) && TryGetId(cbBlock, out int bid) && TryGetId(cbType, out int ptid))
                        LoadPlots(cbPlot, sid, bid, ptid, curPlotId);
                };
                cbType.SelectionChangeCommitted += (s, e) =>
                {
                    if (TryGetId(cbSoc, out int sid) && TryGetId(cbBlock, out int bid) && TryGetId(cbType, out int ptid))
                        LoadPlots(cbPlot, sid, bid, ptid, curPlotId);
                };

                numPrice.Value = curPrice > numPrice.Maximum ? numPrice.Maximum : (curPrice < numPrice.Minimum ? numPrice.Minimum : curPrice);
                numPayAmount.Value = 0;
                try { cbPayMode.SelectedIndex = 0; } catch { }
                txtNotes.Text = curNotes;

                if (dlg.ShowDialog(GetDialogOwner()) != DialogResult.OK)
                {
                    dlg.Dispose();
                    return;
                }

                if (!TryGetId(cbNewCustomer, out int newCustomerId))
                {
                    ShowInfo("Naya owner (customer) select karein.");
                    dlg.Dispose();
                    return;
                }
                if (newCustomerId == curCustomerId)
                {
                    ShowInfo("Naya owner current owner se different hona chahiye.");
                    dlg.Dispose();
                    return;
                }
                if (!TryGetId(cbPlot, out int newPlotId))
                {
                    ShowInfo("Plot select karein.");
                    dlg.Dispose();
                    return;
                }

                decimal newPrice = numPrice.Value;
                if (newPrice < 0) newPrice = 0;
                decimal payAmt = numPayAmount.Value;
                if (payAmt < 0) payAmt = 0;
                string payMode = Convert.ToString(cbPayMode.SelectedItem ?? "Cash").Trim();
                if (string.IsNullOrWhiteSpace(payMode)) payMode = "Cash";
                string newNotes = (txtNotes.Text ?? "").Trim();
                string newPlotNo = cbPlot.Text;
                string newCustomerName = cbNewCustomer.Text;

                if (!AskYesNo(
                    "Owner transfer confirm?\n\n" +
                    "From: " + oldCustomerName + "\n" +
                    "To: " + newCustomerName + "\n" +
                    "Plot: " + oldPlotNo + (newPlotId != curPlotId ? " → " + newPlotNo : "") + "\n" +
                    "Purani receipts (" + receiptCount + ") isi allocation pe new owner ke naam pe rahengi.\n" +
                    (payAmt > 0
                        ? "New receipt: " + payAmt.ToString("N2") + " (" + payMode + ") — new owner.\n"
                        : "New receipt: nahi (amount 0).\n"),
                    "Confirm Owner Transfer"))
                {
                    dlg.Dispose();
                    return;
                }

                string err = ExecuteOwnerTransfer(
                    allocId, curCustomerId, newCustomerId,
                    curPlotId, newPlotId, oldPlotNo, newPlotNo,
                    oldCustomerName, newCustomerName,
                    newPrice, newNotes, receiptCount, payAmt, payMode);
                dlg.Dispose();

                if (err == null)
                {
                    try
                    {
                        var syncRows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>();
                        syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("customer_plots", allocId));
                        if (newPlotId > 0)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("plots", newPlotId));
                        if (curPlotId > 0 && curPlotId != newPlotId)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("plots", curPlotId));
                        HubSyncService.NotifyLocalWrites(syncRows);
                    }
                    catch { try { HubSyncService.NotifyLocalWrite(); } catch { } }
                    ShowInfo(
                        "Owner transfer successful.\n\n" +
                        "• Owner: " + oldCustomerName + " → " + newCustomerName + "\n" +
                        "• Plot: " + (newPlotId != curPlotId ? oldPlotNo + " → " + newPlotNo : oldPlotNo + " (same)") + "\n" +
                        "• Same allocation id/sync_id — " + receiptCount + " purani receipt(s) new owner pe linked.\n" +
                        (payAmt > 0
                            ? "• New receipt " + payAmt.ToString("N2") + " (" + payMode + ") new owner ke naam pe.\n"
                            : ""));
                    GetData();
                }
                else ShowError(err);
            }
            catch (Exception ex)
            {
                ShowError("Owner transfer failed.\n\n" + ex.Message);
            }
        }

        private string ExecuteOwnerTransfer(
            int allocId, int oldCustomerId, int newCustomerId,
            int oldPlotId, int newPlotId, string oldPlotNo, string newPlotNo,
            string oldCustomerName, string newCustomerName,
            decimal newPrice, string newNotes, int receiptCount,
            decimal payAmount, string payMode)
        {
            MainClass.AcquireSharedConnection();
            System.Data.SQLite.SQLiteTransaction tx = null;
            try
            {
                using (var pragma = new System.Data.SQLite.SQLiteCommand("PRAGMA foreign_keys = ON;", MainClass.con))
                    pragma.ExecuteNonQuery();

                tx = MainClass.con.BeginTransaction();

                int curPlot;
                using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
SELECT COALESCE(status,''), plot_id, customer_id FROM customer_plots WHERE id=@id;", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@id", allocId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return "Allocation not found.";
                        string st = Convert.ToString(rd[0]);
                        curPlot = Convert.ToInt32(rd[1]);
                        int cid = Convert.ToInt32(rd[2]);
                        if (!string.Equals(st, "Active", StringComparison.OrdinalIgnoreCase))
                            return "Sirf Active allocation transfer ho sakti hai.";
                        if (curPlot != oldPlotId)
                            return "Allocation plot change ho chuka hai — dubara try karein.";
                        if (cid != oldCustomerId)
                            return "Owner pehle hi change ho chuka hai — dubara try karein.";
                    }
                }

                if (newCustomerId == oldCustomerId)
                    return "Naya owner current se different hona chahiye.";

                // Optional plot move
                if (newPlotId != oldPlotId)
                {
                    using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
UPDATE plots SET status='Hold'
WHERE id=@id AND status='Available';", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", newPlotId);
                        if (cmd.ExecuteNonQuery() <= 0)
                            return "Naya plot Available nahi hai (kisi aur ne le liya).";
                    }
                }

                string noteAdd = " | Owner transfer " + DateTime.Today.ToString("yyyy-MM-dd")
                    + " " + (oldCustomerName ?? "") + " → " + (newCustomerName ?? "")
                    + (newPlotId != oldPlotId ? " | Plot " + oldPlotNo + " → " + newPlotNo : "")
                    + " | Prior receipts kept on same alloc (" + receiptCount + ")";

                // SAME customer_plots.id (+ sync_id) — receipts/ledger FK stay valid
                using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
UPDATE customer_plots
SET customer_id=@ncid,
    plot_id=@npid,
    price=@price,
    notes=TRIM(COALESCE(@notes,'') || @noteAdd)
WHERE id=@id AND status='Active';", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@ncid", newCustomerId);
                    cmd.Parameters.AddWithValue("@npid", newPlotId);
                    cmd.Parameters.AddWithValue("@price", newPrice);
                    cmd.Parameters.AddWithValue("@notes", newNotes ?? "");
                    cmd.Parameters.AddWithValue("@noteAdd", noteAdd);
                    cmd.Parameters.AddWithValue("@id", allocId);
                    if (cmd.ExecuteNonQuery() <= 0)
                        throw new Exception("Allocation owner update fail.");
                }

                if (newPlotId != oldPlotId)
                {
                    using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
UPDATE plots SET status='Available' WHERE id=@id;", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", oldPlotId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // New owner's payment receipt on SAME allocation id
                if (payAmount > 0)
                {
                    string mode = string.IsNullOrWhiteSpace(payMode) ? "Cash" : payMode.Trim();
                    string txnType = string.Equals(mode, "Bank", StringComparison.OrdinalIgnoreCase) ? "ReceiptBank" : "ReceiptCash";
                    string today = DateTime.Today.ToString("yyyy-MM-dd");
                    string nar = "Transfer receipt — " + (newCustomerName ?? "new owner")
                        + " (from " + (oldCustomerName ?? "prior") + ")";

                    long ledgerId;
                    using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
INSERT INTO ledger (customer_plot_id, txn_date, voucher_no, narration, debit, credit, txn_type)
VALUES (@cpid, @date, NULL, @nar, 0, @amt, @tt);
SELECT last_insert_rowid();", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", allocId);
                        cmd.Parameters.AddWithValue("@date", today);
                        cmd.Parameters.AddWithValue("@nar", nar);
                        cmd.Parameters.AddWithValue("@amt", payAmount);
                        cmd.Parameters.AddWithValue("@tt", txnType);
                        ledgerId = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    using (var cmd = new System.Data.SQLite.SQLiteCommand(@"
INSERT INTO receipts (customer_plot_id, receipt_date, amount, mode, ref_no, notes, ledger_id)
VALUES (@cpid, @date, @amt, @mode, NULL, @notes, @lid);", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", allocId);
                        cmd.Parameters.AddWithValue("@date", today);
                        cmd.Parameters.AddWithValue("@amt", payAmount);
                        cmd.Parameters.AddWithValue("@mode", mode);
                        cmd.Parameters.AddWithValue("@notes", nar);
                        cmd.Parameters.AddWithValue("@lid", ledgerId);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                tx = null;
                return null;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                return ex.Message;
            }
            finally
            {
                try { if (tx != null) tx.Rollback(); } catch { }
                MainClass.ReleaseSharedConnection();
            }
        }

        private Form GetDialogOwner()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f is MainForm && f.Visible && !f.IsDisposed)
                        return f;
                }
            }
            catch { }
            try
            {
                var top = FindForm();
                if (top != null && top.TopLevel && !top.IsDisposed) return top;
            }
            catch { }
            return null;
        }

        private Form BuildTransferDialog(
            out Label lblInfo,
            out ComboBox cbNewCustomer,
            out ComboBox cbSoc, out ComboBox cbBlock, out ComboBox cbType, out ComboBox cbPlot,
            out NumericUpDown numPrice, out NumericUpDown numPayAmount, out ComboBox cbPayMode,
            out TextBox txtNotes)
        {
            Color bg = Color.FromArgb(32, 36, 61);
            Color panelBg = Color.FromArgb(37, 41, 74);
            Color accent = Color.FromArgb(112, 51, 255);
            Color accentBlue = Color.FromArgb(0, 123, 255);
            Color white = Color.White;

            var f = new Form
            {
                Text = "Transfer Owner",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Width = 600,
                Height = 640,
                BackColor = bg,
                Padding = new Padding(0),
                AutoScaleMode = AutoScaleMode.Font
            };

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(28, 32, 55)
            };
            var title = new Label
            {
                Text = "Transfer Owner  ·  Same Alloc Sync Id",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = white,
                AutoSize = true,
                Location = new Point(18, 20)
            };
            top.Controls.Add(title);

            lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 92,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 225, 240),
                BackColor = Color.FromArgb(45, 50, 85),
                Padding = new Padding(16, 10, 16, 10)
            };

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 12, 18, 8),
                BackColor = bg
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                BackColor = bg
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 9; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 8 ? 64 : 42));

            Label L(string t) => new Label
            {
                Text = t,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 12, 8, 0),
                ForeColor = white,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            ComboBox Cb()
            {
                var c = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 8, 0, 8),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                    BackColor = panelBg,
                    ForeColor = white
                };
                return c;
            }

            NumericUpDown Num(decimal min) => new NumericUpDown
            {
                DecimalPlaces = 0,
                Minimum = min,
                Maximum = 1000000000,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                BackColor = panelBg,
                ForeColor = white,
                BorderStyle = BorderStyle.FixedSingle
            };

            cbNewCustomer = Cb();
            cbSoc = Cb(); cbBlock = Cb(); cbType = Cb(); cbPlot = Cb();
            numPrice = Num(1);
            numPayAmount = Num(0);
            cbPayMode = Cb();
            cbPayMode.Items.AddRange(new object[] { "Cash", "Bank" });
            txtNotes = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                Font = new Font("Segoe UI", 10.5f),
                BackColor = panelBg,
                ForeColor = white,
                BorderStyle = BorderStyle.FixedSingle
            };

            grid.Controls.Add(L("New Owner"), 0, 0); grid.Controls.Add(cbNewCustomer, 1, 0);
            grid.Controls.Add(L("Society"), 0, 1); grid.Controls.Add(cbSoc, 1, 1);
            grid.Controls.Add(L("Block"), 0, 2); grid.Controls.Add(cbBlock, 1, 2);
            grid.Controls.Add(L("Plot Type"), 0, 3); grid.Controls.Add(cbType, 1, 3);
            grid.Controls.Add(L("Plot"), 0, 4); grid.Controls.Add(cbPlot, 1, 4);
            grid.Controls.Add(L("Price"), 0, 5); grid.Controls.Add(numPrice, 1, 5);
            grid.Controls.Add(L("Pay Amount"), 0, 6); grid.Controls.Add(numPayAmount, 1, 6);
            grid.Controls.Add(L("Pay Mode"), 0, 7); grid.Controls.Add(cbPayMode, 1, 7);
            grid.Controls.Add(L("Notes"), 0, 8); grid.Controls.Add(txtNotes, 1, 8);
            body.Controls.Add(grid);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.FromArgb(28, 32, 55),
                Padding = new Padding(16, 12, 16, 12)
            };

            var btnOK = new Button
            {
                Text = "Transfer",
                Width = 130,
                Height = 42,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                BackColor = accentBlue,
                ForeColor = white,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Location = new Point(bottom.Width - 280, 14);

            var btnCancel = new Button
            {
                Text = "Close",
                Width = 110,
                Height = 42,
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                BackColor = Color.Crimson,
                ForeColor = white,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(bottom.Width - 140, 14);

            bottom.Resize += (s, e) =>
            {
                btnOK.Location = new Point(bottom.ClientSize.Width - 280, 14);
                btnCancel.Location = new Point(bottom.ClientSize.Width - 140, 14);
            };

            bottom.Controls.Add(btnOK);
            bottom.Controls.Add(btnCancel);

            f.Controls.Add(body);
            f.Controls.Add(bottom);
            f.Controls.Add(lblInfo);
            f.Controls.Add(top);
            f.AcceptButton = btnOK;
            f.CancelButton = btnCancel;

            // Soft shadow border feel
            f.Paint += (s, e) =>
            {
                using (var pen = new Pen(accent, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, f.ClientSize.Width - 3, f.ClientSize.Height - 3);
            };

            return f;
        }

        private bool AskYesNo(string message, string title)
        {
            try
            {
                using (var ask = new YesOrNoMessage(message, title))
                    return ask.ShowDialog() == DialogResult.Yes;
            }
            catch
            {
                return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            }
        }

        // ------------------ STATUS/DB ------------------
        private string UpdateAllocationWithStatuses(
            int allocId, int oldPlotId, int newPlotId,
            string oldStatus, string newStatus,
            int newCustomerId, decimal newPrice, string newBookingDate, string newNotes,
            int newBrokerId, decimal newBrokerComm, string newDealType, decimal newMonthly = 0)
        {
            if (string.IsNullOrWhiteSpace(newDealType)) newDealType = "Installment";
            if (string.Equals(newDealType, "Reserve", StringComparison.OrdinalIgnoreCase)) newDealType = "Reserve";
            else if (string.Equals(newDealType, "Sold", StringComparison.OrdinalIgnoreCase)) newDealType = "Sold";
            else newDealType = "Installment";
            string plotDealStatus = newDealType == "Sold" ? "Sold" : "Hold";

            MainClass.AcquireSharedConnection();
            SQLiteTransaction tx = null;
            try
            {
                tx = MainClass.con.BeginTransaction();

                if (newPlotId != oldPlotId)
                {
                    object avail;
                    using (var cmd = new SQLiteCommand(
                        "SELECT CASE WHEN EXISTS(SELECT 1 FROM plots WHERE id=@id AND status='Available') THEN 1 ELSE 0 END;",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", newPlotId);
                        avail = cmd.ExecuteScalar();
                    }
                    if (Convert.ToInt32(avail ?? 0) != 1)
                        return "New plot Available nahin hai.";

                    int held;
                    using (var cmd = new SQLiteCommand(
                        "UPDATE plots SET status=@st WHERE id=@id AND status='Available';",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@st", plotDealStatus);
                        cmd.Parameters.AddWithValue("@id", newPlotId);
                        held = cmd.ExecuteNonQuery();
                    }
                    if (held <= 0) return "New plot ko Hold/Sold nahi kar paaye.";

                    if (string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE plots SET status='Available' WHERE id=@id AND status IN ('Hold','Sold');",
                            MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", oldPlotId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    int upPlot;
                    using (var cmd = new SQLiteCommand(
                        $"UPDATE {T_ALLOC} SET plot_id=@pid WHERE id=@id;", MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@pid", newPlotId);
                        cmd.Parameters.AddWithValue("@id", allocId);
                        upPlot = cmd.ExecuteNonQuery();
                    }
                    if (upPlot <= 0) return "Allocation me plot update fail.";

                    using (var cmd = new SQLiteCommand(
                        "UPDATE plots SET status='Available' WHERE id=@id AND status IN ('Hold','Sold');",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", oldPlotId);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    if (oldStatus == "Active" && newStatus == "Cancelled")
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE plots SET status='Available' WHERE id=@pid AND status IN ('Hold','Sold');",
                            MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@pid", newPlotId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (oldStatus == "Active" && newStatus == "Completed")
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE plots SET status='Sold' WHERE id=@pid AND status IN ('Hold','Available','Sold');",
                            MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@pid", newPlotId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (oldStatus == "Cancelled" && newStatus == "Active")
                    {
                        int ok;
                        using (var cmd = new SQLiteCommand(
                            "UPDATE plots SET status=@st WHERE id=@pid AND status='Available';",
                            MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@st", plotDealStatus);
                            cmd.Parameters.AddWithValue("@pid", newPlotId);
                            ok = cmd.ExecuteNonQuery();
                        }
                        if (ok <= 0) return "Plot ko Hold/Sold nahi kar paaye.";
                    }
                    else if (oldStatus == "Completed") return "Completed allocation ka status change allow nahi.";
                }
                else if (string.Equals(newStatus, "Active", StringComparison.OrdinalIgnoreCase) && newPlotId == oldPlotId)
                {
                    using (var cmd = new SQLiteCommand(
                        "UPDATE plots SET status=@st WHERE id=@pid AND status IN ('Hold','Sold','Available');",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@st", plotDealStatus);
                        cmd.Parameters.AddWithValue("@pid", newPlotId);
                        cmd.ExecuteNonQuery();
                    }
                }

                int up;
                using (var cmd = new SQLiteCommand(
                    $@"UPDATE {T_ALLOC} 
                       SET customer_id=@cid, 
                           price=@price, 
                           booking_date=@bdate, 
                           notes=@notes, 
                           status=@st,
                           broker_id=@bid,
                           broker_commission=@bcomm,
                           deal_type=@deal,
                           monthly_installment=@monthly
                       WHERE id=@id;", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@cid", newCustomerId);
                    cmd.Parameters.AddWithValue("@price", newPrice);
                    cmd.Parameters.AddWithValue("@bdate", newBookingDate);
                    cmd.Parameters.AddWithValue("@notes", newNotes);
                    cmd.Parameters.AddWithValue("@st", newStatus);
                    cmd.Parameters.AddWithValue("@bid", newBrokerId > 0 ? (object)newBrokerId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@bcomm", newBrokerComm);
                    cmd.Parameters.AddWithValue("@deal", newDealType);
                    cmd.Parameters.AddWithValue("@monthly", newMonthly < 0 ? 0 : newMonthly);
                    cmd.Parameters.AddWithValue("@id", allocId);
                    up = cmd.ExecuteNonQuery();
                }
                if (up <= 0) return "Allocation fields update fail.";

                tx.Commit();
                tx = null;
                return null;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                return ex.Message;
            }
            finally
            {
                try { if (tx != null) tx.Rollback(); } catch { }
                MainClass.ReleaseSharedConnection();
            }
        }

        // ------------------ Edit dialog builder ------------------
        private Form BuildEditDialog(
            out ComboBox cbCustomer, out ComboBox cbSoc, out ComboBox cbBlock, out ComboBox cbType, out ComboBox cbPlot,
            out NumericUpDown numPrice, out DateTimePicker dtpBook, out TextBox txtNotes, out ComboBox cbStatus,
            out ComboBox cbBroker, out NumericUpDown numBrokerComm, out ComboBox cbDealType,
            out NumericUpDown numMonthly)
        {
            var f = new Form
            {
                Text = "Edit Allocation",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, // correct
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Width = 620,
                Height = 540,
                Padding = new Padding(14),
                AutoScaleMode = AutoScaleMode.Font
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 8,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Label L(string t) => new Label { Text = t, AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
            ComboBox Cb() => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 16, 8), Anchor = AnchorStyles.Left | AnchorStyles.Right };
            TextBox TbMulti() => new TextBox { Multiline = true, Height = 70, Margin = new Padding(0, 4, 0, 8), Anchor = AnchorStyles.Left | AnchorStyles.Right };
            NumericUpDown Num() => new NumericUpDown { DecimalPlaces = 0, Minimum = 0, Maximum = 1000000000, Margin = new Padding(0, 4, 16, 8), Anchor = AnchorStyles.Left | AnchorStyles.Right };
            DateTimePicker Dtp() => new DateTimePicker { Format = DateTimePickerFormat.Short, Margin = new Padding(0, 4, 0, 8), Anchor = AnchorStyles.Left | AnchorStyles.Right };

            cbCustomer = Cb(); cbSoc = Cb(); cbBlock = Cb(); cbType = Cb(); cbPlot = Cb();
            numPrice = Num(); dtpBook = Dtp(); cbStatus = Cb(); txtNotes = TbMulti();
            cbBroker = Cb(); numBrokerComm = Num(); cbDealType = Cb(); numMonthly = Num();
            cbDealType.Items.AddRange(new object[] { "Installment", "Reserve", "Sold" });
            cbDealType.SelectedIndex = 0;

            grid.Controls.Add(L("Customer"), 0, 0); grid.Controls.Add(cbCustomer, 1, 0);
            grid.Controls.Add(L("Plot Type"), 2, 0); grid.Controls.Add(cbType, 3, 0);
            grid.Controls.Add(L("Society"), 0, 1); grid.Controls.Add(cbSoc, 1, 1);
            grid.Controls.Add(L("Plot No"), 2, 1); grid.Controls.Add(cbPlot, 3, 1);
            grid.Controls.Add(L("Block"), 0, 2); grid.Controls.Add(cbBlock, 1, 2);
            grid.Controls.Add(L("Booking Date"), 2, 2); grid.Controls.Add(dtpBook, 3, 2);
            grid.Controls.Add(L("Price"), 0, 3); grid.Controls.Add(numPrice, 1, 3);
            grid.Controls.Add(L("Status"), 2, 3); grid.Controls.Add(cbStatus, 3, 3);
            grid.Controls.Add(L("Broker"), 0, 4); grid.Controls.Add(cbBroker, 1, 4);
            grid.Controls.Add(L("Broker Commission"), 2, 4); grid.Controls.Add(numBrokerComm, 3, 4);
            grid.Controls.Add(L("Deal Type"), 0, 5); grid.Controls.Add(cbDealType, 1, 5);
            grid.Controls.Add(L("Monthly Installment"), 2, 5); grid.Controls.Add(numMonthly, 3, 5);
            grid.Controls.Add(L("Notes"), 0, 6); grid.Controls.Add(txtNotes, 1, 6); grid.SetColumnSpan(txtNotes, 3);

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, WrapContents = false };
            var btnOK = new Button { Text = "Save", Width = 90, DialogResult = DialogResult.OK, Margin = new Padding(8, 8, 0, 0) };
            var btnCancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel, Margin = new Padding(8, 8, 0, 0) };
            pnlButtons.Controls.Add(btnOK); pnlButtons.Controls.Add(btnCancel);

            f.Controls.Add(pnlButtons); f.Controls.Add(new Panel { Height = 6, Dock = DockStyle.Top }); f.Controls.Add(grid);
            f.AcceptButton = btnOK; f.CancelButton = btnCancel; return f;
        }

        // ------------------ Loaders ------------------
        private static long ToLong(object v) { if (v == null || v == DBNull.Value) return 0; try { return Convert.ToInt64(v); } catch { return 0; } }

        private void SetSelectedValueSafe(ComboBox cb, object wantValueOrId)
        {
            if (cb == null || cb.DataSource == null) return;
            long want = ToLong(wantValueOrId);
            if (cb.DataSource is DataTable dt)
            {
                foreach (DataRow r in dt.Rows) if (ToLong(r["id"]) == want) { cb.SelectedValue = r["id"]; return; }
                cb.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
            }
            else if (cb.DataSource is DataView dv)
            {
                foreach (DataRowView rv in dv) if (ToLong(rv["id"]) == want) { cb.SelectedValue = rv["id"]; return; }
                cb.SelectedIndex = dv.Count > 0 ? 0 : -1;
            }
            else { cb.SelectedValue = want; if (cb.SelectedIndex < 0 && cb.Items.Count > 0) cb.SelectedIndex = 0; }
        }

        private bool TryGetId(ComboBox cb, out int id)
        {
            id = 0; var v = cb?.SelectedValue; long L = ToLong(v);
            if (L > 0) { id = (int)L; return true; }
            if (cb?.SelectedItem is DataRowView drv && drv.Row.Table.Columns.Contains("id"))
            { L = ToLong(drv["id"]); if (L > 0) { id = (int)L; return true; } }
            return false;
        }

        private void LoadCustomers(ComboBox cb, int selectId)
        {
            MainClass.LoadDataIntoComboBox("SELECT id, name FROM customers ORDER BY name;", cb, "name", "id");
            SetSelectedValueSafe(cb, selectId);
        }
        private void LoadSocieties(ComboBox cb, int selectId)
        {
            MainClass.LoadDataIntoComboBox("SELECT id, name FROM societies ORDER BY name;", cb, "name", "id");
            SetSelectedValueSafe(cb, selectId);
        }
        private void LoadBlocks(ComboBox cb, int societyId, int selectId)
        {
            MainClass.LoadDataIntoComboBox($"SELECT id, name FROM blocks WHERE society_id={societyId} ORDER BY name;", cb, "name", "id");
            SetSelectedValueSafe(cb, selectId);
        }
        private void LoadPlotTypes(ComboBox cb, int societyId, int selectId)
        {
            MainClass.LoadDataIntoComboBox($"SELECT id, code FROM plot_types WHERE society_id={societyId} ORDER BY code;", cb, "code", "id");
            SetSelectedValueSafe(cb, selectId);
        }
        private void LoadPlots(ComboBox cb, int sid, int bid, int ptid, int currentPlotId)
        {
            try { MainClass.RepairAllocatedPlotStatuses(); } catch { }
            var dt = MainClass.ExecuteSelectQuery(@"
        SELECT id, plot_no FROM (
            SELECT id, plot_no, 0 AS sortKey FROM plots WHERE id=@curr
            UNION
            SELECT id, plot_no, 1 AS sortKey
            FROM plots
            WHERE society_id=@sid AND block_id=@bid AND plot_type_id=@ptid AND status='Available'
              AND NOT EXISTS (
                SELECT 1 FROM customer_plots cp
                WHERE cp.plot_id = plots.id
                  AND IFNULL(cp.status,'') = 'Active'
              )
        ) t
        GROUP BY id, plot_no, sortKey
        ORDER BY sortKey, plot_no;",
                new Hashtable { { "@curr", currentPlotId }, { "@sid", sid }, { "@bid", bid }, { "@ptid", ptid } });

            cb.DisplayMember = "plot_no"; cb.ValueMember = "id"; cb.DataSource = dt; SetSelectedValueSafe(cb, currentPlotId);
        }
        private void LoadBrokers(ComboBox cb, string selectKey)
        {
            MainClass.LoadBrokerPartnerCombo(cb, selectKey);
        }

        // ------------------ Select Allocation (optional once) ------------------
        private void OpenSelectAllocationOnce()
        {
            if (_selectAllocDismissed) return;
            if (Interlocked.Exchange(ref _selectAllocOpenFlag, 1) == 1) return;

            try
            {
                if (!ShowAllocationPickerWithSearch(out int id, out string disp)) { _selectAllocDismissed = true; return; }

                var nameOnly = disp.Split('|')[0].Trim();
                txtSearch.Text = nameOnly;
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Allocation picker failed.\n\n" + ex.Message);
            }
            finally
            {
                _suppressNextCtrlA = true;
                _ctrlADebounceTimer.Stop();
                _ctrlADebounceTimer.Start();

                Interlocked.Exchange(ref _selectAllocOpenFlag, 0);
            }
        }

        private bool ShowAllocationPickerWithSearch(out int id, out string disp)
        {
            id = 0; disp = "";

            var dt = MainClass.ExecuteSelectQuery(@"
SELECT a.id AS id,
       c.name || ' | Plot ' || p.plot_no || ' | ' || s.name || ' | ' || b.name AS disp,
       c.name AS cust, p.plot_no AS plotno, s.name AS society, b.name AS block
FROM customer_plots a
JOIN customers   c ON c.id=a.customer_id
JOIN plots       p ON p.id=a.plot_id
JOIN societies   s ON s.id=p.society_id
JOIN blocks      b ON b.id=p.block_id
ORDER BY c.name, p.plot_no;", null);

            if (dt == null || dt.Rows.Count == 0)
            {
                ShowInfo("Active allocations not found.");
                return false;
            }

            var dv = new DataView(dt);

            var f = new Form
            {
                Text = "Select Allocation",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, // fixed
                Width = 760,
                Height = 560,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(32, 36, 61),
                ForeColor = Color.White,
                KeyPreview = true
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(32, 36, 61) };
            var tbSearch = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Right,
                Width = 320,
                Margin = new Padding(12),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular)
            };
            var lbl = new Label
            {
                Text = "Select Allocation",
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 320,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            header.Controls.Add(tbSearch);
            header.Controls.Add(lbl);

            var gv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                DataSource = dv,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.FromArgb(32, 36, 61),
                GridColor = Color.FromArgb(50, 56, 76),
                EnableHeadersVisualStyles = false
            };
            gv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(32, 36, 61);
            gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gv.ColumnHeadersHeight = 40;
            gv.DefaultCellStyle.BackColor = Color.FromArgb(32, 36, 61);
            gv.DefaultCellStyle.ForeColor = Color.White;
            gv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 120, 246);
            gv.DefaultCellStyle.SelectionForeColor = Color.White;

            gv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "ID", DataPropertyName = "id", Width = 80 });
            gv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "disp",
                HeaderText = "Customer | Plot | Society | Block",
                DataPropertyName = "disp",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = { Font = new Font("Segoe UI", 11.5f, FontStyle.Bold) }
            });

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(32, 36, 61) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8), Height = 50, AutoSize = true };
            var btnOk = new Button { Text = "Select", Width = 110, Height = 36, Margin = new Padding(8) };
            var btnCancel = new Button { Text = "Close", Width = 110, Height = 36, Margin = new Padding(8) };
            flow.Controls.Add(btnOk); flow.Controls.Add(btnCancel); footer.Controls.Add(flow);

            f.Controls.Add(gv);
            f.Controls.Add(footer);
            f.Controls.Add(header);
            f.AcceptButton = btnOk;
            f.CancelButton = btnCancel;

            tbSearch.TextChanged += (s, e) =>
            {
                var raw = (tbSearch.Text ?? "").Trim().ToLowerInvariant();
                var q = raw.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                dv.RowFilter = string.IsNullOrWhiteSpace(q) ? "" : $"LOWER(cust) LIKE '%{q}%'";
            };

            tbSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    f.DialogResult = DialogResult.OK;
                }
            };

            gv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) f.DialogResult = DialogResult.OK; };
            btnOk.Click += (s, e) => f.DialogResult = DialogResult.OK;
            btnCancel.Click += (s, e) => f.DialogResult = DialogResult.Cancel;

            DialogResult res;
            try { res = f.ShowDialog(this); }
            catch { res = f.ShowDialog(); }

            if (res == DialogResult.OK && gv.CurrentRow != null)
            {
                id = Convert.ToInt32(gv.CurrentRow.Cells["id"].Value);
                disp = Convert.ToString(gv.CurrentRow.Cells["disp"].Value);
            }
            f.Dispose();
            return id > 0;
        }

        // ------------------ WhatsApp helpers ------------------
        private static string NormalizePhoneToDigits(string input)
        {
            var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("03") && digits.Length == 11) digits = "92" + digits.Substring(1);
            return digits;
        }

        private static string BuildWhatsAppMessage(string name, string society, string block, string type, string plotNo, string price, string bookingDate, string status)
        {
            return
$@"Assalam o Alaikum {name},
Aap ke plot allocation ki tafseel:

Society: {society}
Block: {block}
Type: {type}
Plot No: {plotNo}
Price: {price}
Booking Date: {bookingDate}
Status: {status}

Agar koi sawal ho to reply karein.";
        }

        private static void OpenWhatsAppWithPrefill(string digitsE164, string message)
        {
            try
            {
                string encoded = Uri.EscapeDataString(message);
                if (!TryOpenUrl($"whatsapp://send?phone={digitsE164}&text={encoded}"))
                    TryOpenUrl($"https://wa.me/{digitsE164}?text={encoded}");
            }
            catch { /* ignore */ }
        }
        private static bool TryOpenUrl(string url)
        {
            try { var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true }; Process.Start(psi); return true; }
            catch { return false; }
        }

        // ------------------ Columns + VIP styling ------------------
        private void EnsureRuntimeColumns()
        {
            try
            {
                var gv = guna2DataGridView1; if (gv == null) return;

                if (!gv.Columns.Contains("dgvPhone"))
                {
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = "dgvPhone",
                        HeaderText = "Phone",
                        DataPropertyName = "Phone",
                        Width = 120,
                        Visible = true
                    };
                    gv.Columns.Add(col);
                }

                if (!gv.Columns.Contains("dgvDealType"))
                {
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = "dgvDealType",
                        HeaderText = "Deal Type",
                        DataPropertyName = "DealType",
                        Width = 100,
                        Visible = true
                    };
                    int ix = gv.Columns.Contains("dgvStatus") ? gv.Columns["dgvStatus"].DisplayIndex : gv.Columns.Count;
                    gv.Columns.Add(col);
                    try { col.DisplayIndex = Math.Max(0, ix); } catch { }
                }

                if (!gv.Columns.Contains("dgvwa"))
                {
                    var btn = new DataGridViewButtonColumn
                    {
                        Name = "dgvwa",
                        HeaderText = "WhatsApp",
                        Text = "Send",
                        UseColumnTextForButtonValue = true,
                        Width = 110,
                        FlatStyle = FlatStyle.Flat
                    };
                    gv.Columns.Add(btn);
                }

                // ===== NEW: Cancel Allocation button =====
                if (!gv.Columns.Contains("dgvcancel"))
                {
                    var btnCancel = new DataGridViewButtonColumn
                    {
                        Name = "dgvcancel",
                        HeaderText = "Cancel",
                        Text = "Cancel",
                        UseColumnTextForButtonValue = true,
                        Width = 95,
                        FlatStyle = FlatStyle.Flat
                    };
                    gv.Columns.Add(btnCancel);
                }

                // ===== Transfer Plot (receipts auto) =====
                if (!gv.Columns.Contains("dgvtransfer"))
                {
                    var btnTransfer = new DataGridViewButtonColumn
                    {
                        Name = "dgvtransfer",
                        HeaderText = "Transfer",
                        Text = "Transfer",
                        UseColumnTextForButtonValue = true,
                        Width = 100,
                        FlatStyle = FlatStyle.Flat
                    };
                    gv.Columns.Add(btnTransfer);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ensure columns failed.\n\n" + ex.Message);
            }
        }
        private void ReorderActionColumns()
        {
            var gv = guna2DataGridView1;
            if (gv == null) return;

            // Fill mode action columns ko crush na kare — clickable width
            HardenActionCol(gv, "dgvdel", 56);
            HardenActionCol(gv, "dgvedit", 56);
            HardenActionCol(gv, "dgvtransfer", 100);
            HardenActionCol(gv, "dgvcancel", 95);
            HardenActionCol(gv, "dgvwa", 110);

            int last = gv.Columns.Count - 1;

            if (gv.Columns.Contains("dgvdel"))
            {
                gv.Columns["dgvdel"].DisplayIndex = last;
                last--;
            }

            if (gv.Columns.Contains("dgvedit"))
            {
                gv.Columns["dgvedit"].DisplayIndex = last;
                last--;
            }

            if (gv.Columns.Contains("dgvtransfer"))
            {
                gv.Columns["dgvtransfer"].DisplayIndex = last;
                last--;
            }

            if (gv.Columns.Contains("dgvcancel"))
            {
                gv.Columns["dgvcancel"].DisplayIndex = last;
                last--;
            }

            if (gv.Columns.Contains("dgvwa"))
            {
                gv.Columns["dgvwa"].DisplayIndex = last;
            }
        }

        private static void HardenActionCol(DataGridView gv, string name, int width)
        {
            if (gv == null || !gv.Columns.Contains(name)) return;
            try
            {
                var c = gv.Columns[name];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.MinimumWidth = width;
                c.Width = width;
            }
            catch { }
        }
        private void BumpCustomerFont()
        {
            var col = guna2DataGridView1.Columns["dgvCustomer"];
            if (col != null)
            {
                col.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
            }
        }

        private void Grid_CellPainting_VipWhatsApp(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvwa") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);

                Color greenStart = Color.FromArgb(37, 211, 102);
                Color greenEnd = Color.FromArgb(7, 94, 84);
                Color textColor = Color.White;

                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, greenStart, greenEnd, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(0, 128, 0)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("WhatsApp", font, new SolidBrush(textColor), rect, sf);
                }
            }
            catch
            {
                // Never MessageBox during paint
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ------------------ Helpers ------------------
        private static bool TryToInt(object v, out int id)
        {
            id = 0;
            try
            {
                if (v == null || v == DBNull.Value) return false;
                if (v is int i) { id = i; return true; }
                if (v is long l) { id = checked((int)l); return true; }
                return int.TryParse(Convert.ToString(v), out id);
            }
            catch { id = 0; return false; }
        }

        private bool TryGetSelectedId(out int id)
        {
            // Fallback — prefer TryGetIdFromRow(e.RowIndex) on grid clicks
            id = 0;
            try
            {
                int row = guna2DataGridView1.CurrentRow?.Index ?? -1;
                return TryGetIdFromRow(row, out id);
            }
            catch { id = 0; return false; }
        }

        /// <summary>
        /// Clicked row se id — CurrentRow Guna2 pe click ke waqt galat/null ho sakta hai
        /// (isi wajah se Edit/Delete/Transfer fail ho rahe they).
        /// </summary>
        private bool TryGetIdFromRow(int rowIndex, out int id)
        {
            id = 0;
            try
            {
                var gv = guna2DataGridView1;
                if (gv == null || rowIndex < 0 || rowIndex >= gv.Rows.Count) return false;
                if (!gv.Columns.Contains("dgvid")) return false;
                var v = gv.Rows[rowIndex].Cells["dgvid"]?.Value;
                if (v == null || v == DBNull.Value) return false;
                if (v is int i) { id = i; return id > 0; }
                if (v is long l)
                {
                    if (l <= 0 || l > int.MaxValue) return false;
                    id = (int)l;
                    return true;
                }
                if (!int.TryParse(Convert.ToString(v), out id)) return false;
                return id > 0;
            }
            catch { id = 0; return false; }
        }

        private void ShowError(string message)
        {
            try { using (var err = new ErrorFormMessage(message, "Error")) err.ShowDialog(); }
            catch { MessageBox.Show(message, "Error"); }
        }

        private void ShowInfo(string message)
        {
            try { using (var ok = new CustomeMessage(message, "RealEstate Builders")) ok.ShowDialog(); }
            catch { MessageBox.Show(message, "Info"); }
        }

        private void Grid_CellPainting_Cancel(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvcancel") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);

                Color start = Color.FromArgb(220, 53, 69);
                Color end = Color.FromArgb(160, 30, 45);
                Color textColor = Color.White;

                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, start, end, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(120, 20, 30)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("Cancel", font, new SolidBrush(textColor), rect, sf);
                }
            }
            catch
            {
                // Never MessageBox during paint
            }
        }

        private void Grid_CellPainting_Transfer(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvtransfer") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);

                Color start = Color.FromArgb(0, 123, 255);
                Color end = Color.FromArgb(0, 80, 180);
                Color textColor = Color.White;

                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, start, end, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(0, 60, 140)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("Transfer", font, new SolidBrush(textColor), rect, sf);
                }
            }
            catch
            {
                // Never MessageBox during paint
            }
        }

        private void ApplyStatusRowColors()
        {
            try
            {
                foreach (DataGridViewRow row in guna2DataGridView1.Rows)
                {
                    string status = Convert.ToString(row.Cells["dgvStatus"]?.Value);

                    if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    }
                    else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                        row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
