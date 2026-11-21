// frmReceiptFormAdd.cs  (SQLite: customers, customer_plots, receipts, ledger)
// ReceiptForm = CREDIT ONLY. Grid shows Opening(Debit), Receipts(Credit), Total, Closing with running Balance.
// DataGridView has last two columns: dgvEdit (Edit) and dgvDel (Delete). Edit/Delete handled in CellContentClick.

using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmReceiptFormAdd : Sample
    {
        private bool _binding = false;
        private CheckBox _chkCoverInstallments;

        // Edit state
        private long? _editingReceiptId = null;
        private long? _editingLedgerId = null;

        public frmReceiptFormAdd()
        {
            InitializeComponent();
            WireEvents();
            InitUi();
        }

        private void WireEvents()
        {
            this.Load += FrmReceiptFormAdd_Load;
            btnSaveReceipt.Click += BtnSaveReceipt_Click;
            if (btnAllocateManual != null)
                btnAllocateManual.Click += (s, e) => MainClass.CloseDialog(this);
            cmbCustomerPlot.SelectedIndexChanged += (s, e) => { if (!_binding) RefreshGrid(); };

            // IMPORTANT: handle edit/delete via grid click
            grdReceiptView.CellContentClick += grdReceiptView_CellContentClick;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                if (btnSaveReceipt != null && btnSaveReceipt.Enabled && btnSaveReceipt.Visible)
                {
                    BtnSaveReceipt_Click(btnSaveReceipt, EventArgs.Empty);
                    return true;
                }
            }

            if (keyData == Keys.Escape)
            {
                if (ActiveControl is ComboBox cb && cb.DroppedDown)
                    return false;
                MainClass.CloseDialog(this);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitUi()
        {
            if (cmbMode.Items.Count == 0) cmbMode.Items.AddRange(new[] { "Cash", "Bank" });
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            if (cmbMode.SelectedIndex < 0 && cmbMode.Items.Count > 0) cmbMode.SelectedIndex = 0;

            dtpReceiptDate.Format = DateTimePickerFormat.Short;

            EnsureCoverInstallmentsOption();
            ConfigureGrid();
        }

        /// <summary>Additive checkbox only — existing layout/controls unchanged.</summary>
        private void EnsureCoverInstallmentsOption()
        {
            if (_chkCoverInstallments != null && !_chkCoverInstallments.IsDisposed) return;
            _chkCoverInstallments = new CheckBox
            {
                Name = "chkCoverInstallments",
                AutoSize = true,
                Text = "Cover previous/current installments only — no upcoming (WhatsApp hide)",
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.Transparent,
                Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(280, 167),
                TabStop = true
            };
            this.Controls.Add(_chkCoverInstallments);
            _chkCoverInstallments.BringToFront();
        }

        private void ConfigureGrid()
        {
            if (grdReceiptView.Columns.Count > 0) return;

            
            // ---------- columns ----------
            // hidden technicals (keep for logic)
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowType", DataPropertyName = "RowType", Visible = false });
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReceiptId", DataPropertyName = "ReceiptId", Visible = false });
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", DataPropertyName = "Date", Visible = false });

            // visible
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "SrNo", HeaderText = "Sr#", Width = 60, ReadOnly = true });
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", DataPropertyName = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            var colDebit = new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "Debit", DataPropertyName = "Debit", Width = 110 };
            var colCredit = new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "Credit", DataPropertyName = "Credit", Width = 110 };
            var colBal = new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", DataPropertyName = "Balance", Width = 120 };

            colDebit.DefaultCellStyle.Format = "N2";
            colCredit.DefaultCellStyle.Format = "N2";
            colBal.DefaultCellStyle.Format = "N2";
            colDebit.DefaultCellStyle.Alignment =
            colCredit.DefaultCellStyle.Alignment =
            colBal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            grdReceiptView.Columns.Add(colDebit);
            grdReceiptView.Columns.Add(colCredit);
            grdReceiptView.Columns.Add(colBal);

            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mode", HeaderText = "Mode", DataPropertyName = "Mode", Width = 90 });
            grdReceiptView.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefNo", HeaderText = "RefNo", DataPropertyName = "RefNo", Width = 120 });

            // tumhare dgvEdit/dgvDel already added hain → unko last me rakho (designer/code tumhare pass)
            // events
            grdReceiptView.DataBindingComplete += Grd_DataBindingComplete;
            grdReceiptView.CellFormatting += Grd_CellFormatting;
        }
        private void Grd_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Sr# numbering + blanking Mode/RefNo for summary rows
            for (int i = 0; i < grdReceiptView.Rows.Count; i++)
            {
                var r = grdReceiptView.Rows[i];
                r.Cells["SrNo"].Value = (i + 1).ToString();

                var rt = r.Cells["RowType"]?.Value?.ToString();
                bool isSummary = rt == "OPENING" || rt == "TOTAL" || rt == "CLOSING";

                if (isSummary)
                {
                    r.Cells["Mode"].Value = "";
                    r.Cells["RefNo"].Value = "";
                }

                // disable your edit/del buttons visually on summary rows (if they exist)
                if (grdReceiptView.Columns.Contains("dgvEdit"))
                    r.Cells["dgvEdit"].ReadOnly = isSummary;
                if (grdReceiptView.Columns.Contains("dgvDel"))
                    r.Cells["dgvDel"].ReadOnly = isSummary;
            }
        }

        private void Grd_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // pretty styles: summaries bold/gray, negatives red
            var row = grdReceiptView.Rows[e.RowIndex];
            var rt = row.Cells["RowType"]?.Value?.ToString();

            if (rt == "OPENING" || rt == "TOTAL" || rt == "CLOSING")
            {
                row.DefaultCellStyle.Font = new System.Drawing.Font(grdReceiptView.Font, System.Drawing.FontStyle.Bold);
                row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(36, 42, 58);
                row.DefaultCellStyle.ForeColor = System.Drawing.Color.Gainsboro;

                // keep numbers right aligned (already set on columns)
            }

            // negative balance → red
            if (grdReceiptView.Columns[e.ColumnIndex].Name == "Balance" && e.Value != null && e.Value != DBNull.Value)
            {
                double val;
                if (double.TryParse(e.Value.ToString(), out val) && val < 0)
                {
                    row.Cells["Balance"].Style.ForeColor = System.Drawing.Color.IndianRed;
                }
                else
                {
                    row.Cells["Balance"].Style.ForeColor = System.Drawing.Color.WhiteSmoke;
                }
            }

            // compact Description for receipts: if it is numeric/ref-like, keep as-is; else trim spaces
            if (grdReceiptView.Columns[e.ColumnIndex].Name == "Description" && e.Value != null)
            {
                e.Value = e.Value.ToString().Trim();
                e.FormattingApplied = true;
            }
        }

        private void FrmReceiptFormAdd_Load(object sender, EventArgs e)
        {
            LoadCustomerPlots();
            RefreshGrid();
            if (!_editingReceiptId.HasValue)
                SuggestNextRefNo();
        }

        /// <summary>
        /// Auto Ref No: 001, 002… fills gaps after delete. Does not change existing rows.
        /// Non-numeric refs (client custom) are ignored for sequencing.
        /// </summary>
        private void SuggestNextRefNo()
        {
            try
            {
                txtRefNo.Text = ComputeNextRefNo();
            }
            catch
            {
                try { txtRefNo.Text = "001"; } catch { }
            }
        }

        private static string ComputeNextRefNo()
        {
            var used = new HashSet<int>();
            DataTable dt = HubBuilders.MainClass.ExecuteSelectQuery1(
                "SELECT ref_no FROM receipts WHERE ref_no IS NOT NULL AND TRIM(ref_no) <> ''",
                null);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    int? n = ParseNumericRef(Convert.ToString(row["ref_no"]));
                    if (n.HasValue && n.Value > 0) used.Add(n.Value);
                }
            }
            int next = 1;
            while (used.Contains(next)) next++;
            return next.ToString("D3");
        }

        private static int? ParseNumericRef(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string t = raw.Trim();
            foreach (char c in t)
                if (!char.IsDigit(c)) return null;
            int v;
            if (!int.TryParse(t, out v)) return null;
            return v;
        }

        // =============== Utilities ===============
        private object Scalar(string sql, Dictionary<string, object> p = null)
            => HubBuilders.MainClass.ExecuteScalar(sql, p);

        private DataTable SelectQ(string sql, Dictionary<string, object> p = null)
            => HubBuilders.MainClass.ExecuteSelectQuery1(sql, p);

        private int? CurrentCustomerPlotId
        {
            get
            {
                var v = cmbCustomerPlot?.SelectedValue;
                if (v == null || v == DBNull.Value) return null;
                try { return Convert.ToInt32(v); } catch { return null; }
            }
        }

        // =============== Load combo: Active customer_plots ===============
        private void LoadCustomerPlots()
        {
            const string q = @"
SELECT 
  cp.id AS id,
  c.name || ' | Plot ' || p.plot_no AS disp
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p     ON p.id = cp.plot_id
WHERE cp.status = 'Active'
ORDER BY c.name, p.plot_no;";

            _binding = true;
            HubBuilders.MainClass.LoadDataIntoComboBox(q, cmbCustomerPlot, "disp", "id");
            var dt = cmbCustomerPlot.DataSource as DataTable;
            bool has = dt != null && dt.Rows.Count > 0;
            if (has) cmbCustomerPlot.SelectedIndex = 0;
            btnSaveReceipt.Enabled = has;
            if (!has) MessageBox.Show("Active customer_plots nahi mile. Pehle allocation/booking create karo.");
            _binding = false;
        }

        // =============== Grid: Opening + Receipts + Totals/Closing ===============
        private void RefreshGrid()
        {
            try
            {
                var cpId = CurrentCustomerPlotId;
                if (cpId == null) { grdReceiptView.DataSource = null; this.Text = "Receipt"; return; }
                grdReceiptView.DataSource = BuildLedgerViewWithSummaries(cpId.Value); // tumhari existing method
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }


        private DataTable BuildLedgerViewWithSummaries(int customerPlotId)
        {
            var t = new DataTable();
            t.Columns.Add("RowType", typeof(string));     // "OPENING", "RECEIPT", "TOTAL", "CLOSING"
            t.Columns.Add("ReceiptId", typeof(long));
            t.Columns.Add("Date", typeof(string));
            t.Columns.Add("Description", typeof(string));
            t.Columns.Add("Debit", typeof(double));
            t.Columns.Add("Credit", typeof(double));
            t.Columns.Add("Balance", typeof(double));
            t.Columns.Add("Mode", typeof(string));
            t.Columns.Add("RefNo", typeof(string));

            // Opening (Effective Debit)
            const string qPrice = @"SELECT COALESCE(price,0) AS price FROM customer_plots WHERE id=@cpid;";
            var dtPrice = SelectQ(qPrice, new Dictionary<string, object> { { "@cpid", customerPlotId } });
            double plotPrice = (dtPrice != null && dtPrice.Rows.Count > 0) ? SafeDouble(dtPrice.Rows[0]["price"]) : 0d;

            const string qLedgerSum = @"
SELECT COALESCE(SUM(debit),0) AS tdebit,
       COALESCE(SUM(credit),0) AS tcredit
FROM ledger
WHERE customer_plot_id=@cpid;";
            var dtLed = SelectQ(qLedgerSum, new Dictionary<string, object> { { "@cpid", customerPlotId } });
            double tDebit = 0d, tCredit = 0d;
            if (dtLed != null && dtLed.Rows.Count > 0)
            {
                tDebit = SafeDouble(dtLed.Rows[0]["tdebit"]);
                tCredit = SafeDouble(dtLed.Rows[0]["tcredit"]);
            }

            double opening = tDebit > 0 ? tDebit : plotPrice;
            double running = opening;

            var rOpen = t.NewRow();
            rOpen["RowType"] = "OPENING";
            rOpen["ReceiptId"] = DBNull.Value;
            rOpen["Date"] = ""; // show empty (or booking date if you store)
            rOpen["Description"] = "Opening (Debit)";
            rOpen["Debit"] = opening;
            rOpen["Credit"] = 0d;
            rOpen["Balance"] = running;
            rOpen["Mode"] = "";
            rOpen["RefNo"] = "";
            t.Rows.Add(rOpen);

            // Receipts in chronological order, compute running balance
            const string qRcptAsc = @"
SELECT id, receipt_date, amount, mode, COALESCE(ref_no,'') AS ref_no, COALESCE(notes,'') AS notes
FROM receipts
WHERE customer_plot_id=@cpid
ORDER BY date(receipt_date) ASC, id ASC;";
            var rcpts = SelectQ(qRcptAsc, new Dictionary<string, object> { { "@cpid", customerPlotId } });

            double totalCredit = 0d;

            foreach (DataRow rr in rcpts.Rows)
            {
                double amt = SafeDouble(rr["amount"]);
                totalCredit += amt;
                running -= amt;

                var rw = t.NewRow();
                rw["RowType"] = "RECEIPT";
                rw["ReceiptId"] = Convert.ToInt64(rr["id"]);
                rw["Date"] = Convert.ToDateTime(rr["receipt_date"]).ToString("yyyy-MM-dd");
                rw["Description"] = string.IsNullOrWhiteSpace(rr["notes"]?.ToString()) ? "Receipt" : rr["notes"].ToString();
                rw["Debit"] = 0d;
                rw["Credit"] = amt;
                rw["Balance"] = running;
                rw["Mode"] = rr["mode"].ToString();
                rw["RefNo"] = rr["ref_no"].ToString();
                t.Rows.Add(rw);
            }

            // Totals row
            var rTot = t.NewRow();
            rTot["RowType"] = "TOTAL";
            rTot["ReceiptId"] = DBNull.Value;
            rTot["Date"] = "";
            rTot["Description"] = "Total Credit";
            rTot["Debit"] = 0d;
            rTot["Credit"] = totalCredit;
            rTot["Balance"] = DBNull.Value;
            rTot["Mode"] = "";
            rTot["RefNo"] = "";
            t.Rows.Add(rTot);

            // Closing balance
            var rClose = t.NewRow();
            rClose["RowType"] = "CLOSING";
            rClose["ReceiptId"] = DBNull.Value;
            rClose["Date"] = "";
            rClose["Description"] = "Closing Balance (Debit - Credit)";
            rClose["Debit"] = opening;
            rClose["Credit"] = totalCredit;
            rClose["Balance"] = opening - totalCredit;
            rClose["Mode"] = "";
            rClose["RefNo"] = "";
            t.Rows.Add(rClose);

            this.Text = $"Receipt | Opening: {opening:N2}  Credit: {totalCredit:N2}  Closing: {(opening - totalCredit):N2}";
            return t;
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v; return double.TryParse(o.ToString(), out v) ? v : 0d;
        }

        // =============== Grid: Edit/Delete via CellContentClick ===============
        private void grdReceiptView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = grdReceiptView.Columns[e.ColumnIndex].Name;
            if (colName != "dgvEdit" && colName != "dgvDel") return;

            var row = grdReceiptView.Rows[e.RowIndex];
            var rowType = row.Cells["RowType"]?.Value?.ToString();
            if (rowType != "RECEIPT") return; // only receipts are editable/deletable

            var idObj = row.Cells["ReceiptId"]?.Value;
            if (idObj == null || idObj == DBNull.Value) { MessageBox.Show("Invalid receipt row."); return; }
            long receiptId = Convert.ToInt64(idObj);

            if (colName == "dgvEdit")
            {
                BeginEditFromGrid(receiptId);
            }
            else if (colName == "dgvDel")
            {
                DeleteReceiptFromGrid(receiptId);
            }
        }

        private void BeginEditFromGrid(long receiptId)
        {
            const string q = @"
SELECT r.id, r.customer_plot_id, r.receipt_date, r.amount, r.mode, r.ref_no, r.notes, r.ledger_id
FROM receipts r
WHERE r.id=@id;";
            var dt = SelectQ(q, new Dictionary<string, object> { { "@id", receiptId } });
            if (dt.Rows.Count == 0) { MessageBox.Show("Receipt not found."); return; }
            var r = dt.Rows[0];

            _editingReceiptId = receiptId;
            _editingLedgerId = r["ledger_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["ledger_id"]);

            // ensure same CP selected
            int cpId = 0;
            try { cpId = Convert.ToInt32(r["customer_plot_id"]); } catch { }
            if (cpId > 0 && CurrentCustomerPlotId != cpId)
            {
                _binding = true;
                try { cmbCustomerPlot.SelectedValue = cpId; } catch { }
                _binding = false;
            }

            try
            {
                if (r["receipt_date"] != DBNull.Value
                    && DateTime.TryParse(Convert.ToString(r["receipt_date"]), out DateTime rd))
                    dtpReceiptDate.Value = rd;
            }
            catch { }
            try
            {
                decimal amt = Convert.ToDecimal(SafeDouble(r["amount"]));
                if (amt < numAmount.Minimum) amt = numAmount.Minimum;
                if (amt > numAmount.Maximum) amt = numAmount.Maximum;
                numAmount.Value = amt;
            }
            catch { }
            cmbMode.SelectedItem = r["mode"]?.ToString() == "Bank" ? "Bank" : "Cash";
            txtRefNo.Text = r["ref_no"] == DBNull.Value ? "" : r["ref_no"].ToString();
            string rawNotes = r["notes"] == DBNull.Value ? "" : r["notes"].ToString();
            EnsureCoverInstallmentsOption();
            if (_chkCoverInstallments != null)
                _chkCoverInstallments.Checked = HubBuilders.InstallmentCoverHelper.HasToken(rawNotes);
            txtReceiptNotes.Text = HubBuilders.InstallmentCoverHelper.StripToken(rawNotes);

            btnSaveReceipt.Text = "Update";
            this.Text = $"Receipt | Editing #{receiptId}";
        }

        private void DeleteReceiptFromGrid(long receiptId)
        {
            if (MessageBox.Show("Delete this receipt?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            SQLiteTransaction tx = null;
            try
            {
                HubBuilders.MainClass.AcquireSharedConnection();

                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", HubBuilders.MainClass.con))
                    pragma.ExecuteNonQuery();

                // fetch ledger_id first
                long? ledgerId = null;
                using (var cmd = new SQLiteCommand("SELECT ledger_id FROM receipts WHERE id=@id;", HubBuilders.MainClass.con))
                {
                    cmd.Parameters.AddWithValue("@id", receiptId);
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value) ledgerId = Convert.ToInt64(obj);
                }

                var cascade = HubSyncService.CollectReceiptCascade(receiptId, ledgerId);

                tx = HubBuilders.MainClass.con.BeginTransaction();
                // pending_delete + local deletes EK transaction; SoftMark HTTP bahar
                HubSyncService.QueuePendingDeletesForCascade(HubBuilders.MainClass.con, tx, cascade);

                HubSyncService.DeletePartnerMoneyWhere(
                    HubBuilders.MainClass.con, tx,
                    "receipt_id=@rid",
                    cmd => cmd.Parameters.AddWithValue("@rid", receiptId),
                    softMarkCloud: false);

                // delete receipt
                using (var cmd = new SQLiteCommand("DELETE FROM receipts WHERE id=@id;", HubBuilders.MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@id", receiptId);
                    cmd.ExecuteNonQuery();
                }

                // delete linked ledger (if any)
                if (ledgerId.HasValue)
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM ledger WHERE id=@lid;", HubBuilders.MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@lid", ledgerId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                tx = null;
                try { HubSyncService.SoftMarkQueuedCascade(cascade); } catch { }
                try { HubSyncService.NotifyLocalWrite(); } catch { }
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                MessageBox.Show("Delete failed: " + ex.Message);
                return;
            }
            finally
            {
                HubBuilders.MainClass.ReleaseSharedConnection();
            }

            // reset if we were editing this one; else refresh next Ref if adding new
            if (_editingReceiptId == receiptId)
            {
                _editingReceiptId = null;
                _editingLedgerId = null;
                btnSaveReceipt.Text = "Save";
                SuggestNextRefNo();
                txtReceiptNotes.Clear();
                numAmount.Value = 0;
            }
            else if (!_editingReceiptId.HasValue)
            {
                SuggestNextRefNo();
            }

            RefreshGrid(); // recompute opening/closing
        }

        private string ApplyInstallmentCoverIfChecked(int customerPlotId, double amount, string notes)
        {
            try
            {
                EnsureCoverInstallmentsOption();
                if (_chkCoverInstallments == null || !_chkCoverInstallments.Checked)
                    return HubBuilders.InstallmentCoverHelper.StripToken(notes);

                DateTime booking = DateTime.Today;
                double monthly = 0;
                var dt = SelectQ(@"
SELECT cp.booking_date,
       COALESCE(cp.monthly_installment,0) AS monthly_installment,
       COALESCE(bmi.monthly_amount,0) AS block_monthly
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE cp.id=@id;", new Dictionary<string, object> { { "@id", customerPlotId } });
                if (dt != null && dt.Rows.Count > 0)
                {
                    DateTime parsed;
                    if (DateTime.TryParse(Convert.ToString(dt.Rows[0]["booking_date"]), out parsed))
                        booking = parsed.Date;
                    monthly = SafeDouble(dt.Rows[0]["monthly_installment"]);
                    if (monthly <= 0.009) monthly = SafeDouble(dt.Rows[0]["block_monthly"]);
                }

                var already = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var paidDt = SelectQ(@"
SELECT receipt_date, notes FROM receipts
WHERE customer_plot_id=@id AND amount > 0
  AND (@ex = 0 OR id <> @exid);",
                    new Dictionary<string, object>
                    {
                        { "@id", customerPlotId },
                        { "@ex", _editingReceiptId.HasValue ? 1 : 0 },
                        { "@exid", _editingReceiptId ?? 0 }
                    });
                if (paidDt != null)
                {
                    foreach (DataRow row in paidDt.Rows)
                    {
                        foreach (var k in HubBuilders.InstallmentCoverHelper.ParseKeys(Convert.ToString(row["notes"])))
                            already.Add(k);
                        DateTime d;
                        if (DateTime.TryParse(Convert.ToString(row["receipt_date"]), out d))
                            already.Add(d.ToString("yyyy-MM"));
                    }
                }

                var keys = HubBuilders.InstallmentCoverHelper.ComputeCoverKeys(
                    booking, DateTime.Today, amount, monthly, already);
                return HubBuilders.InstallmentCoverHelper.ApplyToken(notes, keys);
            }
            catch
            {
                return notes;
            }
        }

        // =============== Save (Insert OR Update) ===============
        private void BtnSaveReceipt_Click(object sender, EventArgs e)
        {
            if (CurrentCustomerPlotId == null) { MessageBox.Show("Pehle allocation select karo."); return; }
            if (numAmount.Value <= 0) { MessageBox.Show("Amount must be > 0."); return; }
            if (cmbMode.SelectedItem == null) { MessageBox.Show("Mode select karo."); return; }

            var cpId = CurrentCustomerPlotId.Value;
            var amount = (double)numAmount.Value;
            var mode = cmbMode.SelectedItem.ToString(); // 'Cash' or 'Bank'
            var refNo = string.IsNullOrWhiteSpace(txtRefNo.Text) ? null : txtRefNo.Text.Trim();
            var notes = string.IsNullOrWhiteSpace(txtReceiptNotes.Text) ? null : txtReceiptNotes.Text.Trim();
            notes = ApplyInstallmentCoverIfChecked(cpId, amount, notes);
            var receiptDate = dtpReceiptDate.Value.Date.ToString("yyyy-MM-dd");

            bool isUpdate = _editingReceiptId.HasValue;

            SQLiteTransaction tx = null;
            try
            {
                HubBuilders.MainClass.AcquireSharedConnection();

                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", HubBuilders.MainClass.con))
                    pragma.ExecuteNonQuery();

                tx = HubBuilders.MainClass.con.BeginTransaction();

                if (isUpdate)
                {
                    // Update ledger
                    if (_editingLedgerId.HasValue)
                    {
                        using (var cmd = new SQLiteCommand(@"
UPDATE ledger
SET txn_date=@date, credit=@amt, txn_type=@tt, narration=@nar
WHERE id=@lid;", HubBuilders.MainClass.con, tx))
                        {
                            cmd.Parameters.AddWithValue("@date", receiptDate);
                            cmd.Parameters.AddWithValue("@amt", amount);
                            cmd.Parameters.AddWithValue("@tt", mode == "Bank" ? "ReceiptBank" : "ReceiptCash");
                            cmd.Parameters.AddWithValue("@nar", (object)(notes ?? $"Receipt ({mode})"));
                            cmd.Parameters.AddWithValue("@lid", _editingLedgerId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Update receipt
                    using (var cmd = new SQLiteCommand(@"
UPDATE receipts
SET receipt_date=@date, amount=@amt, mode=@mode, ref_no=@ref, notes=@notes
WHERE id=@rid;", HubBuilders.MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@mode", mode);
                        cmd.Parameters.AddWithValue("@ref", (object)(refNo ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@notes", (object)(notes ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@rid", _editingReceiptId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // INSERT: 1) ledger credit
                    long ledgerId;
                    using (var cmd = new SQLiteCommand(@"
INSERT INTO ledger (customer_plot_id, txn_date, voucher_no, narration, debit, credit, txn_type, sync_id)
VALUES (@cpid, @date, @vno, @nar, 0, @amt, @tt, lower(hex(randomblob(16))));
SELECT last_insert_rowid();", HubBuilders.MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", cpId);
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@vno", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@nar", (object)(notes ?? $"Receipt ({mode})"));
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@tt", mode == "Bank" ? "ReceiptBank" : "ReceiptCash");
                        ledgerId = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    // 2) receipt
                    using (var cmd = new SQLiteCommand(@"
INSERT INTO receipts (customer_plot_id, receipt_date, amount, mode, ref_no, notes, ledger_id, sync_id)
VALUES (@cpid, @date, @amt, @mode, @ref, @notes, @lid, lower(hex(randomblob(16))));", HubBuilders.MainClass.con, tx))
                    {
                        cmd.Parameters.AddWithValue("@cpid", cpId);
                        cmd.Parameters.AddWithValue("@date", receiptDate);
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@mode", mode);
                        cmd.Parameters.AddWithValue("@ref", (object)(refNo ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@notes", (object)(notes ?? (object)DBNull.Value));
                        cmd.Parameters.AddWithValue("@lid", ledgerId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", HubBuilders.MainClass.con, tx))
                        _editingReceiptId = Convert.ToInt64(idCmd.ExecuteScalar());
                    _editingLedgerId = ledgerId;
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                MessageBox.Show((isUpdate ? "Update" : "Save") + " failed: " + ex.Message);
                return;
            }
            finally
            {
                HubBuilders.MainClass.ReleaseSharedConnection();
            }

            try
            {
                HubBuilders.MainClass.LogStaffEntry(
                    isUpdate ? "Update" : "Insert",
                    "receipts",
                    "ReceiptFormAdd save");
            }
            catch { }

            try
            {
                var syncRows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>();
                if (_editingLedgerId.HasValue && _editingLedgerId.Value > 0)
                    syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("ledger", _editingLedgerId.Value));
                if (_editingReceiptId.HasValue && _editingReceiptId.Value > 0)
                    syncRows.Add(new System.Collections.Generic.KeyValuePair<string, long>("receipts", _editingReceiptId.Value));
                if (syncRows.Count > 0)
                    HubSyncService.NotifyLocalWrites(syncRows);
                else
                    HubSyncService.NotifyLocalWrite();
            }
            catch { try { HubSyncService.NotifyLocalWrite(); } catch { } }

            // reset edit state & UI
            _editingReceiptId = null;
            _editingLedgerId = null;
            btnSaveReceipt.Text = "Save";
            SuggestNextRefNo();
            txtReceiptNotes.Clear();
            numAmount.Value = 0;
            if (_chkCoverInstallments != null) _chkCoverInstallments.Checked = false;

            RefreshGrid();
        }
    }
}
