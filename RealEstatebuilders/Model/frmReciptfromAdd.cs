using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmReciptfromAdd : SampleAdd
    {
        public int CustomerPlotId { get; set; } = 0;
        public long? EditingReceiptId { get; set; } = null;

        public int SavedCustomerPlotId { get; private set; } = 0;
        public double SavedRemaining { get; private set; } = 0;

        private long? _editingLedgerId = null;
        private ComboBox _cmbPartner;
        private bool _partnerUiReady;

        public frmReciptfromAdd()
        {
            InitializeComponent();
            this.Load += FrmReceiptFormAdd_Load;
        }

        private void SetSaveButtonText(string text)
        {
            if (this.AcceptButton is Button b) b.Text = text;
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        private void FrmReceiptFormAdd_Load(object sender, EventArgs e)
        {
            try
            {
            if (cmbMode.Items.Count == 0) cmbMode.Items.AddRange(new[] { "Cash", "Bank" });
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            if (cmbMode.SelectedIndex < 0) cmbMode.SelectedIndex = 0;
            dtpReceiptDate.Format = DateTimePickerFormat.Short;

            try { label1.Text = "Add Receipt"; } catch { }

            try
            {
                numAmount.DataBindings?.Clear();
                numAmount.Minimum = 0;
                numAmount.Maximum = 1000000000;
                numAmount.DecimalPlaces = 2;
            }
            catch { }

            MainClass.LoadDataIntoComboBox(@"
SELECT cp.id AS id, c.name || ' | Plot ' || p.plot_no AS disp
FROM customer_plots cp
JOIN customers c ON c.id=cp.customer_id
JOIN plots     p ON p.id=cp.plot_id
WHERE cp.status='Active'
ORDER BY c.name, p.plot_no;",
                cmbCustomerPlot, "disp", "id");

            if (CustomerPlotId > 0)
                cmbCustomerPlot.SelectedValue = CustomerPlotId;

            if (!EditingReceiptId.HasValue)
            {
                try
                {
                    numAmount.Value = 0;
                    SuggestNextRefNo();
                    txtReceiptNotes.Clear();
                    dtpReceiptDate.Value = DateTime.Today;
                }
                catch { }
            }
            else
            {
                LoadForEdit(EditingReceiptId.Value);
            }

            cmbCustomerPlot.SelectionChangeCommitted += (s, ev) => { };
            cmbCustomerPlot.SelectedValueChanged += (s, ev) => { };

            cmbCustomerPlot.IntegralHeight = false;   // exact height use karo (scrollbar enable)
            cmbCustomerPlot.MaxDropDownItems = 20;    // ek view me 20 items
            cmbCustomerPlot.DropDownHeight = 320;     // ~16px * 20 (adjustable)
            cmbCustomerPlot.DropDownStyle = ComboBoxStyle.DropDownList; // safer UX

            EnsurePartnerCombo();
            TryPrefillPartnerFromAllocation();
            cmbCustomerPlot.SelectedValueChanged += (s2, ev2) => TryPrefillPartnerFromAllocation();
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        /// <summary>
        /// Auto Ref No: 001, 002… fills gaps after delete. Existing custom refs untouched.
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
            var used = new System.Collections.Generic.HashSet<int>();
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

        private void EnsurePartnerCombo()
        {
            if (_partnerUiReady) return;
            _partnerUiReady = true;
            try
            {
                MainClass.EnsurePartnerMoneySchema();
                var lbl = new Label
                {
                    Text = "Partner (paise lene wala)",
                    ForeColor = System.Drawing.Color.White,
                    Font = new System.Drawing.Font("Segoe UI Semibold", 9.5f, System.Drawing.FontStyle.Bold),
                    AutoSize = true,
                    Location = new System.Drawing.Point(548, 183)
                };
                Controls.Add(lbl);
                lbl.BringToFront();

                _cmbPartner = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new System.Drawing.Point(535, 207),
                    Size = new System.Drawing.Size(248, 28),
                    Font = new System.Drawing.Font("Segoe UI", 10f)
                };
                Controls.Add(_cmbPartner);
                _cmbPartner.BringToFront();

                MainClass.LoadDataIntoComboBox(
                    "SELECT person_id AS id, name FROM Person WHERE is_partner=1 ORDER BY name;",
                    _cmbPartner, "name", "id");
                _cmbPartner.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void TryPrefillPartnerFromAllocation()
        {
            try
            {
                if (_cmbPartner == null) return;
                if (!TryGetCustomerPlotId(out int cpId) || cpId <= 0) return;
                var pid = MainClass.GetAllocationPartnerId(cpId);
                if (pid.HasValue && pid.Value > 0)
                    _cmbPartner.SelectedValue = pid.Value;
            }
            catch { }
        }

        private bool TryGetCustomerPlotId(out int cpId)
        {
            cpId = 0;
            try
            {
                if (cmbCustomerPlot.SelectedValue == null) return false;
                cpId = Convert.ToInt32(cmbCustomerPlot.SelectedValue);
                return cpId > 0;
            }
            catch { return false; }
        }

        private int SelectedPartnerId()
        {
            try
            {
                if (_cmbPartner?.SelectedValue == null || _cmbPartner.SelectedValue == DBNull.Value) return 0;
                return Convert.ToInt32(_cmbPartner.SelectedValue);
            }
            catch { return 0; }
        }

        private void LoadForEdit(long receiptId)
        {
            try
            {
            var dt = MainClass.ExecuteSelectQuery1(@"
SELECT r.id, r.customer_plot_id, r.receipt_date, r.amount, r.mode, r.ref_no, r.notes, r.ledger_id
FROM receipts r WHERE r.id=@id;",
                new Dictionary<string, object> { { "@id", receiptId } });

            if (dt == null || dt.Rows.Count != 1)
            {
                MessageBox.Show("Receipt not found.");
                this.DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            var r = dt.Rows[0];
            try { cmbCustomerPlot.SelectedValue = Convert.ToInt32(r["customer_plot_id"]); } catch { }
            try
            {
                if (r["receipt_date"] != DBNull.Value
                    && DateTime.TryParse(Convert.ToString(r["receipt_date"]), out DateTime rd))
                    dtpReceiptDate.Value = rd;
                else
                    dtpReceiptDate.Value = DateTime.Today;
            }
            catch { dtpReceiptDate.Value = DateTime.Today; }
            try
            {
                decimal amt = Convert.ToDecimal(SafeDouble(r["amount"]));
                if (amt < numAmount.Minimum) amt = numAmount.Minimum;
                if (amt > numAmount.Maximum) amt = numAmount.Maximum;
                numAmount.Value = amt;
            }
            catch { numAmount.Value = numAmount.Minimum; }
            cmbMode.SelectedItem = r["mode"]?.ToString() == "Bank" ? "Bank" : "Cash";
            txtRefNo.Text = r["ref_no"] == DBNull.Value ? "" : r["ref_no"].ToString();
            txtReceiptNotes.Text = r["notes"] == DBNull.Value ? "" : r["notes"].ToString();
            _editingLedgerId = r["ledger_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["ledger_id"]);
            try { label1.Text = "Edit Receipt"; } catch { }

            EnsurePartnerCombo();
            try
            {
                var pdt = MainClass.ExecuteSelectQuery1(
                    "SELECT partner_id FROM partner_money WHERE receipt_id=@rid LIMIT 1;",
                    new Dictionary<string, object> { { "@rid", receiptId } });
                if (pdt != null && pdt.Rows.Count > 0 && _cmbPartner != null)
                    _cmbPartner.SelectedValue = Convert.ToInt32(pdt.Rows[0]["partner_id"]);
                else
                    TryPrefillPartnerFromAllocation();
            }
            catch { TryPrefillPartnerFromAllocation(); }
            _editingLedgerId = r["ledger_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["ledger_id"]);

            this.Text = $"Edit Receipt #{receiptId}";
            SetSaveButtonText("Update");
            try { label1.Text = "Edit Receipt"; } catch { }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            return double.TryParse(o.ToString(), out var v) ? v : 0d;
        }

        private (double opening, double totalCredit, double remaining) GetRemaining(int cpId, long? excludeReceiptId)
        {
            double tDebit = 0, price = 0;

            var objD = MainClass.ExecuteScalar(
                "SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=@cpid",
                new Dictionary<string, object> { { "@cpid", cpId } });
            if (objD != null && objD != DBNull.Value) double.TryParse(objD.ToString(), out tDebit);

            var objP = MainClass.ExecuteScalar(
                "SELECT COALESCE(price,0) FROM customer_plots WHERE id=@cpid",
                new Dictionary<string, object> { { "@cpid", cpId } });
            if (objP != null && objP != DBNull.Value) double.TryParse(objP.ToString(), out price);

            double opening = tDebit > 0 ? tDebit : price;

            string sqlC = "SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=@cpid";
            var pr = new Dictionary<string, object> { { "@cpid", cpId } };
            if (excludeReceiptId.HasValue)
            {
                sqlC += " AND id<>@rid";
                pr["@rid"] = excludeReceiptId.Value;
            }
            var objC = MainClass.ExecuteScalar(sqlC, pr);
            double totalCredit = (objC == null || objC == DBNull.Value) ? 0 : Convert.ToDouble(objC);

            double remaining = opening - totalCredit;
            if (remaining < 0) remaining = 0;

            return (opening, totalCredit, remaining);
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbCustomerPlot.SelectedValue == null) { MessageBox.Show("Allocation select karein."); return; }
            int cpId;
            try { cpId = Convert.ToInt32(cmbCustomerPlot.SelectedValue); }
            catch
            {
                MessageBox.Show("Allocation select karein.");
                return;
            }
            if (cpId <= 0) { MessageBox.Show("Allocation select karein."); return; }
            if (numAmount.Value <= 0) { MessageBox.Show("Amount > 0 required."); return; }
            if (cmbMode.SelectedItem == null) { MessageBox.Show("Mode select karein."); return; }

            double amount = (double)numAmount.Value;
            string mode = cmbMode.SelectedItem.ToString();
            string refNo = string.IsNullOrWhiteSpace(txtRefNo.Text) ? null : txtRefNo.Text.Trim();
            string notes = string.IsNullOrWhiteSpace(txtReceiptNotes.Text) ? null : txtReceiptNotes.Text.Trim();
            string receiptDate = dtpReceiptDate.Value.ToString("yyyy-MM-dd");

            // Overpayment Guard
            var rm = GetRemaining(cpId, EditingReceiptId);
            double maxAllowed = rm.remaining;
            if (maxAllowed <= 0)
            {
                MessageBox.Show("Is allocation ka debit settle ho chuka hai. New receipt add/update nahi ho sakti.");
                return;
            }
            if (amount > maxAllowed)
            {
                MessageBox.Show($"Amount remaining se zyada hai.\nRemaining: {maxAllowed:N2}\nPlease enter equal or less.");
                try { numAmount.Value = (decimal)maxAllowed; } catch { }
                numAmount.Focus();
                return;
            }

            bool isUpdate = EditingReceiptId.HasValue;

            SQLiteTransaction tx = null;
            try
            {
                HubBuilders.MainClass.AcquireSharedConnection();
                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", HubBuilders.MainClass.con))
                    pragma.ExecuteNonQuery();

                tx = HubBuilders.MainClass.con.BeginTransaction();

                if (isUpdate)
                {
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
                        cmd.Parameters.AddWithValue("@rid", EditingReceiptId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    long ledgerId;
                    using (var cmd = new SQLiteCommand(@"
INSERT INTO ledger (customer_plot_id, txn_date, voucher_no, narration, debit, credit, txn_type)
VALUES (@cpid, @date, @vno, @nar, 0, @amt, @tt);
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
                    using (var cmd = new SQLiteCommand(@"
INSERT INTO receipts (customer_plot_id, receipt_date, amount, mode, ref_no, notes, ledger_id)
VALUES (@cpid, @date, @amt, @mode, @ref, @notes, @lid);", HubBuilders.MainClass.con, tx))
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
                        EditingReceiptId = Convert.ToInt64(idCmd.ExecuteScalar());
                }

                // Partner cash In — receipt ke paise partner ke hisab mein
                // (partner clear ho to pehli entry delete; Upsert handle karta hai)
                int partnerId = SelectedPartnerId();
                long ridForPartner = EditingReceiptId ?? 0;
                if (ridForPartner > 0)
                {
                    MainClass.UpsertPartnerMoneyForReceipt(
                        ridForPartner,
                        partnerId,
                        cpId,
                        receiptDate,
                        (decimal)amount,
                        mode,
                        notes,
                        HubBuilders.MainClass.con,
                        tx);
                }

                tx.Commit();

                try { HubSyncService.NotifyLocalWrite(); } catch { }

                // clear edit flag if we used it as temp holder for new id
                if (!isUpdate) EditingReceiptId = null;

                // Return info to View
                var rm2 = GetRemaining(cpId, null);
                SavedCustomerPlotId = cpId;
                SavedRemaining = rm2.remaining;

                try
                {
                    HubBuilders.MainClass.LogStaffEntry(
                        isUpdate ? "Update" : "Insert",
                        "receipts",
                        $"amount={amount}; mode={mode}; cpid={cpId}; partner={partnerId}");
                }
                catch { }

                if (rm2.remaining <= 0)
                {
                    MessageBox.Show(
                        "Payment completed. No further receipts allowed for this allocation.",
                        "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                MessageBox.Show((isUpdate ? "Update" : "Save") + " failed: " + ex.Message);
            }
            finally
            {
                HubBuilders.MainClass.ReleaseSharedConnection();
            }
        }
    }
}
