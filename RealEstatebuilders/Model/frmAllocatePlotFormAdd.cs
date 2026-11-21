// =========================
// RealEstatebuilders.Model.frmAllocatePlotFormAdd (UNTOUCHED; already supports broker on Add)
// =========================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace HubBuilders.Model
{
    public partial class frmAllocatePlotFormAdd : SampleAdd
    {
        // ===== Ctrl+1: Guna-styled search (same colors as ComboBox) | Ctrl+2: back =====
        // No black/white WinForms TextBox — uses Guna2TextBox matching form design.
        private static readonly Color ThemeFill = Color.FromArgb(37, 41, 74);
        private static readonly Color ThemeBorder = Color.FromArgb(112, 51, 255);
        private static readonly Color ThemeText = Color.White;

        private sealed class SearchSlot
        {
            public ComboBox Target;
            public Guna2TextBox Box;
            public ListBox Suggest;
            public string Placeholder;
        }

        private readonly List<SearchSlot> _slots = new List<SearchSlot>();
        private bool _ready;
        private bool _busy;
        private bool _active;

        private Guna2ComboBox cmbDealType;
        private Label lblDealType;
        private Guna2NumericUpDown numMonthlyInstallment;
        private Label lblMonthlyInstallment;

        public int CreatedAllocationId { get; private set; } = 0;

        public frmAllocatePlotFormAdd()
        {
            InitializeComponent();
            this.Load += FrmAllocatePlotFormAdd_Load;
            this.KeyPreview = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == (Keys.Control | Keys.D1))
            {
                EnsureSearchSetup();
                EnterSearchMode();
                return true;
            }
            if (keyData == (Keys.Control | Keys.D2))
            {
                EnsureSearchSetup();
                ExitSearchMode(apply: true);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        private void FrmAllocatePlotFormAdd_Load(object sender, EventArgs e)
        {
            try
            {
            MainClass.EnsureAllocationSchema();

            MainClass.LoadDataIntoComboBox(
                "SELECT id, name FROM customers ORDER BY name;",
                cmbCustomer, "name", "id");
            cmbCustomer.SelectedIndex = -1;

            MainClass.LoadDataIntoComboBox(
                "SELECT id, name FROM societies ORDER BY name;",
                cmbSociety, "name", "id");
            cmbSociety.SelectedIndex = -1;

            cmbBlock.DataSource = null;
            cmbPlotType.DataSource = null;
            cmbPlotNo.DataSource = null;

            cmbSociety.SelectionChangeCommitted += (s, ev) => RefreshBlocksAndTypes();
            cmbSociety.SelectedValueChanged += (s, ev) =>
            {
                RefreshBlocksAndTypes();
                if (_active) SyncDependents();
            };

            cmbBlock.SelectionChangeCommitted += (s, ev) =>
            {
                RefreshAvailablePlots();
                TryApplyBlockMonthlyDefault();
            };
            cmbBlock.SelectedValueChanged += (s, ev) =>
            {
                RefreshAvailablePlots();
                if (_active) SyncSlot(FindSlot(cmbPlotNo));
                TryApplyBlockMonthlyDefault();
            };

            cmbPlotType.SelectionChangeCommitted += (s, ev) => RefreshAvailablePlots();
            cmbPlotType.SelectedValueChanged += (s, ev) =>
            {
                RefreshAvailablePlots();
                if (_active) SyncSlot(FindSlot(cmbPlotNo));
            };

            if (numPrice.Minimum < 0) numPrice.Minimum = 0;
            if (numPrice.Value < 0) numPrice.Value = 0;
            dtpBookingDate.Value = DateTime.Today;

            EnsureDealTypeCombo();
            EnsureMonthlyInstallmentField();

            MakeScrollable(cmbCustomer);
            MakeScrollable(cmbSociety);
            MakeScrollable(cmbBlock);
            MakeScrollable(cmbPlotType);
            MakeScrollable(cmbPlotNo);

            MainClass.LoadBrokerPartnerCombo(cmbBroker);
            MakeScrollable(cmbBroker);

            EnsureSearchSetup();
            ExitSearchMode(apply: false);

            cmbBroker.SelectedValueChanged += (s, ev) =>
            {
                if (!MainClass.TryParseBrokerPartnerKey(cmbBroker.SelectedValue, out string kind, out int bid))
                    return;
                if (kind != "B") return;
                var dtx = MainClass.ExecuteSelectQuery(
                    "SELECT commission_amount_default FROM brokers WHERE id=@id;",
                    new Hashtable { { "@id", bid } });
                if (dtx.Rows.Count == 1)
                {
                    decimal d;
                    if (!decimal.TryParse(Convert.ToString(dtx.Rows[0][0]), out d)) d = 0m;
                    if (d < numBrokerCommission.Minimum) d = numBrokerCommission.Minimum;
                    if (d > numBrokerCommission.Maximum) d = numBrokerCommission.Maximum;
                    numBrokerCommission.Value = d;
                }
            };
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void SyncDependents()
        {
            SyncSlot(FindSlot(cmbBlock));
            SyncSlot(FindSlot(cmbPlotType));
            SyncSlot(FindSlot(cmbPlotNo));
        }
        // -------------------- HELPERS --------------------
        private bool TryGetId(ComboBox cb, out int id)
        {
            id = 0;
            var v = cb?.SelectedValue;
            if (v == null || v == DBNull.Value) return false;
            try { id = Convert.ToInt32(v); return id > 0; }
            catch
            {
                var drv = v as DataRowView;
                if (drv != null && drv.Row.Table.Columns.Contains("id"))
                {
                    id = Convert.ToInt32(drv["id"]);
                    return id > 0;
                }
                return false;
            }
        }

        private void RefreshBlocksAndTypes()
        {
            cmbBlock.DataSource = null;
            cmbPlotType.DataSource = null;
            cmbPlotNo.DataSource = null;

            int sid;
            if (!TryGetId(cmbSociety, out sid)) return;

            // Blocks of selected society
            MainClass.LoadDataIntoComboBox(
                "SELECT id, name FROM blocks WHERE society_id=" + sid + " ORDER BY name;",
                cmbBlock, "name", "id");
            cmbBlock.SelectedIndex = -1;

            // Plot types of selected society
            MainClass.LoadDataIntoComboBox(
                "SELECT id, code FROM plot_types WHERE society_id=" + sid + " ORDER BY code;",
                cmbPlotType, "code", "id");
            cmbPlotType.SelectedIndex = -1;
        }

        private void RefreshAvailablePlots()
        {
            cmbPlotNo.DataSource = null;

            int sid, bid, ptid;
            if (!TryGetId(cmbSociety, out sid)) return;
            if (!TryGetId(cmbBlock, out bid)) return;
            if (!TryGetId(cmbPlotType, out ptid)) return;

            // Sync/desync: Active allocation ho to status Available na dikhe
            try { MainClass.RepairAllocatedPlotStatuses(); } catch { }

            var dt = MainClass.ExecuteSelectQuery(
                @"SELECT id, plot_no
                  FROM plots
                  WHERE society_id=@sid AND block_id=@bid AND plot_type_id=@ptid
                    AND status='Available'
                    AND NOT EXISTS (
                      SELECT 1 FROM customer_plots cp
                      WHERE cp.plot_id = plots.id
                        AND IFNULL(cp.status,'') = 'Active'
                    )
                  ORDER BY plot_no;",
                new Hashtable { { "@sid", sid }, { "@bid", bid }, { "@ptid", ptid } });

            cmbPlotNo.DisplayMember = "plot_no";
            cmbPlotNo.ValueMember = "id";
            cmbPlotNo.DataSource = dt;
            cmbPlotNo.SelectedIndex = -1;
        }

        // -------------------- SAVE = ALLOCATE --------------------
        public override void btnSave_Click(object sender, EventArgs e)
        {
            int custId, sid, bid, ptid, plotId;

            if (!TryGetId(cmbCustomer, out custId))
            { MessageBox.Show("Customer select karein.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbCustomer.DroppedDown = true; return; }

            if (!TryGetId(cmbSociety, out sid))
            { MessageBox.Show("Society select karein.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbSociety.DroppedDown = true; return; }

            if (!TryGetId(cmbBlock, out bid))
            { MessageBox.Show("Block select karein.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbBlock.DroppedDown = true; return; }

            if (!TryGetId(cmbPlotType, out ptid))
            { MessageBox.Show("Plot Type select karein.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbPlotType.DroppedDown = true; return; }

            if (!TryGetId(cmbPlotNo, out plotId))
            { MessageBox.Show("Plot No select karein (sirf Available dikhte hain).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cmbPlotNo.DroppedDown = true; return; }

            // Price / broker / commission optional — Reserve bina paisa / bina broker bhi save
            decimal price = numPrice.Value;
            if (price < 0) price = 0;

            string bpKind = "";
            int bpId = 0;
            MainClass.TryParseBrokerPartnerKey(cmbBroker.SelectedValue, out bpKind, out bpId);

            decimal brokerComm = numBrokerCommission.Value;
            if (brokerComm < 0) brokerComm = 0;

            int brokerIdForDb = (bpKind == "B" && bpId > 0) ? bpId : 0;
            // ==============================================

            string dealType = GetSelectedDealType();
            string plotStatus = dealType == "Sold" ? "Sold" : "Hold";
            decimal monthlyInst = 0;
            try
            {
                if (numMonthlyInstallment != null) monthlyInst = numMonthlyInstallment.Value;
                if (monthlyInst < 0) monthlyInst = 0;
            }
            catch { monthlyInst = 0; }
            if (monthlyInst <= 0)
            {
                try
                {
                    double def = BlockMonthlyInstallmentStore.GetAmountForBlock(bid);
                    if (def > 0) monthlyInst = (decimal)def;
                }
                catch { }
            }

            string bookingDate = dtpBookingDate.Value.ToString("yyyy-MM-dd");
            string notes = (txtAllocNotes.Text ?? "").Trim();

            try
            {
                MainClass.AcquireSharedConnection();
                SQLiteTransaction tx = null;
                try
                {
                    tx = MainClass.con.BeginTransaction();

                    int upd;
                    using (var cmd = new SQLiteCommand(
                        "UPDATE plots SET status=@st WHERE id=@pid AND status='Available';",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@st", plotStatus);
                        cmd.Parameters.AddWithValue("@pid", plotId);
                        upd = cmd.ExecuteNonQuery();
                    }

                    if (upd <= 0)
                    {
                        tx.Rollback();
                        tx = null;
                        MessageBox.Show("Selected plot ab Available nahi raha.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        RefreshAvailablePlots();
                        return;
                    }

                    int ins;
                    using (var cmd = new SQLiteCommand(
                        @"INSERT INTO customer_plots
                    (customer_id, plot_id, price, booking_date, notes, status, broker_id, broker_commission, deal_type, monthly_installment, sync_id)
                  VALUES
                    (@cid, @pid, @price, @bdate, @notes, 'Active', @brokerId, @brokerComm, @dealType, @monthly, lower(hex(randomblob(16))));",
                        MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cid", custId);
                        cmd.Parameters.AddWithValue("@pid", plotId);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@bdate", bookingDate);
                        cmd.Parameters.AddWithValue("@notes", notes);
                        cmd.Parameters.AddWithValue("@brokerId", brokerIdForDb > 0 ? (object)brokerIdForDb : DBNull.Value);
                        cmd.Parameters.AddWithValue("@brokerComm", brokerComm);
                        cmd.Parameters.AddWithValue("@dealType", dealType);
                        cmd.Parameters.AddWithValue("@monthly", monthlyInst);
                        ins = cmd.ExecuteNonQuery();
                    }

                    if (ins <= 0)
                    {
                        tx.Rollback();
                        tx = null;
                        new ErrorFormMessage("Allocation save fail ho gaya.", "Error").ShowDialog();
                        return;
                    }

                    long newCpId = 0;
                    using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", MainClass.con, tx))
                        newCpId = Convert.ToInt64(idCmd.ExecuteScalar());

                    // Partner select → commission Partner Cash Book mein (optional)
                    if (bpKind == "P" && bpId > 0 && newCpId > 0)
                    {
                        MainClass.SyncAllocationPartnerCommission(
                            newCpId, bpId, brokerComm, bookingDate, MainClass.con, tx);
                    }

                    tx.Commit();
                    tx = null;

                    // MainClass.SQL bypass — PushOneNow warna sync_id null / cloud miss
                    try
                    {
                        // Plot pehle cloud — warna allocate upsert/FK fail, mobile miss
                        var syncRows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>();
                        if (plotId > 0)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("plots", plotId));
                        if (newCpId > 0)
                            syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("customer_plots", newCpId));
                        HubSyncService.NotifyLocalWrites(syncRows);
                    }
                    catch { try { HubSyncService.NotifyLocalWrite(); } catch { } }

                    try
                    {
                        MainClass.LogStaffEntry("Insert", "customer_plots",
                            $"plot={plotId}; customer={custId}; price={price}; deal={dealType}; bp={bpKind}:{bpId}; comm={brokerComm}");
                    }
                    catch { }

                    new CustomeMessage(
                        "Plot allocate ho gaya (" + dealType + " — plot: " + plotStatus + ").",
                        "RealEstate Builders").ShowDialog();

                    cmbPlotNo.DataSource = null;
                    RefreshAvailablePlots();
                    numPrice.Value = 0;
                    try { if (numMonthlyInstallment != null) numMonthlyInstallment.Value = 0; } catch { }
                    txtAllocNotes.Clear();
                    dtpBookingDate.Value = DateTime.Today;
                    try { if (cmbDealType != null) cmbDealType.SelectedIndex = 0; } catch { }

                    cmbBroker.SelectedIndex = -1;
                    if (numBrokerCommission.Minimum <= 0 && numBrokerCommission.Maximum >= 0)
                        numBrokerCommission.Value = 0;
                }
                catch (Exception ex)
                {
                    try { tx?.Rollback(); } catch { }
                    HubBuilders.AppSafety.ShowSoft(ex);
                }
                finally
                {
                    MainClass.ReleaseSharedConnection();
                }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void EnsureDealTypeCombo()
        {
            if (cmbDealType != null) return;

            // Notes above Save/Close (panel2 dock bottom) — form taller; notes sit cleanly above buttons
            try
            {
                if (label8 != null) label8.Top = 410;
                if (txtAllocNotes != null)
                {
                    txtAllocNotes.Top = 435;
                    txtAllocNotes.Height = 70;
                }
                // Keep Save/Close panel on bottom and above other controls
                try
                {
                    var p2 = this.Controls["panel2"] as Panel;
                    if (p2 != null)
                    {
                        p2.Dock = DockStyle.Bottom;
                        p2.Height = 71;
                        p2.BringToFront();
                    }
                }
                catch { }
            }
            catch { }

            lblDealType = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(30, 340),
                Text = "Deal Type"
            };

            cmbDealType = new Guna2ComboBox
            {
                Name = "cmbDealType",
                AutoRoundedCorners = true,
                BackColor = Color.Transparent,
                BorderColor = Color.FromArgb(112, 51, 255),
                BorderRadius = 17,
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FillColor = Color.FromArgb(37, 41, 74),
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                ItemHeight = 30,
                Location = new Point(17, 362),
                Size = new Size(245, 36)
            };
            cmbDealType.Items.Add("Installment");
            cmbDealType.Items.Add("Reserve");
            cmbDealType.Items.Add("Sold");
            cmbDealType.SelectedIndex = 0; // default Installment

            this.Controls.Add(lblDealType);
            this.Controls.Add(cmbDealType);
            lblDealType.BringToFront();
            cmbDealType.BringToFront();
            try
            {
                var p2 = this.Controls["panel2"] as Panel;
                if (p2 != null) p2.BringToFront();
            }
            catch { }
            MakeScrollable(cmbDealType);
        }

        private void EnsureMonthlyInstallmentField()
        {
            if (numMonthlyInstallment != null) return;
            lblMonthlyInstallment = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(294, 340),
                Text = "Monthly Installment"
            };
            numMonthlyInstallment = new Guna2NumericUpDown
            {
                Name = "numMonthlyInstallment",
                AutoRoundedCorners = true,
                BackColor = Color.FromArgb(32, 36, 61),
                BorderColor = Color.FromArgb(112, 51, 255),
                BorderRadius = 17,
                FillColor = Color.FromArgb(37, 41, 74),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                Location = new Point(294, 362),
                Size = new Size(245, 36),
                Minimum = 0,
                Maximum = 1000000000,
                DecimalPlaces = 0,
                Value = 0
            };
            this.Controls.Add(lblMonthlyInstallment);
            this.Controls.Add(numMonthlyInstallment);
            lblMonthlyInstallment.BringToFront();
            numMonthlyInstallment.BringToFront();
            try
            {
                var p2 = this.Controls["panel2"] as Panel;
                if (p2 != null) p2.BringToFront();
            }
            catch { }
        }

        private void TryApplyBlockMonthlyDefault()
        {
            try
            {
                if (numMonthlyInstallment == null) return;
                if (numMonthlyInstallment.Value > 0) return;
                int bid;
                if (!TryGetId(cmbBlock, out bid) || bid <= 0) return;
                double def = BlockMonthlyInstallmentStore.GetAmountForBlock(bid);
                if (def <= 0) return;
                decimal d = (decimal)def;
                if (d < numMonthlyInstallment.Minimum) d = numMonthlyInstallment.Minimum;
                if (d > numMonthlyInstallment.Maximum) d = numMonthlyInstallment.Maximum;
                numMonthlyInstallment.Value = d;
            }
            catch { }
        }

        private string GetSelectedDealType()
        {
            try
            {
                if (cmbDealType == null || cmbDealType.SelectedItem == null)
                    return "Installment";
                string s = Convert.ToString(cmbDealType.SelectedItem).Trim();
                if (string.Equals(s, "Reserve", StringComparison.OrdinalIgnoreCase)) return "Reserve";
                if (string.Equals(s, "Sold", StringComparison.OrdinalIgnoreCase)) return "Sold";
                return "Installment";
            }
            catch { return "Installment"; }
        }

        // ================== Search mode: Guna2TextBox (theme fill — NOT black) ==================

        private void MakeScrollable(ComboBox cb)
        {
            if (cb == null) return;
            cb.IntegralHeight = false;
            cb.MaxDropDownItems = 20;
            cb.DropDownHeight = 320;
            cb.DropDownStyle = ComboBoxStyle.DropDownList; // keep original look
        }

        private void EnsureSearchSetup()
        {
            if (_ready) return;

            AddSlot(cmbCustomer, "Type customer name / mobile…");
            AddSlot(cmbSociety, "Type society…");
            AddSlot(cmbBlock, "Type block…");
            AddSlot(cmbPlotType, "Type plot type…");
            AddSlot(cmbPlotNo, "Type plot no…");
            AddSlot(cmbBroker, "Type broker / partner…");

            this.Resize += (s, e) => { if (_active) PositionAll(); };
            _ready = true;
        }

        private void AddSlot(ComboBox target, string placeholder)
        {
            if (target == null) return;

            // Same look as Guna ComboBox on this form
            var box = new Guna2TextBox
            {
                FillColor = ThemeFill,
                ForeColor = ThemeText,
                BorderColor = ThemeBorder,
                BorderRadius = 17,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                PlaceholderForeColor = Color.Silver,
                PlaceholderText = placeholder,
                Cursor = Cursors.IBeam,
                Visible = false,
                TabStop = true
            };
            try
            {
                box.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
                box.HoverState.BorderColor = ThemeBorder;
                box.ShadowDecoration.Parent = box;
            }
            catch { }

            var suggest = new ListBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = ThemeFill,          // same as combo — not pure black
                ForeColor = ThemeText,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                IntegralHeight = false,
                Height = ReferenceEquals(target, cmbCustomer) ? 200 : 140
            };

            var slot = new SearchSlot
            {
                Target = target,
                Box = box,
                Suggest = suggest,
                Placeholder = placeholder
            };

            box.Enter += (s, e) =>
            {
                HideOtherSuggests(slot);
                RefreshSuggest(slot);
            };

            box.TextChanged += (s, e) =>
            {
                if (_busy || !_active) return;
                RefreshSuggest(slot);
                TryExact(slot);
            };

            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && suggest.Visible && suggest.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    suggest.Focus();
                    if (suggest.SelectedIndex < 0) suggest.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplySlot(slot);
                    int ix = _slots.IndexOf(slot);
                    if (ix >= 0 && ix + 1 < _slots.Count)
                        _slots[ix + 1].Box.Focus();
                    else
                        suggest.Visible = false;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    ExitSearchMode(apply: true);
                }
            };

            box.Leave += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    if (!_active || _busy) return;
                    if (suggest.Focused) return;
                    suggest.Visible = false;
                    ApplySlot(slot);
                }));
            };

            suggest.Click += (s, e) =>
            {
                if (suggest.SelectedItem == null) return;
                _busy = true;
                try { box.Text = Convert.ToString(suggest.SelectedItem); }
                finally { _busy = false; }
                ApplySlot(slot);
                suggest.Visible = false;
                box.Focus();
            };

            suggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (suggest.SelectedItem != null)
                    {
                        _busy = true;
                        try { box.Text = Convert.ToString(suggest.SelectedItem); }
                        finally { _busy = false; }
                        ApplySlot(slot);
                        suggest.Visible = false;
                        box.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    box.Focus();
                }
            };

            this.Controls.Add(box);
            this.Controls.Add(suggest);
            _slots.Add(slot);
        }

        private SearchSlot FindSlot(ComboBox cb)
        {
            foreach (var s in _slots)
                if (ReferenceEquals(s.Target, cb)) return s;
            return null;
        }

        private void HideOtherSuggests(SearchSlot keep)
        {
            foreach (var s in _slots)
                if (s.Suggest != null && !ReferenceEquals(s, keep))
                    s.Suggest.Visible = false;
        }

        private void EnterSearchMode()
        {
            if (_slots.Count == 0) return;
            _active = true;
            _busy = true;
            try
            {
                foreach (var slot in _slots)
                {
                    SyncSlot(slot);
                    slot.Box.Visible = true;
                    slot.Box.BringToFront();
                    slot.Suggest.Visible = false;
                }
                PositionAll();
            }
            finally { _busy = false; }

            try
            {
                _slots[0].Box.Focus();
                _slots[0].Box.SelectAll();
            }
            catch { }
        }

        private void ExitSearchMode(bool apply)
        {
            if (apply)
            {
                foreach (var slot in _slots)
                    ApplySlot(slot);
            }

            _active = false;
            foreach (var slot in _slots)
            {
                slot.Box.Visible = false;
                slot.Suggest.Visible = false;
            }
            try { cmbCustomer?.Focus(); } catch { }
        }

        private void PositionAll()
        {
            foreach (var slot in _slots)
            {
                var t = slot.Target;
                if (t == null) continue;
                slot.Box.SetBounds(t.Left, t.Top, t.Width, Math.Max(36, t.Height));
                int h = ReferenceEquals(t, cmbCustomer) ? 200 : 140;
                slot.Suggest.SetBounds(slot.Box.Left, slot.Box.Bottom + 2, Math.Max(slot.Box.Width, 240), h);
                slot.Box.BringToFront();
                if (slot.Suggest.Visible) slot.Suggest.BringToFront();
            }
        }

        private void SyncSlot(SearchSlot slot)
        {
            if (slot?.Box == null || slot.Target == null) return;
            _busy = true;
            try
            {
                string cur = "";
                if (slot.Target.SelectedIndex >= 0)
                    cur = Convert.ToString(slot.Target.Text ?? "").Trim();
                slot.Box.Text = cur;
            }
            finally { _busy = false; }
        }

        private void RefreshSuggest(SearchSlot slot)
        {
            if (slot?.Suggest == null || slot.Target == null) return;
            string q = (slot.Box.Text ?? "").Trim();

            List<string> items;
            if (ReferenceEquals(slot.Target, cmbCustomer))
                items = FilterCustomer(q);
            else
                items = FilterCombo(slot.Target, q);

            slot.Suggest.BeginUpdate();
            try
            {
                slot.Suggest.Items.Clear();
                foreach (var s in items) slot.Suggest.Items.Add(s);
            }
            finally { slot.Suggest.EndUpdate(); }

            PositionAll();
            if (slot.Suggest.Items.Count > 0 && slot.Box.Focused)
            {
                if (slot.Suggest.SelectedIndex < 0) slot.Suggest.SelectedIndex = 0;
                slot.Suggest.Visible = true;
                slot.Suggest.BringToFront();
                slot.Box.BringToFront();
            }
            else
            {
                slot.Suggest.Visible = false;
            }
        }

        private List<string> FilterCombo(ComboBox cb, string q)
        {
            var list = new List<string>();
            foreach (var s in GetComboDisplayList(cb))
            {
                if (q.Length == 0 || s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(s);
                    if (list.Count >= 80) break;
                }
            }
            return list;
        }

        private List<string> FilterCustomer(string q)
        {
            var list = new List<string>();
            try
            {
                var dt = MainClass.ExecuteSelectQuery(
                    "SELECT name, COALESCE(mobile,'') AS mobile FROM customers ORDER BY name;", null);
                if (dt == null) return list;
                q = (q ?? "").Trim();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string name = Convert.ToString(dt.Rows[i]["name"] ?? "").Trim();
                    string mobile = Convert.ToString(dt.Rows[i]["mobile"] ?? "").Trim();
                    if (name.Length == 0) continue;
                    string disp = string.IsNullOrEmpty(mobile) ? name : (name + " | " + mobile);
                    if (q.Length == 0
                        || name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || mobile.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        list.Add(disp);
                        if (list.Count >= 100) break;
                    }
                }
            }
            catch { }
            return list;
        }

        private void TryExact(SearchSlot slot)
        {
            string t = (slot.Box.Text ?? "").Trim();
            if (t.Length == 0) return;
            if (ReferenceEquals(slot.Target, cmbCustomer))
            {
                if (TrySelectCustomerFromText(t)) { /* keep typing ok */ }
                return;
            }
            int row = FindComboRow(slot.Target, StripPipe(t));
            if (row < 0) return;
            var dt = GetComboTable(slot.Target);
            string col = GetDisplayColumn(slot.Target);
            if (dt == null || !dt.Columns.Contains(col)) return;
            string cand = Convert.ToString(dt.Rows[row][col] ?? "").Trim();
            if (string.Equals(cand, StripPipe(t), StringComparison.OrdinalIgnoreCase))
                SelectComboRow(slot.Target, row);
        }

        private void ApplySlot(SearchSlot slot)
        {
            if (slot?.Box == null || slot.Target == null) return;
            string txt = (slot.Box.Text ?? "").Trim();
            if (txt.Length == 0) return;

            if (slot.Suggest.Visible && slot.Suggest.SelectedItem != null)
            {
                string sel = Convert.ToString(slot.Suggest.SelectedItem).Trim();
                if (sel.Length > 0) txt = sel;
            }

            if (ReferenceEquals(slot.Target, cmbCustomer))
            {
                TrySelectCustomerFromText(txt);
                return;
            }

            int row = FindComboRow(slot.Target, StripPipe(txt));
            if (row >= 0) SelectComboRow(slot.Target, row);
        }

        private static string StripPipe(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return "";
            int p = txt.IndexOf('|');
            if (p >= 0) txt = txt.Substring(0, p);
            return txt.Trim();
        }

        private List<string> GetComboDisplayList(ComboBox cb)
        {
            var list = new List<string>();
            var dt = GetComboTable(cb);
            string col = GetDisplayColumn(cb);
            if (dt == null || !dt.Columns.Contains(col)) return list;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var s = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                if (s.Length > 0) list.Add(s);
            }
            return list;
        }

        private DataTable GetComboTable(ComboBox cb)
        {
            if (cb?.DataSource == null) return null;
            if (cb.DataSource is DataTable dt) return dt;
            if (cb.DataSource is DataView dv) return dv.Table;
            if (cb.DataSource is BindingSource bs)
            {
                if (bs.DataSource is DataTable t) return t;
                if (bs.DataSource is DataView v) return v.Table;
            }
            return null;
        }

        private string GetDisplayColumn(ComboBox cb)
        {
            string dm = (cb?.DisplayMember ?? "").Trim();
            return dm.Length > 0 ? dm : "name";
        }

        private string GetValueColumn(ComboBox cb)
        {
            string vm = (cb?.ValueMember ?? "").Trim();
            return vm.Length > 0 ? vm : "id";
        }

        private bool TrySelectCustomerFromText(string txt)
        {
            txt = (txt ?? "").Trim();
            if (txt.Length == 0) return false;
            string namePart = StripPipe(txt);
            string mobilePart = "";
            int pipe = txt.IndexOf('|');
            if (pipe >= 0) mobilePart = txt.Substring(pipe + 1).Trim();

            int row = FindComboRow(cmbCustomer, namePart);
            if (row >= 0) { SelectComboRow(cmbCustomer, row); return true; }

            try
            {
                var dt = MainClass.ExecuteSelectQuery(
                    "SELECT id, name, COALESCE(mobile,'') AS mobile FROM customers ORDER BY name;", null);
                if (dt == null) return false;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dt.Rows[i]["id"]);
                    string name = Convert.ToString(dt.Rows[i]["name"] ?? "").Trim();
                    string mobile = Convert.ToString(dt.Rows[i]["mobile"] ?? "").Trim();
                    if (string.Equals(name, namePart, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrEmpty(mobilePart) && string.Equals(mobile, mobilePart, StringComparison.OrdinalIgnoreCase))
                        || string.Equals(mobile, txt, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbCustomer.SelectedValue = id;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private int FindComboRow(ComboBox cb, string input)
        {
            var dt = GetComboTable(cb);
            string col = GetDisplayColumn(cb);
            input = (input ?? "").Trim();
            if (dt == null || input.Length == 0 || !dt.Columns.Contains(col)) return -1;
            int exact = -1, starts = -1, contains = -1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var name = Convert.ToString(dt.Rows[i][col] ?? "").Trim();
                if (name.Length == 0) continue;
                if (exact < 0 && string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) exact = i;
                if (starts < 0 && name.StartsWith(input, StringComparison.OrdinalIgnoreCase)) starts = i;
                if (contains < 0 && name.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0) contains = i;
                if (exact >= 0) break;
            }
            if (exact >= 0) return exact;
            if (starts >= 0) return starts;
            return contains;
        }

        private void SelectComboRow(ComboBox cb, int rowIndex)
        {
            var dt = GetComboTable(cb);
            if (dt != null && rowIndex >= 0 && rowIndex < dt.Rows.Count)
            {
                string valueCol = GetValueColumn(cb);
                if (dt.Columns.Contains(valueCol))
                {
                    try
                    {
                        cb.SelectedValue = dt.Rows[rowIndex][valueCol];
                        if (cb.SelectedValue == null || cb.SelectedValue is DBNull)
                            cb.SelectedIndex = rowIndex;
                        return;
                    }
                    catch { }
                }
            }
            try { cb.SelectedIndex = rowIndex; } catch { }
        }
    }
}
