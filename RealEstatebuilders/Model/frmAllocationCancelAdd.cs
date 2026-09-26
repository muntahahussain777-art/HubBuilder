using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmAllocationCancelAdd : SampleAdd
    {
        // Parent form se allocation id aayegi
        public int CustomerPlotId { get; set; } = 0;

        // Save ke baad useful return values
        public long SavedCancellationId { get; private set; } = 0;
        public double SavedRefundableAmount { get; private set; } = 0;
        public double SavedDeductionAmount { get; private set; } = 0;

        private int _plotId = 0;
        private string _currentStatus = "";
        private double _allocationAmount = 0;
        private double _receivedAmount = 0;
        private int _receiptCount = 0;

        public frmAllocationCancelAdd()
        {
            InitializeComponent();
            this.Load += frmAllocationCancelAdd_Load;
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        private void frmAllocationCancelAdd_Load(object sender, EventArgs e)
        {
            try
            {
                MainClass.EnsureAllocationSchema();
                ApplyCancelFormStyle();

                dtpCancelDate.Format = DateTimePickerFormat.Short;
                dtpCancelDate.Value = DateTime.Today;

                txtAllocation.ReadOnly = true;
                txtAllocationAmount.ReadOnly = true;
                txtReceivedAmount.ReadOnly = true;
                txtDeductionAmount.ReadOnly = true;
                txtRefundableAmount.ReadOnly = true;

                cmbCustomerPlot.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbCustomerPlot.Enabled = false; // sirf display ke liye

                // Meri marzi: koi bhi % (0–100) type / select
                cmbDeductionPercent.Items.Clear();
                foreach (var p in new[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "60", "70", "80", "90", "100" })
                    cmbDeductionPercent.Items.Add(p);

                cmbDeductionPercent.DropDownStyle = ComboBoxStyle.DropDown; // type bhi kar sako
                cmbDeductionPercent.Text = "30";

                cmbDeductionPercent.SelectedIndexChanged -= cmbDeductionPercent_SelectedIndexChanged;
                cmbDeductionPercent.SelectedIndexChanged += cmbDeductionPercent_SelectedIndexChanged;
                cmbDeductionPercent.TextChanged -= cmbDeductionPercent_TextChanged;
                cmbDeductionPercent.TextChanged += cmbDeductionPercent_TextChanged;
                cmbDeductionPercent.Leave -= cmbDeductionPercent_Leave;
                cmbDeductionPercent.Leave += cmbDeductionPercent_Leave;

                // Labels: clear meaning (layout same)
                try
                {
                    gunaLabel5.Text = "Cancel Date";
                    gunaLabel6.Text = "Khatoti %";
                    gunaLabel8.Text = "Khatoti Amount";
                    gunaLabel7.Text = "Refundable Amount";
                }
                catch { }

                if (CustomerPlotId <= 0)
                {
                    ShowError("Allocation select nahi hui.");
                    this.DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }

                LoadAllocationData();
                RecalculateAmounts();
            }
            catch (Exception ex)
            {
                ShowError("Cancel form load failed.\n\n" + ex.Message);
                this.DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void ApplyCancelFormStyle()
        {
            try
            {
                this.Text = "Cancel Allocation";
                label1.Text = "Cancel Allocation";
                this.BackColor = System.Drawing.Color.FromArgb(32, 36, 61);

                var accent = System.Drawing.Color.FromArgb(112, 51, 255);
                var fieldBg = System.Drawing.Color.FromArgb(37, 41, 74);
                var white = System.Drawing.Color.White;

                foreach (Control c in this.Controls)
                {
                    if (c is Guna.UI.WinForms.GunaLabel gl)
                    {
                        gl.ForeColor = white;
                        gl.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
                    }
                    else if (c is Guna.UI.WinForms.GunaTextBox tb)
                    {
                        tb.BaseColor = fieldBg;
                        tb.BorderColor = accent;
                        tb.ForeColor = white;
                        tb.FocusedForeColor = System.Drawing.Color.Black;
                    }
                    else if (c is Guna.UI.WinForms.GunaComboBox cb)
                    {
                        cb.BaseColor = fieldBg;
                        cb.BorderColor = accent;
                        cb.ForeColor = white;
                    }
                    else if (c is Guna.UI.WinForms.GunaDateTimePicker dp)
                    {
                        dp.BaseColor = fieldBg;
                        dp.BorderColor = accent;
                        dp.ForeColor = white;
                        dp.FocusedColor = accent;
                    }
                }

                // Highlight money fields
                txtDeductionAmount.BorderColor = System.Drawing.Color.FromArgb(220, 53, 69);
                txtRefundableAmount.BorderColor = System.Drawing.Color.FromArgb(40, 167, 69);
                cmbDeductionPercent.BorderColor = System.Drawing.Color.FromArgb(255, 193, 7);
            }
            catch { }
        }

        private void cmbDeductionPercent_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecalculateAmounts();
        }

        private void cmbDeductionPercent_TextChanged(object sender, EventArgs e)
        {
            RecalculateAmounts();
        }

        private void cmbDeductionPercent_Leave(object sender, EventArgs e)
        {
            double p = GetSelectedPercent();
            if (p < 0) p = 0;
            if (p > 100) p = 100;
            cmbDeductionPercent.Text = p.ToString("0.##");
            RecalculateAmounts();
        }

        private void LoadAllocationData()
        {
            var dt = MainClass.ExecuteSelectQuery1(@"
SELECT 
    cp.id,
    cp.plot_id,
    COALESCE(cp.price,0) AS price,
    COALESCE(cp.status,'') AS status,
    c.name || ' | Plot ' || p.plot_no || ' | ' || s.name || ' | ' || b.name AS alloc_disp
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
WHERE cp.id = @id;",
                new Dictionary<string, object> { { "@id", CustomerPlotId } });

            if (dt == null || dt.Rows.Count != 1)
                throw new Exception("Allocation record not found.");

            var r = dt.Rows[0];

            _plotId = Convert.ToInt32(r["plot_id"]);
            _currentStatus = Convert.ToString(r["status"]);
            _allocationAmount = SafeDouble(r["price"]);

            if (string.Equals(_currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Ye allocation pehle hi cancelled hai.");

            if (string.Equals(_currentStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Completed allocation cancel nahi ho sakti.");

            string allocText = Convert.ToString(r["alloc_disp"]);

            txtAllocation.Text = allocText;

            cmbCustomerPlot.DataSource = null;
            cmbCustomerPlot.Items.Clear();

            DataTable dtAlloc = new DataTable();
            dtAlloc.Columns.Add("id", typeof(int));
            dtAlloc.Columns.Add("disp", typeof(string));
            dtAlloc.Rows.Add(CustomerPlotId, allocText);

            cmbCustomerPlot.DisplayMember = "disp";
            cmbCustomerPlot.ValueMember = "id";
            cmbCustomerPlot.DataSource = dtAlloc;
            cmbCustomerPlot.SelectedValue = CustomerPlotId;

            txtAllocationAmount.Text = _allocationAmount.ToString("N2");

            var objRecv = MainClass.ExecuteScalar(
                "SELECT COALESCE(SUM(amount),0) FROM receipts WHERE customer_plot_id=@cpid;",
                new Dictionary<string, object> { { "@cpid", CustomerPlotId } });

            _receivedAmount = SafeDouble(objRecv);
            txtReceivedAmount.Text = _receivedAmount.ToString("N2");

            var objCnt = MainClass.ExecuteScalar(
                "SELECT COUNT(1) FROM receipts WHERE customer_plot_id=@cpid;",
                new Dictionary<string, object> { { "@cpid", CustomerPlotId } });
            _receiptCount = Convert.ToInt32(objCnt ?? 0);
        }

        private void RecalculateAmounts()
        {
            double percent = GetSelectedPercent();
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            double deduction = Math.Round((_receivedAmount * percent) / 100.0, 2);
            double refundable = Math.Round(_receivedAmount - deduction, 2);
            if (refundable < 0) refundable = 0;

            txtDeductionAmount.Text = deduction.ToString("N2");
            txtRefundableAmount.Text = refundable.ToString("N2");
        }

        private double GetSelectedPercent()
        {
            string raw = (cmbDeductionPercent.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw) && cmbDeductionPercent.SelectedItem != null)
                raw = cmbDeductionPercent.SelectedItem.ToString();

            raw = raw.Replace("%", "").Trim();
            if (!double.TryParse(raw, out double p))
                return 0;
            return p;
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double.TryParse(o.ToString().Replace(",", ""), out double v);
            return v;
        }

        public override void btnSave_Click(object sender, EventArgs e)
        {
            if (CustomerPlotId <= 0)
            {
                ShowInfo("Allocation select karein.");
                return;
            }

            if (string.Equals(_currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                ShowInfo("Ye allocation already cancelled hai.");
                return;
            }

            if (string.Equals(_currentStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                ShowInfo("Completed allocation cancel nahi ho sakti.");
                return;
            }

            if (!string.Equals(_currentStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                ShowInfo("Sirf Active allocation cancel ho sakti hai.");
                return;
            }

            double deductionPercent = GetSelectedPercent();
            if (deductionPercent < 0 || deductionPercent > 100)
            {
                ShowInfo("Khatoti % 0 se 100 ke darmiyan likhein (jaise 20, 30, 17.5).");
                cmbDeductionPercent.Focus();
                return;
            }

            RecalculateAmounts();

            double deductionAmount = SafeDouble(txtDeductionAmount.Text);
            double refundableAmount = SafeDouble(txtRefundableAmount.Text);
            string cancelDate = dtpCancelDate.Value.ToString("yyyy-MM-dd");
            string notes = string.IsNullOrWhiteSpace(txtCancelNotes.Text) ? null : txtCancelNotes.Text.Trim();

            string confirmMsg =
                "Allocation cancel karna hai?\n\n" +
                "Received: " + _receivedAmount.ToString("N2") + "\n" +
                "Khatoti (" + deductionPercent.ToString("0.##") + "%): " + deductionAmount.ToString("N2") + "\n" +
                "Refundable (customer): " + refundableAmount.ToString("N2") + "\n" +
                "Receipts (" + _receiptCount + "): history mein is cancelled allocation pe rahengi — delete nahi hongi.\n\n" +
                "Plot Available ho jayega (dobara allocate ho sakta hai).";

            if (!AskYesNo(confirmMsg, "Confirm Cancellation"))
                return;

            SQLiteTransaction tx = null;

            try
            {
                MainClass.AcquireSharedConnection();

                using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", MainClass.con))
                    pragma.ExecuteNonQuery();

                tx = MainClass.con.BeginTransaction();

                using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(status,''), plot_id
FROM customer_plots
WHERE id=@id;", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@id", CustomerPlotId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new Exception("Allocation record not found.");

                        string dbStatus = Convert.ToString(rd[0]);
                        _plotId = Convert.ToInt32(rd[1]);

                        if (string.Equals(dbStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Ye allocation already cancelled hai.");

                        if (string.Equals(dbStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Completed allocation cancel nahi ho sakti.");

                        if (!string.Equals(dbStatus, "Active", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Sirf Active allocation cancel ho sakti hai.");
                    }
                }

                long cancellationId = 0;

                using (var cmd = new SQLiteCommand(@"
INSERT INTO allocation_cancellations
(
    customer_plot_id,
    cancel_date,
    deduction_percent,
    allocation_amount,
    received_amount,
    deduction_amount,
    refundable_amount,
    notes
)
VALUES
(
    @cpid,
    @cdate,
    @dpercent,
    @allocamt,
    @recvamt,
    @dedamt,
    @refundamt,
    @notes
);
SELECT last_insert_rowid();", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@cpid", CustomerPlotId);
                    cmd.Parameters.AddWithValue("@cdate", cancelDate);
                    cmd.Parameters.AddWithValue("@dpercent", deductionPercent);
                    cmd.Parameters.AddWithValue("@allocamt", _allocationAmount);
                    cmd.Parameters.AddWithValue("@recvamt", _receivedAmount);
                    cmd.Parameters.AddWithValue("@dedamt", deductionAmount);
                    cmd.Parameters.AddWithValue("@refundamt", refundableAmount);
                    cmd.Parameters.AddWithValue("@notes", (object)(notes ?? (object)DBNull.Value));

                    cancellationId = Convert.ToInt64(cmd.ExecuteScalar());
                }

                // Khatoti / refund amounts allocation_cancellations mein save — receipts history pe rehti hain

                using (var cmd = new SQLiteCommand(@"
UPDATE customer_plots
SET status = 'Cancelled',
    cancelled_date = @cdate,
    cancellation_id = @cid,
    notes = TRIM(COALESCE(notes,'') || @noteAdd)
WHERE id = @id;", MainClass.con, tx))
                {
                    string noteAdd = " | Cancelled " + cancelDate
                        + " | Khatoti " + deductionPercent.ToString("0.##") + "%=" + deductionAmount.ToString("N2")
                        + " | Refundable=" + refundableAmount.ToString("N2")
                        + " | Receipts kept in history (" + _receiptCount + ")";

                    cmd.Parameters.AddWithValue("@cdate", cancelDate);
                    cmd.Parameters.AddWithValue("@cid", cancellationId);
                    cmd.Parameters.AddWithValue("@noteAdd", noteAdd);
                    cmd.Parameters.AddWithValue("@id", CustomerPlotId);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows <= 0)
                        throw new Exception("Allocation status update fail ho gaya.");
                }

                using (var cmd = new SQLiteCommand(@"
UPDATE plots
SET status = 'Available'
WHERE id = @pid;", MainClass.con, tx))
                {
                    cmd.Parameters.AddWithValue("@pid", _plotId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();

                try { HubSyncService.NotifyLocalWrite(); } catch { }

                SavedCancellationId = cancellationId;
                SavedRefundableAmount = refundableAmount;
                SavedDeductionAmount = deductionAmount;

                try
                {
                    MainClass.LogStaffEntry("Insert", "allocation_cancellations",
                        $"cpid={CustomerPlotId}; refund={refundableAmount:N2}");
                }
                catch { }

                ShowInfo(
                    "Allocation cancel ho gayi.\n\n" +
                    "• Plot ab Available hai — dobara allocate kar sakte ho.\n" +
                    "• " + _receiptCount + " receipt(s) history mein isi cancelled allocation pe hain (delete nahi hui).\n" +
                    "• Khatoti (aapki): " + deductionAmount.ToString("N2") + "\n" +
                    "• Customer ko refund: " + refundableAmount.ToString("N2"));

                this.DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                ShowError("Cancellation failed.\n\n" + ex.Message);
            }
            finally
            {
                MainClass.ReleaseSharedConnection();
            }
        }

        private void ShowError(string message)
        {
            try
            {
                using (var err = new ErrorFormMessage(message, "Error"))
                    err.ShowDialog();
            }
            catch
            {
                MessageBox.Show(message, "Error");
            }
        }

        private void ShowInfo(string message)
        {
            try
            {
                using (var ok = new CustomeMessage(message, "Hub Builders"))
                    ok.ShowDialog();
            }
            catch
            {
                MessageBox.Show(message, "Info");
            }
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
