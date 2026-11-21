using Guna.UI2.WinForms;
using HubBuilders.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace HubBuilders.View
{
    public partial class frmReceiptFormView : SampleView
    {
        // -------- State --------
        private int _currentCustomerPlotId = 0;
        private string _currentAllocationDisp = "";

        // Smart-search modes (GLOBAL modes – txtSearch k liye use NAHIN honge)
        private bool _isRefMode = false; private string _refQuery = "";
        private bool _isCustMode = false; private string _custQuery = "";

        // Reentrancy / throttle
        private bool _gridActionGuard = false;
        private bool _dialogOpen = false;
        private DateTime _lastAddClick = DateTime.MinValue;

        // Low-level CAS guard for Add dialog (hard stop against double-open)
        private int _addOpenFlag = 0;

        // WhatsApp paint
        private readonly Color WaStart = Color.FromArgb(37, 211, 102);
        private readonly Color WaEnd = Color.FromArgb(18, 140, 126);
        private readonly Color WaText = Color.White;

        // Edit paint
        private readonly Color EditStart = Color.FromArgb(64, 148, 255);
        private readonly Color EditEnd = Color.FromArgb(24, 112, 210);
        private readonly Color EditText = Color.White;

        // Delete paint
        private readonly Color DelStart = Color.FromArgb(255, 82, 82);
        private readonly Color DelEnd = Color.FromArgb(210, 24, 24);
        private readonly Color DelText = Color.White;

        // VIP inline customer/allocation finder (no forced popup)
        private Panel _vipAllocPanel;
        private TextBox _txtVipAllocSearch;
        private ComboBox _cmbVipSociety;
        private DataGridView _dgvVipAlloc;
        private Label _lblVipSelected;
        private Label _lblVipHint;
        private Label _lblVipSociety;
        private DataTable _vipAllocTable;
        private DataView _vipAllocView;
        private bool _vipApplying;
        private bool _vipFilterBusy;
        private const int VipAllocPanelWidth = 340;

        // Date-range filter (From/To inclusive on receipt_date)
        private bool _dateFilterReady = false;
        private bool _dateFilterBusy = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        static frmReceiptFormView()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public frmReceiptFormView()
        {
            InitializeComponent();

            this.Load += frmReceiptFormView_Load;

            // paints
            guna2DataGridView1.CellPainting += Grid_CellPainting_WA;
            guna2DataGridView1.CellPainting += Grid_CellPainting_Edit;
            guna2DataGridView1.CellPainting += Grid_CellPainting_Del;

            WireGridEventsOnce();

            guna2DataGridView1.CurrentCellDirtyStateChanged += (s, ev) =>
            {
                if (guna2DataGridView1.IsCurrentCellDirty)
                    guna2DataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        // -------- txtSearch overrides (SampleView se) --------
        public override void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var q = (txtSearch == null ? "" : txtSearch.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(q))
                {
                    _isRefMode = _isCustMode = false;
                    _refQuery = _custQuery = "";
                    GetData(); // empty search → selected plot ki saari receipts
                    return;
                }
                // Typing = find customer/ref/plot; do NOT hide receipts by notes filter
                SmartSearchExternal(q, false);
            }
            catch (Exception ex)
            {
                ShowError("Search update failed.\n\n" + ex.Message);
            }
        }

        public override void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    var q = (txtSearch == null ? "" : txtSearch.Text ?? "").Trim();
                    SmartSearchExternal(q, true);
                }
            }
            catch (Exception ex)
            {
                ShowError("Search key handling failed.\n\n" + ex.Message);
            }
        }

        // -------- Event wiring dedupe --------
        private void WireGridEventsOnce()
        {
            try
            {
                var gv = guna2DataGridView1;
                if (gv == null) return;
                gv.CellClick -= Grid_CellHandler;
                gv.CellContentClick -= Grid_CellHandler;
                gv.CellContentClick += Grid_CellHandler; // single attach
            }
            catch (Exception ex)
            {
                ShowError("Grid event wiring failed.\n\n" + ex.Message);
            }
        }

        // -------- Hotkeys --------
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (keyData == (Keys.Control | Keys.F))
                {
                    var tb = txtSearch;
                    if (tb != null) { tb.Focus(); tb.SelectAll(); }
                    return true;
                }
                if (keyData == (Keys.Control | Keys.A))
                {
                    int id; string disp;
                    if (ShowAllocationPicker(out id, out disp))
                    {
                        ApplyVipAllocationSelection(id, disp);
                    }
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }
            catch (Exception ex)
            {
                ShowError("Hotkey handling failed.\n\n" + ex.Message);
                return true;
            }
        }

        // -------- Load --------
        private void frmReceiptFormView_Load(object sender, EventArgs e)
        {
            try
            {
                this.KeyPreview = true;
                guna2DataGridView1.DataBindingComplete += (s, ev) => { try { ReorderActionColumnsAndLock(); } catch (Exception ex2) { ShowError("Post-bind failed.\n\n" + ex2.Message); } };

                EnsureRuntimeColumns();
                WireGridEventsOnce();

                // Default date range: last 5 years → today (same as Customer Plots Receipts VIP)
                try
                {
                    var today = DateTime.Today;
                    fromdate.Value = today.AddYears(-5);
                    todate.Value = today;
                }
                catch { }

                _dateFilterReady = true;

                // VIP: no forced Select Allocation popup — inline finder on this form only
                EnsureVipAllocationFinder();
                this.Text = "Receipt View — type name to find customer";

                // Form pehle open, list/totals baad — hang feel khatam (UI same)
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        LoadVipAllocations();
                        ApplyDateFilterAndRefresh();
                        try
                        {
                            if (_txtVipAllocSearch != null && !_txtVipAllocSearch.IsDisposed)
                                _txtVipAllocSearch.Focus();
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        ShowError("Form load failed.\n\n" + ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                ShowError("Form load failed.\n\n" + ex.Message);
            }
        }

        // -------- Date range filter (From / To) --------
        private void btnLoadDates_Click(object sender, EventArgs e)
        {
            ApplyDateFilterAndRefresh();
        }

        private void fromdate_ValueChanged(object sender, EventArgs e)
        {
            if (!_dateFilterReady || _dateFilterBusy) return;
            ApplyDateFilterAndRefresh();
        }

        private void todate_ValueChanged(object sender, EventArgs e)
        {
            if (!_dateFilterReady || _dateFilterBusy) return;
            ApplyDateFilterAndRefresh();
        }

        private void ApplyDateFilterAndRefresh()
        {
            if (_dateFilterBusy) return;
            _dateFilterBusy = true;
            try
            {
                NormalizeDateRange();

                if (_isRefMode && !string.IsNullOrWhiteSpace(_refQuery))
                    TryBindRefReceipts(_refQuery);
                else if (_isCustMode && !string.IsNullOrWhiteSpace(_custQuery))
                    TryBindCustomerReceipts(_custQuery);
                else
                    GetData();
            }
            catch (Exception ex)
            {
                ShowError("Date filter failed.\n\n" + ex.Message);
            }
            finally
            {
                _dateFilterBusy = false;
            }
        }

        private void NormalizeDateRange()
        {
            try
            {
                if (fromdate == null || todate == null) return;
                if (fromdate.Value.Date <= todate.Value.Date) return;
                var tmp = fromdate.Value;
                fromdate.Value = todate.Value;
                todate.Value = tmp;
            }
            catch { }
        }

        private string GetFilterFromDate()
        {
            try
            {
                if (fromdate == null) return DateTime.Today.ToString("yyyy-MM-dd");
                return fromdate.Value.Date.ToString("yyyy-MM-dd");
            }
            catch { return DateTime.Today.ToString("yyyy-MM-dd"); }
        }

        private string GetFilterToDate()
        {
            try
            {
                if (todate == null) return DateTime.Today.ToString("yyyy-MM-dd");
                return todate.Value.Date.ToString("yyyy-MM-dd");
            }
            catch { return DateTime.Today.ToString("yyyy-MM-dd"); }
        }

        /// <summary>
        /// Top totals: From–To + Society. Easy recover view:
        /// Total Amount = sab; Total Receipt = allocate/booking din ke baad;
        /// Total Advance = usi din jab plot allocate/book hua.
        /// </summary>
        private void UpdateRangeTotals()
        {
            try
            {
                string fromStr = GetFilterFromDate();
                string toStr = GetFilterToDate();
                int societyId = GetVipSelectedSocietyId();

                // Date-range total — Society filter (0 = All Societies)
                string sql = @"
SELECT COUNT(*) AS cnt,
       COALESCE(SUM(r.amount), 0) AS total_amount,
       COALESCE(SUM(CASE WHEN date(r.receipt_date) = date(cp.booking_date) THEN r.amount ELSE 0 END), 0) AS total_advance,
       COALESCE(SUM(CASE WHEN date(r.receipt_date) > date(cp.booking_date) THEN r.amount ELSE 0 END), 0) AS total_receipt
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE date(r.receipt_date) >= date(@from)
  AND date(r.receipt_date) <= date(@to)
  AND (@sid = 0 OR p.society_id = @sid);";
                var prms = new Dictionary<string, object>
                {
                    { "@from", fromStr },
                    { "@to", toStr },
                    { "@sid", societyId }
                };

                var dt = MainClass.ExecuteSelectQuery1(sql, prms);
                decimal totalAmt = 0m, totalAdv = 0m, totalReceipt = 0m;
                int cnt = 0;
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    cnt = Convert.ToInt32(row["cnt"] == DBNull.Value ? 0 : row["cnt"]);
                    totalAmt = Convert.ToDecimal(row["total_amount"] == DBNull.Value ? 0 : row["total_amount"]);
                    totalAdv = Convert.ToDecimal(row["total_advance"] == DBNull.Value ? 0 : row["total_advance"]);
                    totalReceipt = Convert.ToDecimal(row["total_receipt"] == DBNull.Value ? 0 : row["total_receipt"]);
                }

                // Order: Total Amount → Total Receipt (baad) → Total Advance (allocate din)
                if (lblTotalReceiptAmount != null)
                    lblTotalReceiptAmount.Text = "Total Amount: " + totalAmt.ToString("N2");
                if (lblTotalAdvance != null)
                {
                    lblTotalAdvance.ForeColor = Color.FromArgb(120, 220, 180);
                    lblTotalAdvance.Text = "Total Receipt: " + totalReceipt.ToString("N2");
                }
                if (lblReceiptCount != null)
                {
                    lblReceiptCount.ForeColor = Color.FromArgb(255, 210, 120);
                    lblReceiptCount.Text = "Total Advance: " + totalAdv.ToString("N2")
                        + "  |  Count: " + cnt.ToString();
                }
            }
            catch (Exception ex)
            {
                if (lblTotalReceiptAmount != null) lblTotalReceiptAmount.Text = "Total Amount: —";
                if (lblTotalAdvance != null) lblTotalAdvance.Text = "Total Receipt: —";
                if (lblReceiptCount != null) lblReceiptCount.Text = "Total Advance: —";
                ShowError("Range totals failed.\n\n" + ex.Message);
            }
        }

        // -------- Columns --------
private void EnsureRuntimeColumns()
{
    try
    {
        var gv = guna2DataGridView1; if (gv == null) return;
        gv.AutoGenerateColumns = false;

        if (!gv.Columns.Contains("dgvRowType"))
        {
            gv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "dgvRowType",
                HeaderText = "RowType",
                DataPropertyName = "row_type",
                Visible = false
            });
        }

        // --- dgvReceiptId: header text ONLY -> "AutoNo" (binding as-is) ---
        if (!gv.Columns.Contains("dgvReceiptId"))
        {
            gv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "dgvReceiptId",
                HeaderText = "AutoNo",              // <- rename only
                DataPropertyName = "ref_no",        // aapke current code ke mutabiq
                Visible = false
            });
        }
        else
        {
            var col = gv.Columns["dgvReceiptId"] as DataGridViewTextBoxColumn;
            if (col != null)
            {
                col.HeaderText = "AutoNo";          // <- rename only
                // binding ko nahi chher rahe; aap ne pehle hi ref_no set kiya hua hai
                // col.DataPropertyName = col.DataPropertyName; // no-op
            }
        }

        if (!gv.Columns.Contains("dgvCustMobile"))
        {
            gv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "dgvCustMobile",
                HeaderText = "Mobile",
                DataPropertyName = "customer_mobile",
                Visible = false
            });
        }

                // --- dgvRefNo: header text ONLY -> "ReceiptNo" (binding jaisa hai waisa hi rahe)
                if (gv.Columns.Contains("dgvRefNo"))
                {
                    var refCol = gv.Columns["dgvRefNo"] as DataGridViewTextBoxColumn;
                    if (refCol != null) refCol.HeaderText = "ReceiptNo";
                }


                if (!gv.Columns.Contains("dgvedit"))
        {
            gv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "dgvedit",
                HeaderText = "Edit",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                Width = 78,
                FlatStyle = FlatStyle.Flat
            });
        }
        if (!gv.Columns.Contains("dgvdel"))
        {
            gv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "dgvdel",
                HeaderText = "Delete",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 88,
                FlatStyle = FlatStyle.Flat
            });
        }

        if (!gv.Columns.Contains("dgvwhtsapp"))
        {
            gv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "dgvwhtsapp",
                HeaderText = "WhatsApp",
                Text = "Send",
                UseColumnTextForButtonValue = true,
                Width = 110,
                FlatStyle = FlatStyle.Flat
            });
        }

        if (gv.Columns.Contains("dgvsms"))
            gv.Columns.Remove("dgvsms");
    }
    catch (Exception ex)
    {
        ShowError("Ensure columns failed.\n\n" + ex.Message);
    }
}

        private void ReorderActionColumnsAndLock()
        {
            var gv = guna2DataGridView1; if (gv == null) return;

            if (gv.Columns.Contains("dgvedit"))
                gv.Columns["dgvedit"].DisplayIndex = gv.Columns.Count - 3;
            if (gv.Columns.Contains("dgvdel"))
                gv.Columns["dgvdel"].DisplayIndex = gv.Columns.Count - 2;
            if (gv.Columns.Contains("dgvwhtsapp"))
                gv.Columns["dgvwhtsapp"].DisplayIndex = gv.Columns.Count - 1;

            foreach (DataGridViewRow r in gv.Rows)
            {
                var rt = Convert.ToString(r.Cells["dgvRowType"]?.Value);
                bool summary = string.Equals(rt, "OPENING", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(rt, "TOTAL", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(rt, "CLOSING", StringComparison.OrdinalIgnoreCase);
                if (summary)
                {
                    if (gv.Columns.Contains("dgvedit")) r.Cells["dgvedit"].ReadOnly = true;
                    if (gv.Columns.Contains("dgvdel")) r.Cells["dgvdel"].ReadOnly = true;
                }
            }
        }

        // -------- Toolbar: Add (single-open) --------
        public override void btnAdd_Click(object sender, EventArgs e)
        {
            if (MainClass.UiClicksBlocked) return;

            // millisecond throttle
            var now = DateTime.UtcNow;
            if ((now - _lastAddClick).TotalMilliseconds < 500) return;
            _lastAddClick = now;

            // strong CAS guard (prevents double-open from any source)
            if (Interlocked.Exchange(ref _addOpenFlag, 1) == 1) return;

            bool openedDialog = false;
            try
            {
                if (_dialogOpen) return;

                if ((_isRefMode || _isCustMode) && _currentCustomerPlotId <= 0)
                {
                    FocusVipAllocationFinder();
                    ShowInfo("Left list se customer select karein.");
                    return;
                }
                if (_currentCustomerPlotId <= 0)
                {
                    FocusVipAllocationFinder();
                    ShowInfo("Pehle left se customer / allocation select karein.");
                    return;
                }

                _dialogOpen = true;
                openedDialog = true;
                btnAdd.Enabled = false;
                try
                {
                    using (var dlg = new frmReciptfromAdd { CustomerPlotId = _currentCustomerPlotId })
                    {
                        var res = dlg.ShowDialog(this); // modal
                        if (res == DialogResult.OK)
                        {
                            if (_isRefMode && !string.IsNullOrWhiteSpace(_refQuery)) TryBindRefReceipts(_refQuery);
                            else if (_isCustMode && !string.IsNullOrWhiteSpace(_custQuery)) TryBindCustomerReceipts(_custQuery);
                            else GetData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Add dialog failed.\n\n" + ex.Message);
                }
                finally
                {
                    btnAdd.Enabled = true;
                    _dialogOpen = false;
                }
            }
            finally
            {
                // Close ke baad leftover click se Add dobara na khule
                if (openedDialog)
                    ReleaseAddGuardSoon();
                else
                    Interlocked.Exchange(ref _addOpenFlag, 0);
            }
        }

        private void ReleaseAddGuardSoon()
        {
            void release()
            {
                _lastAddClick = DateTime.UtcNow;
                Interlocked.Exchange(ref _addOpenFlag, 0);
            }

            try
            {
                if (!IsHandleCreated)
                {
                    release();
                    return;
                }

                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        var t = new System.Windows.Forms.Timer { Interval = 350 };
                        t.Tick += (s, ev) =>
                        {
                            try { t.Stop(); t.Dispose(); } catch { }
                            release();
                        };
                        t.Start();
                    }
                    catch
                    {
                        release();
                    }
                }));
            }
            catch
            {
                release();
            }
        }

        // -------- Core Ledger: sirf SELECTED plot, saari dates (doosra plot mix nahi) --------
        public void GetData()
        {
            try
            {
                if (_currentCustomerPlotId <= 0) { guna2DataGridView1.DataSource = null; UpdateRangeTotals(); return; }
                if (_isRefMode || _isCustMode) return; // global modes only via explicit calls, NOT txtSearch

                EnsureRuntimeColumns();

                // IMPORTANT: only @cpid — selected plot ki SAARI receipts (search box se hide nahi)
                string sql = @"
WITH alloc_cust AS (
    SELECT c.mobile AS customer_mobile,
           CAST(p.plot_no AS TEXT) AS plot_no
    FROM customer_plots cp
    JOIN customers c ON c.id = cp.customer_id
    JOIN plots     p ON p.id = cp.plot_id
    WHERE cp.id = @cpid
),
open_val AS (
    SELECT CASE 
             WHEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=@cpid) > 0 
                  THEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=@cpid)
             ELSE (SELECT COALESCE(price,0) FROM customer_plots WHERE id=@cpid)
           END AS opening
),
receipts_filtered AS (
    SELECT r.id, r.receipt_date, r.amount, COALESCE(r.notes,'') AS notes, COALESCE(r.ref_no,'') AS refno
    FROM receipts r
    WHERE r.customer_plot_id = @cpid
),
tot AS (
    SELECT COALESCE(SUM(amount),0) AS total_credit FROM receipts_filtered
)
SELECT * FROM (
    SELECT 'OPENING' AS row_type, NULL AS receipt_id, '' AS date,
           ('Opening (Debit) — Plot ' || (SELECT plot_no FROM alloc_cust)) AS description,
           (SELECT opening FROM open_val) AS debit, 0 AS credit,
           (SELECT customer_mobile FROM alloc_cust) AS customer_mobile,
           '' AS refno
    UNION ALL
    SELECT 'RECEIPT' AS row_type, rf.id AS receipt_id, rf.receipt_date AS date,
           CASE WHEN rf.notes<>'' THEN rf.notes ELSE 'Receipt' END AS description,
           0 AS debit, rf.amount AS credit,
           (SELECT customer_mobile FROM alloc_cust) AS customer_mobile,
           rf.refno AS refno
    FROM receipts_filtered rf
    UNION ALL
    SELECT 'TOTAL' AS row_type, NULL, '',
           ('Total Credit — Plot ' || (SELECT plot_no FROM alloc_cust)), 0,
           (SELECT total_credit FROM tot),
           (SELECT customer_mobile FROM alloc_cust),
           '' AS refno
    UNION ALL
    SELECT 'CLOSING' AS row_type, NULL, '',
           ('Closing Balance — Plot ' || (SELECT plot_no FROM alloc_cust)),
           (SELECT opening FROM open_val),
           (SELECT total_credit FROM tot),
           (SELECT customer_mobile FROM alloc_cust),
           '' AS refno
) x
ORDER BY
  CASE row_type WHEN 'OPENING' THEN 0 WHEN 'RECEIPT' THEN 1 WHEN 'TOTAL' THEN 2 ELSE 3 END,
  date(date) ASC,
  receipt_id ASC;";

                var prms = new Dictionary<string, object> {
                    {"@cpid", _currentCustomerPlotId}
                };

                ListBox lb = new ListBox();
                lb.Items.Add(dgvRowType);
                lb.Items.Add(dgvReceiptId);
                lb.Items.Add(dgvDate);
                lb.Items.Add(dgvDescription);
                lb.Items.Add(dgvDebit);
                lb.Items.Add(dgvCredit);

                MainClass.LoadData1(sql, guna2DataGridView1, lb, prms);

                if (!string.IsNullOrWhiteSpace(_currentAllocationDisp))
                    this.Text = "Receipt View — " + _currentAllocationDisp;

                UpdateRangeTotals();
            }
            catch (Exception ex)
            {
                ShowError("Fetching ledger failed.\n\n" + ex.Message);
            }
        }

        // -------- Global search APIs (intact, but txtSearch inko trigger NAHIN karta) --------
        public void SmartSearchExternal(string query, bool enterPressed = false)
        {
            try
            {
                SmartSearchCore(query?.Trim() ?? string.Empty, enterPressed);
            }
            catch (Exception ex)
            {
                ShowError("Smart search failed.\n\n" + ex.Message);
            }
        }

        private void SmartSearchCore(string q, bool enterPressed)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                _isRefMode = _isCustMode = false;
                _refQuery = _custQuery = "";
                GetData(); return;
            }

            // GLOBAL modes — ab summaries bhi dikhengi
            if (TryBindRefReceipts(q)) { _isRefMode = true; _isCustMode = false; _refQuery = q; _custQuery = ""; return; }
            if (TryBindCustomerReceipts(q)) { _isRefMode = false; _isCustMode = true; _refQuery = ""; _custQuery = q; return; }

            _isRefMode = _isCustMode = false; _refQuery = _custQuery = "";

            var dt = MainClass.ExecuteSelectQuery1(@"
SELECT cp.id AS id, c.name || ' | Plot ' || p.plot_no AS disp
FROM customer_plots cp
JOIN customers c ON c.id=cp.customer_id
JOIN plots     p ON p.id=cp.plot_id
WHERE cp.status='Active'
  AND (LOWER(c.name) LIKE '%'||LOWER(@q)||'%' OR LOWER(p.plot_no) LIKE '%'||LOWER(@q)||'%')
ORDER BY c.name, p.plot_no;", new Dictionary<string, object> { { "@q", q } });

            if (dt == null || dt.Rows.Count == 0) return;

            if (dt.Rows.Count == 1)
            {
                _currentCustomerPlotId = Convert.ToInt32(dt.Rows[0]["id"]);
                _currentAllocationDisp = Convert.ToString(dt.Rows[0]["disp"]);
                try { if (txtSearch != null) txtSearch.Text = ""; } catch { }
                this.Text = "Receipt View — " + _currentAllocationDisp;
                GetData(); return;
            }

            if (enterPressed)
            {
                int pickedId; string pickedDisp;
                if (ShowPickerForTable(dt, out pickedId, out pickedDisp))
                {
                    _currentCustomerPlotId = pickedId;
                    _currentAllocationDisp = pickedDisp;
                    try { if (txtSearch != null) txtSearch.Text = ""; } catch { }
                    this.Text = "Receipt View — " + _currentAllocationDisp;
                    GetData();
                }
            }
        }

        // -------- Global REF search with summaries --------
        private bool TryBindRefReceipts(string q)
        {
            try
            {
                EnsureRuntimeColumns();
                int societyId = GetVipSelectedSocietyId();
                var prms = new Dictionary<string, object> {
                    { "@q", q },
                    { "@sid", societyId }
                };

                string sqlRef = @"
WITH matches AS (
  SELECT r.id, r.receipt_date, r.amount, COALESCE(r.notes,'') AS notes, COALESCE(r.ref_no,'') AS refno,
         cp.id AS cp_id, c.name AS cust_name, p.plot_no AS plot_no, c.mobile AS customer_mobile
  FROM receipts r
  JOIN customer_plots cp ON cp.id=r.customer_plot_id
  JOIN customers c ON c.id=cp.customer_id
  JOIN plots     p ON p.id=cp.plot_id
  WHERE COALESCE(r.ref_no,'') <> '' AND LOWER(r.ref_no) LIKE '%'||LOWER(@q)||'%'
    AND (@sid = 0 OR p.society_id = @sid)
),
per_alloc AS (
  SELECT cp_id,
         MAX(customer_mobile) AS customer_mobile,
         MAX(cust_name) AS cust_name,
         MAX(plot_no) AS plot_no
  FROM matches
  GROUP BY cp_id
),
open_val AS (
  SELECT pa.cp_id,
         CASE 
           WHEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=pa.cp_id) > 0 
                THEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=pa.cp_id)
           ELSE (SELECT COALESCE(price,0) FROM customer_plots WHERE id=pa.cp_id)
         END AS opening
  FROM per_alloc pa
),
tot AS (
  SELECT m.cp_id, COALESCE(SUM(m.amount),0) AS total_credit
  FROM matches m
  GROUP BY m.cp_id
)
SELECT * FROM (
  -- opening per allocation
  SELECT 'OPENING' AS row_type, NULL AS receipt_id, '' AS date,
         ('Opening (Debit) — '||pa.cust_name||' | Plot '||pa.plot_no) AS description,
         ov.opening AS debit, 0 AS credit,
         pa.customer_mobile AS customer_mobile,
         '' AS refno,
         pa.cust_name AS sort_cust, pa.plot_no AS sort_plot, 0 AS sort_row
  FROM per_alloc pa
  JOIN open_val ov ON ov.cp_id=pa.cp_id

  UNION ALL
  -- receipts
  SELECT 'RECEIPT' AS row_type, m.id AS receipt_id, m.receipt_date AS date,
         (m.cust_name || ' | Plot ' || m.plot_no || ' : ' ||
          CASE WHEN m.notes<>'' THEN m.notes ELSE 'Receipt' END ||
          CASE WHEN m.refno<>'' THEN ' [Ref:'||m.refno||']' ELSE '' END) AS description,
         0 AS debit, m.amount AS credit,
         m.customer_mobile AS customer_mobile,
         m.refno AS refno,
         m.cust_name AS sort_cust, m.plot_no AS sort_plot, 1 AS sort_row
  FROM matches m

  UNION ALL
  -- total per allocation
  SELECT 'TOTAL' AS row_type, NULL, '', 
         ('Total Credit — '||pa.cust_name||' | Plot '||pa.plot_no), 
         0, t.total_credit,
         pa.customer_mobile, '',
         pa.cust_name, pa.plot_no, 2
  FROM per_alloc pa
  JOIN tot t ON t.cp_id=pa.cp_id

  UNION ALL
  -- closing per allocation
  SELECT 'CLOSING' AS row_type, NULL, '', 
         ('Closing Balance (Debit - Credit) — '||pa.cust_name||' | Plot '||pa.plot_no),
         ov.opening, t.total_credit,
         pa.customer_mobile, '',
         pa.cust_name, pa.plot_no, 3
  FROM per_alloc pa
  JOIN open_val ov ON ov.cp_id=pa.cp_id
  JOIN tot t ON t.cp_id=pa.cp_id
) x
ORDER BY sort_cust, sort_plot,
         CASE row_type WHEN 'OPENING' THEN 0 WHEN 'RECEIPT' THEN 1 WHEN 'TOTAL' THEN 2 ELSE 3 END,
         date(date) ASC,
         receipt_id ASC;";

                ListBox lb = new ListBox();
                lb.Items.Add(dgvRowType);
                lb.Items.Add(dgvReceiptId);
                lb.Items.Add(dgvDate);
                lb.Items.Add(dgvDescription);
                lb.Items.Add(dgvDebit);
                lb.Items.Add(dgvCredit);

                MainClass.LoadData1(sqlRef, guna2DataGridView1, lb, prms);
                UpdateRangeTotals();

                int rows = guna2DataGridView1.Rows.Count;
                if (rows > 0)
                {
                    this.Text = "Receipt View — Ref: " + q + "  (" + rows + " rows incl. summaries)";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ShowError("Ref search failed.\n\n" + ex.Message);
                return false;
            }
        }

        // -------- Global CUSTOMER name search with summaries --------
        private bool TryBindCustomerReceipts(string q)
        {
            try
            {
                EnsureRuntimeColumns();
                int societyId = GetVipSelectedSocietyId();
                string sqlCust = @"
WITH allocs AS (
  SELECT cp.id AS cp_id, c.name AS cust_name, p.plot_no AS plot_no, c.mobile AS customer_mobile
  FROM customer_plots cp
  JOIN customers c ON c.id=cp.customer_id
  JOIN plots     p ON p.id=cp.plot_id
  WHERE LOWER(c.name) LIKE '%'||LOWER(@q)||'%'
    AND IFNULL(cp.status,'') <> 'Cancelled'
    AND (@sid = 0 OR p.society_id = @sid)
),
receipts_filtered AS (
  SELECT r.id, r.receipt_date, r.amount, COALESCE(r.notes,'') AS notes, COALESCE(r.ref_no,'') AS refno,
         a.cp_id, a.cust_name, a.plot_no, a.customer_mobile
  FROM receipts r
  JOIN allocs a ON a.cp_id = r.customer_plot_id
),
open_val AS (
  SELECT a.cp_id,
         CASE 
           WHEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=a.cp_id) > 0 
                THEN (SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=a.cp_id)
           ELSE (SELECT COALESCE(price,0) FROM customer_plots WHERE id=a.cp_id)
         END AS opening
  FROM allocs a
),
tot AS (
  SELECT rf.cp_id, COALESCE(SUM(rf.amount),0) AS total_credit
  FROM receipts_filtered rf
  GROUP BY rf.cp_id
)
SELECT * FROM (
  SELECT 'OPENING' AS row_type, NULL AS receipt_id, '' AS date,
         ('Opening (Debit) — '||a.cust_name||' | Plot '||a.plot_no) AS description,
         ov.opening AS debit, 0 AS credit,
         a.customer_mobile AS customer_mobile,
         '' AS refno,
         a.cust_name AS sort_cust, a.plot_no AS sort_plot, 0 AS sort_row
  FROM allocs a
  JOIN open_val ov ON ov.cp_id=a.cp_id

  UNION ALL
  SELECT 'RECEIPT' AS row_type, rf.id AS receipt_id, rf.receipt_date AS date,
         (rf.cust_name || ' | Plot ' || rf.plot_no || ' : ' ||
          CASE WHEN rf.notes<>'' THEN rf.notes ELSE 'Receipt' END ||
          CASE WHEN rf.refno<>'' THEN ' [Ref:'||rf.refno||']' ELSE '' END
         ) AS description,
         0 AS debit, rf.amount AS credit,
         rf.customer_mobile AS customer_mobile,
         rf.refno AS refno,
         rf.cust_name AS sort_cust, rf.plot_no AS sort_plot, 1 AS sort_row
  FROM receipts_filtered rf

  UNION ALL
  SELECT 'TOTAL' AS row_type, NULL, '', 
         ('Total Credit — '||a.cust_name||' | Plot '||a.plot_no),
         0, t.total_credit,
         a.customer_mobile, '',
         a.cust_name, a.plot_no, 2
  FROM allocs a
  LEFT JOIN tot t ON t.cp_id=a.cp_id

  UNION ALL
  SELECT 'CLOSING' AS row_type, NULL, '',
         ('Closing Balance (Debit - Credit) — '||a.cust_name||' | Plot '||a.plot_no),
         ov.opening, COALESCE(t.total_credit,0),
         a.customer_mobile, '',
         a.cust_name, a.plot_no, 3
  FROM allocs a
  JOIN open_val ov ON ov.cp_id=a.cp_id
  LEFT JOIN tot t ON t.cp_id=a.cp_id
) x
ORDER BY sort_cust, sort_plot,
         CASE row_type WHEN 'OPENING' THEN 0 WHEN 'RECEIPT' THEN 1 WHEN 'TOTAL' THEN 2 ELSE 3 END,
         date(date) ASC,
         receipt_id ASC;";

                var prms = new Dictionary<string, object>
                {
                    { "@q", q },
                    { "@sid", societyId }
                };

                ListBox lb = new ListBox();
                lb.Items.Add(dgvRowType);
                lb.Items.Add(dgvReceiptId);
                lb.Items.Add(dgvDate);
                lb.Items.Add(dgvDescription);
                lb.Items.Add(dgvDebit);
                lb.Items.Add(dgvCredit);

                MainClass.LoadData1(sqlCust, guna2DataGridView1, lb, prms);
                UpdateRangeTotals();

                int rows = guna2DataGridView1.Rows.Count;
                if (rows > 0)
                {
                    this.Text = "Receipt View — Customer: " + q + "  (" + rows + " rows incl. summaries)";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ShowError("Customer search failed.\n\n" + ex.Message);
                return false;
            }
        }

        // -------- Allocation picker --------
        private void EnsureAllocationSelected()
        {
            try
            {
                if (_currentCustomerPlotId > 0) return;
                // Prefer VIP inline finder — no forced popup on open
                FocusVipAllocationFinder();
            }
            catch (Exception ex)
            {
                ShowError("Allocation selection failed.\n\n" + ex.Message);
            }
        }

        private void FocusVipAllocationFinder()
        {
            try
            {
                EnsureVipAllocationFinder();
                if (_txtVipAllocSearch != null && !_txtVipAllocSearch.IsDisposed)
                {
                    _txtVipAllocSearch.Focus();
                    _txtVipAllocSearch.SelectAll();
                }
            }
            catch { }
        }

        private void EnsureVipAllocationFinder()
        {
            try
            {
                if (_vipAllocPanel != null && !_vipAllocPanel.IsDisposed) return;

                _vipAllocPanel = new Panel
                {
                    Name = "vipAllocPanel",
                    Width = VipAllocPanelWidth,
                    BackColor = Color.FromArgb(28, 32, 58),
                    Padding = new Padding(10, 8, 10, 8),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
                };

                var header = new Label
                {
                    Text = "Find Customer",
                    AutoSize = false,
                    Height = 26,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                _lblVipSociety = new Label
                {
                    Text = "Society (important)",
                    AutoSize = false,
                    Height = 20,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 200, 120),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                _cmbVipSociety = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 10f),
                    FlatStyle = FlatStyle.Flat,
                    IntegralHeight = false,
                    MaxDropDownItems = 12
                };

                _lblVipHint = new Label
                {
                    Text = "Search: Name · Plot · Block · Type · CNIC · Mobile",
                    AutoSize = false,
                    Height = 32,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(180, 190, 210),
                    TextAlign = ContentAlignment.TopLeft
                };

                _txtVipAllocSearch = new TextBox
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Top,
                    Height = 28,
                    Font = new Font("Segoe UI", 10.5f),
                    BackColor = Color.FromArgb(37, 41, 74),
                    ForeColor = Color.White
                };

                _lblVipSelected = new Label
                {
                    Text = "Selected: (none)",
                    AutoSize = false,
                    Height = 48,
                    Dock = DockStyle.Bottom,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(120, 220, 180),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                _dgvVipAlloc = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    AutoGenerateColumns = false,
                    MultiSelect = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    BorderStyle = BorderStyle.None,
                    BackgroundColor = Color.FromArgb(32, 36, 66),
                    GridColor = Color.FromArgb(50, 56, 76),
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersHeight = 34,
                    RowTemplate = { Height = 30 }
                };
                _dgvVipAlloc.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 45, 95);
                _dgvVipAlloc.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                _dgvVipAlloc.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
                _dgvVipAlloc.DefaultCellStyle.BackColor = Color.FromArgb(32, 36, 66);
                _dgvVipAlloc.DefaultCellStyle.ForeColor = Color.White;
                _dgvVipAlloc.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 120, 246);
                _dgvVipAlloc.DefaultCellStyle.SelectionForeColor = Color.White;
                _dgvVipAlloc.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                _dgvVipAlloc.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                _dgvVipAlloc.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "id",
                    HeaderText = "ID",
                    DataPropertyName = "id",
                    Visible = false
                });
                _dgvVipAlloc.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "disp",
                    HeaderText = "Name | Plot | Society | Block",
                    DataPropertyName = "disp",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                // Dock order: Fill, Bottom, then Tops (last Top = visual top)
                _vipAllocPanel.Controls.Add(_dgvVipAlloc);
                _vipAllocPanel.Controls.Add(_lblVipSelected);
                _vipAllocPanel.Controls.Add(_txtVipAllocSearch);
                _vipAllocPanel.Controls.Add(_lblVipHint);
                _vipAllocPanel.Controls.Add(_cmbVipSociety);
                _vipAllocPanel.Controls.Add(_lblVipSociety);
                _vipAllocPanel.Controls.Add(header);

                this.Controls.Add(_vipAllocPanel);
                _vipAllocPanel.BringToFront();
                LayoutVipAllocationFinder();

                _cmbVipSociety.SelectedIndexChanged += (s, e) =>
                {
                    try
                    {
                        // Allocate jaisa: society change → list filter + wrong plot clear + totals
                        ApplyVipAllocationFilter(true);
                        ClearSelectionIfOutsideSocietyFilter();
                        UpdateRangeTotals();
                    }
                    catch (Exception ex) { ShowError("Society filter failed.\n\n" + ex.Message); }
                };

                _txtVipAllocSearch.TextChanged += (s, e) =>
                {
                    try { ApplyVipAllocationFilter(true); }
                    catch (Exception ex) { ShowError("Search filter failed.\n\n" + ex.Message); }
                };

                _txtVipAllocSearch.KeyDown += (s, e) =>
                {
                    try
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.SuppressKeyPress = true;
                            if (_dgvVipAlloc != null && _dgvVipAlloc.CurrentRow != null)
                                ApplyVipAllocationFromRow(_dgvVipAlloc.CurrentRow);
                            else if (_vipAllocView != null && _vipAllocView.Count == 1)
                                ApplyVipAllocationFromViewIndex(0);
                        }
                        else if (e.KeyCode == Keys.Down && _dgvVipAlloc != null && _dgvVipAlloc.Rows.Count > 0)
                        {
                            e.SuppressKeyPress = true;
                            _dgvVipAlloc.Focus();
                            if (_dgvVipAlloc.CurrentRow == null)
                                _dgvVipAlloc.Rows[0].Selected = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError("Search key failed.\n\n" + ex.Message);
                    }
                };

                _dgvVipAlloc.CellClick += (s, e) =>
                {
                    try
                    {
                        if (e.RowIndex >= 0)
                            ApplyVipAllocationFromRow(_dgvVipAlloc.Rows[e.RowIndex]);
                    }
                    catch (Exception ex) { ShowError("Select failed.\n\n" + ex.Message); }
                };
                _dgvVipAlloc.CellDoubleClick += (s, e) =>
                {
                    try
                    {
                        if (e.RowIndex >= 0)
                            ApplyVipAllocationFromRow(_dgvVipAlloc.Rows[e.RowIndex]);
                    }
                    catch (Exception ex) { ShowError("Select failed.\n\n" + ex.Message); }
                };
                _dgvVipAlloc.KeyDown += (s, e) =>
                {
                    try
                    {
                        if (e.KeyCode == Keys.Enter && _dgvVipAlloc.CurrentRow != null)
                        {
                            e.SuppressKeyPress = true;
                            ApplyVipAllocationFromRow(_dgvVipAlloc.CurrentRow);
                        }
                    }
                    catch (Exception ex) { ShowError("Select failed.\n\n" + ex.Message); }
                };

                this.Resize -= VipAllocPanel_Resize;
                this.Resize += VipAllocPanel_Resize;
            }
            catch (Exception ex)
            {
                ShowError("Customer finder setup failed.\n\n" + ex.Message);
            }
        }

        private void VipAllocPanel_Resize(object sender, EventArgs e)
        {
            try { LayoutVipAllocationFinder(); }
            catch { }
        }

        private void LayoutVipAllocationFinder()
        {
            try
            {
                if (_vipAllocPanel == null || _vipAllocPanel.IsDisposed) return;
                int top = (panel1 != null) ? panel1.Bottom + 2 : 90;
                _vipAllocPanel.Location = new Point(0, top);
                _vipAllocPanel.Width = VipAllocPanelWidth;
                _vipAllocPanel.Height = Math.Max(120, this.ClientSize.Height - top - 4);

                if (guna2DataGridView1 != null)
                {
                    int gap = 8;
                    int gridTop = Math.Max(guna2DataGridView1.Top, top);
                    guna2DataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    guna2DataGridView1.Left = VipAllocPanelWidth + gap;
                    guna2DataGridView1.Top = gridTop;
                    guna2DataGridView1.Width = Math.Max(200, this.ClientSize.Width - VipAllocPanelWidth - gap - 12);
                    guna2DataGridView1.Height = Math.Max(120, this.ClientSize.Height - gridTop - 8);
                }
            }
            catch { }
        }

        private void LoadVipSocieties()
        {
            try
            {
                if (_cmbVipSociety == null) return;

                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Rows.Add(0, "(All Societies)");

                var sdt = MainClass.ExecuteSelectQuery1(
                    "SELECT id, name FROM societies ORDER BY name;", null);
                if (sdt != null)
                {
                    foreach (DataRow r in sdt.Rows)
                    {
                        try
                        {
                            dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                        }
                        catch { }
                    }
                }

                _cmbVipSociety.DisplayMember = "name";
                _cmbVipSociety.ValueMember = "id";
                _cmbVipSociety.DataSource = dt;
                if (_cmbVipSociety.Items.Count > 0)
                    _cmbVipSociety.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("Society list load failed.\n\n" + ex.Message);
            }
        }

        private int GetVipSelectedSocietyId()
        {
            try
            {
                if (_cmbVipSociety == null || _cmbVipSociety.SelectedValue == null || _cmbVipSociety.SelectedValue == DBNull.Value)
                    return 0;
                return Convert.ToInt32(_cmbVipSociety.SelectedValue);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>Selected plot dusri society ka ho to clear — society filter mix na dikhe.</summary>
        private void ClearSelectionIfOutsideSocietyFilter()
        {
            try
            {
                int societyId = GetVipSelectedSocietyId();
                if (societyId <= 0 || _currentCustomerPlotId <= 0) return;

                bool stillVisible = false;
                if (_vipAllocView != null)
                {
                    for (int i = 0; i < _vipAllocView.Count; i++)
                    {
                        try
                        {
                            if (Convert.ToInt32(_vipAllocView[i]["id"]) == _currentCustomerPlotId)
                            {
                                stillVisible = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (stillVisible) return;

                _currentCustomerPlotId = 0;
                _currentAllocationDisp = "";
                _isRefMode = _isCustMode = false;
                _refQuery = _custQuery = "";
                try { if (_lblVipSelected != null) _lblVipSelected.Text = "Selected: (none)"; } catch { }
                this.Text = "Receipt View — type name to find customer";
                try { guna2DataGridView1.DataSource = null; } catch { }
            }
            catch { }
        }

        private void LoadVipAllocations()
        {
            try
            {
                EnsureVipAllocationFinder();
                LoadVipSocieties();

                // Same Active allocations — extra columns only for VIP find (CNIC/Mobile/Type/Society)
                _vipAllocTable = MainClass.ExecuteSelectQuery1(@"
SELECT cp.id AS id,
       c.name || ' | Plot ' || p.plot_no || ' | ' || COALESCE(s.name,'') || ' | ' || COALESCE(b.name,'') ||
         CASE WHEN COALESCE(pt.code,'')<>'' THEN ' | ' || pt.code ELSE '' END AS disp,
       COALESCE(c.name,'') AS cust,
       CAST(p.plot_no AS TEXT) AS plotno,
       COALESCE(s.name,'') AS society,
       COALESCE(b.name,'') AS block,
       COALESCE(pt.code,'') AS plottype,
       COALESCE(c.cnic,'') AS cnic,
       COALESCE(c.mobile,'') AS mobile,
       COALESCE(p.society_id, 0) AS society_id
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots     p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
JOIN blocks    b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.status = 'Active'
ORDER BY s.name, c.name, p.plot_no;", null);

                if (_vipAllocTable == null)
                    _vipAllocTable = new DataTable();

                _vipAllocTable.CaseSensitive = false;
                _vipAllocView = new DataView(_vipAllocTable);
                if (_dgvVipAlloc != null)
                    _dgvVipAlloc.DataSource = _vipAllocView;

                ApplyVipAllocationFilter(false);

                // Only auto-pick when exactly one row after filters
                if (_vipAllocView != null && _vipAllocView.Count == 1)
                    ApplyVipAllocationFromViewIndex(0);
            }
            catch (Exception ex)
            {
                ShowError("Customer list load failed.\n\n" + ex.Message);
            }
        }

        private void ApplyVipAllocationFilter(bool autoPickSingle)
        {
            if (_vipFilterBusy) return;
            _vipFilterBusy = true;
            try
            {
                if (_vipAllocView == null) return;

                int societyId = GetVipSelectedSocietyId();
                string q = EscapeRowFilter((_txtVipAllocSearch?.Text ?? "").Trim());

                var parts = new List<string>();
                if (societyId > 0)
                    parts.Add("society_id = " + societyId);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    // Name, plot, society, block, type, CNIC, mobile — multi-society same-name easy
                    parts.Add(
                        "(cust LIKE '%" + q + "%' OR " +
                        "plotno LIKE '%" + q + "%' OR " +
                        "society LIKE '%" + q + "%' OR " +
                        "block LIKE '%" + q + "%' OR " +
                        "plottype LIKE '%" + q + "%' OR " +
                        "cnic LIKE '%" + q + "%' OR " +
                        "mobile LIKE '%" + q + "%' OR " +
                        "disp LIKE '%" + q + "%')");
                }

                _vipAllocView.RowFilter = parts.Count == 0 ? "" : string.Join(" AND ", parts);

                if (autoPickSingle && _vipAllocView.Count == 1)
                    ApplyVipAllocationFromViewIndex(0);
            }
            catch (Exception ex)
            {
                try { if (_vipAllocView != null) _vipAllocView.RowFilter = ""; } catch { }
                ShowError("Filter failed.\n\n" + ex.Message);
            }
            finally
            {
                _vipFilterBusy = false;
            }
        }

        private void ApplyVipAllocationFromViewIndex(int index)
        {
            try
            {
                if (_vipAllocView == null || index < 0 || index >= _vipAllocView.Count) return;
                var row = _vipAllocView[index];
                int id = Convert.ToInt32(row["id"]);
                string disp = Convert.ToString(row["disp"]);
                ApplyVipAllocationSelection(id, disp);
            }
            catch (Exception ex)
            {
                ShowError("Select failed.\n\n" + ex.Message);
            }
        }

        private void ApplyVipAllocationFromRow(DataGridViewRow row)
        {
            try
            {
                if (row == null || row.IsNewRow) return;
                object idObj = row.Cells["id"].Value;
                if (idObj == null || idObj == DBNull.Value) return;
                int id = Convert.ToInt32(idObj);
                string disp = Convert.ToString(row.Cells["disp"].Value);
                ApplyVipAllocationSelection(id, disp);
            }
            catch (Exception ex)
            {
                ShowError("Select failed.\n\n" + ex.Message);
            }
        }

        private void ApplyVipAllocationSelection(int customerPlotId, string display)
        {
            if (customerPlotId <= 0) return;
            if (_vipApplying) return;
            _vipApplying = true;
            try
            {
                // Same selection state as old picker — ledger logic unchanged
                _currentCustomerPlotId = customerPlotId;
                _currentAllocationDisp = display ?? "";
                _isRefMode = _isCustMode = false;
                _refQuery = _custQuery = "";
                try { if (txtSearch != null) txtSearch.Text = ""; } catch { }
                this.Text = "Receipt View — " + _currentAllocationDisp;
                if (_lblVipSelected != null)
                    _lblVipSelected.Text = "Selected: " + _currentAllocationDisp;
                GetData();
            }
            catch (Exception ex)
            {
                ShowError("Selection failed.\n\n" + ex.Message);
            }
            finally
            {
                _vipApplying = false;
            }
        }

        private static string EscapeRowFilter(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // DataView.RowFilter — special chars escape
            var sb = new System.Text.StringBuilder(s.Length * 2);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\'': sb.Append("''"); break;
                    case '[': sb.Append("[[]"); break;
                    case ']': sb.Append("[]]"); break;
                    case '%': sb.Append("[%]"); break;
                    case '*': sb.Append("[*]"); break;
                    case '#': sb.Append("[#]"); break;
                    case '\\': sb.Append("[\\]"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private bool ShowAllocationPicker(out int customerPlotId, out string display)
        {
            customerPlotId = 0; display = "";
            try
            {
                var dt = MainClass.ExecuteSelectQuery1(@"
SELECT cp.id AS id,
       c.name || ' | Plot ' || p.plot_no || ' | ' || s.name || ' | ' || b.name AS disp,
       c.name   AS cust,
       p.plot_no AS plotno,
       s.name   AS society,
       b.name   AS block
FROM customer_plots cp
JOIN customers c ON c.id=cp.customer_id
JOIN plots     p ON p.id=cp.plot_id
JOIN societies s ON s.id=p.society_id
JOIN blocks    b ON b.id=p.block_id
WHERE cp.status='Active'
ORDER BY c.name, p.plot_no;", null);

                if (dt == null || dt.Rows.Count == 0)
                {
                    ShowInfo("Active allocations not found.");
                    return false;
                }

                // make LIKE comparisons case-insensitive
                dt.CaseSensitive = false;
                var dv = new DataView(dt);

                var f = new Form
                {
                    Text = "Select Allocation",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Width = 760,
                    Height = 560,
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowInTaskbar = false,
                    BackColor = Color.FromArgb(32, 36, 61),
                    ForeColor = Color.White,
                    KeyPreview = true
                };

                var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(32, 36, 61) };
                var lbl = new Label
                {
                    Text = "Select Allocation",
                    AutoSize = false,
                    Dock = DockStyle.Left,
                    Width = 300,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0)
                };
                var tbSearch = new TextBox
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Right,
                    Width = 340,
                    Margin = new Padding(12),
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Regular)
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
                    DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold) }
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
                    var q = EscapeRowFilter(tbSearch.Text?.Trim() ?? "");
                    dv.RowFilter = string.IsNullOrWhiteSpace(q)
                        ? ""
                        : $"cust LIKE '%{q}%' OR plotno LIKE '%{q}%' OR society LIKE '%{q}%' OR block LIKE '%{q}%'";
                };
                tbSearch.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; f.DialogResult = DialogResult.OK; }
                };

                gv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) f.DialogResult = DialogResult.OK; };
                btnOk.Click += (s, e) => f.DialogResult = DialogResult.OK;
                btnCancel.Click += (s, e) => f.DialogResult = DialogResult.Cancel;

                DialogResult res;
                try { res = f.ShowDialog(this); }
                catch { res = f.ShowDialog(); }

                if (res == DialogResult.OK && gv.CurrentRow != null)
                {
                    if (!int.TryParse(Convert.ToString(gv.CurrentRow.Cells["id"].Value), out customerPlotId)
                        || customerPlotId <= 0)
                        return false;
                    display = Convert.ToString(gv.CurrentRow.Cells["disp"].Value);
                }
                f.Dispose();
                return customerPlotId > 0;
            }
            catch (Exception ex)
            {
                ShowError("Allocation picker failed.\n\n" + ex.Message);
                return false;
            }
        }


        private bool ShowPickerForTable(DataTable dt, out int id, out string disp)
        {
            int resultId = 0;
            string resultDisp = "";
            try
            {
                var dv = new DataView(dt);

                var f = new Form
                {
                    Text = "Select Allocation",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Width = 640,
                    Height = 520,
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowInTaskbar = false,
                    BackColor = Color.FromArgb(32, 36, 61),
                    ForeColor = Color.White,
                    KeyPreview = true
                };

                var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(32, 36, 61) };
                var lbl = new Label
                {
                    Text = "Select Allocation",
                    AutoSize = false,
                    Dock = DockStyle.Left,
                    Width = 260,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0)
                };
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
                gv.Columns.Add(new DataGridViewTextBoxColumn { Name = "disp", HeaderText = "Customer | Plot", DataPropertyName = "disp", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

                var footer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(32, 36, 61) };
                var flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8), Height = 50, AutoSize = true };
                var btnOk = new Button { Text = "Select", Width = 110, Height = 36, Margin = new Padding(8) };
                var btnCancel = new Button { Text = "Cancel", Width = 110, Height = 36, Margin = new Padding(8) };
                flow.Controls.Add(btnOk); flow.Controls.Add(btnCancel); footer.Controls.Add(flow);

                f.Controls.Add(gv);
                f.Controls.Add(footer);
                f.Controls.Add(header);
                f.AcceptButton = btnOk;
                f.CancelButton = btnCancel;

                gv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) f.DialogResult = DialogResult.OK; };
                btnOk.Click += (s, e) => f.DialogResult = DialogResult.OK;
                btnCancel.Click += (s, e) => f.DialogResult = DialogResult.Cancel;

                DialogResult res;
                try { res = f.ShowDialog(this); }
                catch { res = f.ShowDialog(); }

                if (res == DialogResult.OK && gv.CurrentRow != null)
                {
                    if (int.TryParse(Convert.ToString(gv.CurrentRow.Cells["id"].Value), out int parsedId)
                        && parsedId > 0)
                    {
                        resultId = parsedId;
                        resultDisp = Convert.ToString(gv.CurrentRow.Cells["disp"].Value);
                    }
                }
                f.Dispose();
            }
            catch (Exception ex)
            {
                ShowError("Generic picker failed.\n\n" + ex.Message);
            }

            id = resultId; disp = resultDisp;
            return id > 0;
        }

        // -------- Grid Buttons (single-fire) --------
        private void Grid_CellHandler(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dialogOpen) return;
            if (_gridActionGuard) return;

            _gridActionGuard = true;
            try
            {
                var gv = guna2DataGridView1;
                var col = gv.Columns[e.ColumnIndex].Name;

                if (col == "dgvedit")
                {
                    var rt = Convert.ToString(gv.Rows[e.RowIndex].Cells["dgvRowType"]?.Value);
                    if (!string.Equals(rt, "RECEIPT", StringComparison.OrdinalIgnoreCase)) return;

                    if (!gv.Columns.Contains("dgvReceiptId")) { ShowError("dgvReceiptId missing."); return; }
                    if (!long.TryParse(Convert.ToString(gv.Rows[e.RowIndex].Cells["dgvReceiptId"]?.Value), out var rid) || rid <= 0)
                        return;

                    if (_dialogOpen) return;
                    _dialogOpen = true;
                    try
                    {
                        using (var dlg = new frmReciptfromAdd { EditingReceiptId = rid })
                        {
                            var res = dlg.ShowDialog(this);
                            if (res == DialogResult.OK)
                            {
                                if (_isRefMode && !string.IsNullOrWhiteSpace(_refQuery)) TryBindRefReceipts(_refQuery);
                                else if (_isCustMode && !string.IsNullOrWhiteSpace(_custQuery)) TryBindCustomerReceipts(_custQuery);
                                else GetData();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError("Edit dialog failed.\n\n" + ex.Message);
                    }
                    finally { _dialogOpen = false; }
                    return;
                }

                if (col == "dgvdel")
                {
                    var rt = Convert.ToString(gv.Rows[e.RowIndex].Cells["dgvRowType"]?.Value);
                    if (!string.Equals(rt, "RECEIPT", StringComparison.OrdinalIgnoreCase)) return;

                    if (!gv.Columns.Contains("dgvReceiptId")) { ShowError("dgvReceiptId missing."); return; }
                    if (!long.TryParse(Convert.ToString(gv.Rows[e.RowIndex].Cells["dgvReceiptId"]?.Value), out var rid) || rid <= 0)
                        return;

                    if (!AskYesNo("Delete this receipt?", "Confirm")) return;

                    long? ledgerId = null;
                    try
                    {
                        var obj = MainClass.ExecuteScalar("SELECT ledger_id FROM receipts WHERE id=@id;",
                            new Dictionary<string, object> { { "@id", rid } });
                        if (obj != null && obj != DBNull.Value) ledgerId = Convert.ToInt64(obj);
                    }
                    catch (Exception ex)
                    {
                        ShowError("Pre-delete fetch failed.\n\n" + ex.Message);
                        return;
                    }

                    var cascade = HubSyncService.CollectReceiptCascade(rid, ledgerId);

                    SQLiteTransaction tx = null;
                    try
                    {
                        HubBuilders.MainClass.AcquireSharedConnection();
                        using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", HubBuilders.MainClass.con))
                            pragma.ExecuteNonQuery();

                        tx = HubBuilders.MainClass.con.BeginTransaction();
                        // pending_delete + local deletes EK transaction; SoftMark HTTP bahar
                        HubSyncService.QueuePendingDeletesForCascade(HubBuilders.MainClass.con, tx, cascade);

                        HubSyncService.DeletePartnerMoneyWhere(
                            HubBuilders.MainClass.con, tx,
                            "receipt_id=@rid",
                            cmd => cmd.Parameters.AddWithValue("@rid", rid),
                            softMarkCloud: false);

                        using (var cmd = new SQLiteCommand("DELETE FROM receipts WHERE id=@id;", HubBuilders.MainClass.con, tx))
                        { cmd.Parameters.AddWithValue("@id", rid); cmd.ExecuteNonQuery(); }

                        if (ledgerId.HasValue)
                        {
                            using (var cmd = new SQLiteCommand("DELETE FROM ledger WHERE id=@lid;", HubBuilders.MainClass.con, tx))
                            { cmd.Parameters.AddWithValue("@lid", ledgerId.Value); cmd.ExecuteNonQuery(); }
                        }

                        tx.Commit();

                        try { HubSyncService.SoftMarkQueuedCascade(cascade); } catch { }
                        try { HubSyncService.NotifyLocalWrite(); } catch { }
                        try { HubSyncService.RaiseLiveChanged(); } catch { }

                        ShowInfo("Receipt deleted successfully.");

                        if (_isRefMode && !string.IsNullOrWhiteSpace(_refQuery)) TryBindRefReceipts(_refQuery);
                        else if (_isCustMode && !string.IsNullOrWhiteSpace(_custQuery)) TryBindCustomerReceipts(_custQuery);
                        else GetData();
                    }
                    catch (Exception ex)
                    {
                        try { tx?.Rollback(); } catch { }
                        ShowError("Delete failed.\n\n" + ex.Message);
                    }
                    finally
                    {
                        HubBuilders.MainClass.ReleaseSharedConnection();
                    }
                    return;
                }

                if (col == "dgvwhtsapp")
                {
                    try
                    {
                        string mobile = Convert.ToString(gv.Rows[e.RowIndex].Cells["dgvCustMobile"]?.Value);
                        var digits = NormalizePhoneToDigits(mobile);
                        if (string.IsNullOrWhiteSpace(digits))
                        {
                            ShowInfo("Customer mobile not available.");
                            return;
                        }
                        var msg = BuildFullLedgerWhatsAppMessage();
                        OpenWhatsAppWithPrefill(digits, msg);
                    }
                    catch (Exception ex)
                    {
                        ShowError("WhatsApp open failed.\n\n" + ex.Message);
                    }
                }
            }
            finally
            {
                _gridActionGuard = false;
            }
        }

        // -------- Paints --------
        private void Grid_CellPainting_WA(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvwhtsapp") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, WaStart, WaEnd, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(0, 100, 0)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("WhatsApp", font, new SolidBrush(WaText), rect, sf);
                }
            }
            catch
            {
                // Never MessageBox during paint
            }
        }

        private void Grid_CellPainting_Edit(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvedit") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, EditStart, EditEnd, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(18, 80, 160)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("Edit", font, new SolidBrush(EditText), rect, sf);
                }
            }
            catch
            {
                // Never MessageBox during paint
            }
        }

        private void Grid_CellPainting_Del(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                if (guna2DataGridView1.Columns[e.ColumnIndex].Name != "dgvdel") return;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Rectangle rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 6, e.CellBounds.Width - 12, e.CellBounds.Height - 12);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, DelStart, DelEnd, LinearGradientMode.Vertical))
                using (Pen pen = new Pen(Color.FromArgb(150, 20, 20)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Font font = new Font(e.CellStyle.Font ?? Control.DefaultFont, FontStyle.Bold))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                    e.Graphics.DrawString("Delete", font, new SolidBrush(DelText), rect, sf);
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

        // -------- Helpers --------
        private static string NormalizePhoneToDigits(string input)
        {
            try
            {
                var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
                if (digits.StartsWith("03") && digits.Length == 11)
                    digits = "92" + digits.Substring(1);
                return digits;
            }
            catch
            {
                return "";
            }
        }

        private static void OpenWhatsAppWithPrefill(string digitsE164, string message)
        {
            try
            {
                string encoded = Uri.EscapeDataString(message);
                if (!TryOpenUrl("whatsapp://send?phone=" + digitsE164 + "&text=" + encoded))
                    TryOpenUrl("https://wa.me/" + digitsE164 + "?text=" + encoded);
            }
            catch (Exception)
            {
                // swallowing; outer caller shows error
            }
        }

        private static bool TryOpenUrl(string url)
        {
            try { var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true }; Process.Start(psi); return true; }
            catch { return false; }
        }

        private static decimal ParseMoneySafe(string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s)) return 0m;
                s = s.Replace(",", "").Trim();
                decimal d;
                if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
                    return d;
                if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out d))
                    return d;
                return 0m;
            }
            catch { return 0m; }
        }

        private string BuildFullLedgerWhatsAppMessage()
        {
            try
            {
                var gv = guna2DataGridView1;
                var sb = new StringBuilder();

                sb.AppendLine("Assalam o Alaikum,");
                sb.AppendLine(string.IsNullOrWhiteSpace(_currentAllocationDisp) ? "Receipt Ledger:" : "Receipt Ledger — " + _currentAllocationDisp + ":");
                sb.AppendLine("");

                decimal openingDebit = 0m;
                foreach (DataGridViewRow r in gv.Rows)
                {
                    var rt = Convert.ToString(r.Cells["dgvRowType"].Value);
                    if (string.Equals(rt, "OPENING", StringComparison.OrdinalIgnoreCase))
                    {
                        openingDebit = ParseMoneySafe(SafeCell(r, "dgvDebit"));
                        sb.AppendLine("Opening Debit: " + openingDebit.ToString("0.##"));
                        break;
                    }
                }

                bool anyReceipt = false;
                decimal totalCredit = 0m;
                foreach (DataGridViewRow r in gv.Rows)
                {
                    var rt = Convert.ToString(r.Cells["dgvRowType"].Value);
                    if (!string.Equals(rt, "RECEIPT", StringComparison.OrdinalIgnoreCase)) continue;

                    anyReceipt = true;
                    var date = SafeCell(r, "dgvDate");
                    var credit = SafeCell(r, "dgvCredit");
                    totalCredit += ParseMoneySafe(credit);

                    string refno = guna2DataGridView1.Columns.Contains("dgvRefNo") ? SafeCell(r, "dgvRefNo") : "";
                    if (string.IsNullOrWhiteSpace(refno))
                        refno = SafeCell(r, "dgvDescription");

                    sb.AppendLine("Receipt: " + date + " | " + refno + " | Amount: " + credit);
                }
                if (!anyReceipt) sb.AppendLine("Receipt: —");

                string totalCreditStr = null;
                foreach (DataGridViewRow r in gv.Rows)
                {
                    var rt = Convert.ToString(r.Cells["dgvRowType"].Value);
                    if (string.Equals(rt, "TOTAL", StringComparison.OrdinalIgnoreCase))
                    {
                        totalCreditStr = SafeCell(r, "dgvCredit");
                        var tot = ParseMoneySafe(totalCreditStr);
                        if (tot > 0m) totalCredit = tot;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(totalCreditStr))
                    totalCreditStr = totalCredit.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                sb.AppendLine("Total Credit: " + totalCreditStr);

                foreach (DataGridViewRow r in gv.Rows)
                {
                    var rt = Convert.ToString(r.Cells["dgvRowType"].Value);
                    if (string.Equals(rt, "CLOSING", StringComparison.OrdinalIgnoreCase))
                    {
                        var d = SafeCell(r, "dgvDebit");
                        var c = SafeCell(r, "dgvCredit");
                        sb.AppendLine("Closing (Debit - Credit): " + d + " - " + c);
                        break;
                    }
                }

                decimal balance = openingDebit - totalCredit;
                sb.AppendLine("");
                sb.AppendLine("Summary:");
                sb.AppendLine("Debit(Open): " + openingDebit.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                sb.AppendLine("Credit(Total): " + totalCredit.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("Balance: " + balance.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ShowError("Building WhatsApp message failed.\n\n" + ex.Message);
                return "Receipt Ledger.";
            }
        }

        private static string SafeCell(DataGridViewRow r, string colName)
        {
            if (!r.DataGridView.Columns.Contains(colName)) return "";
            var v = r.Cells[colName].FormattedValue;
            return v == null ? "" : Convert.ToString(v);
        }

        // -------- Dialog helpers (custom forms) --------
        private void ShowError(string message)
        {
            try
            {
                using (var err = new ErrorFormMessage(message, "Error"))
                    err.ShowDialog();
            }
            catch { MessageBox.Show(message, "Error"); }
        }

        private void ShowInfo(string message)
        {
            try
            {
                using (var ok = new CustomeMessage(message, "RealEstate Builders"))
                    ok.ShowDialog();
            }
            catch { MessageBox.Show(message, "Info"); }
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
    }
}
