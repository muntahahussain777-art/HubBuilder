using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace HubBuilders.Model
{
    public partial class frmInstallmentPlanFormsAdd : SampleAdd
    {
        // Edit mode (installment_plans.id)
        public int id = 0;

        public frmInstallmentPlanFormsAdd()
        {
            InitializeComponent();

            // defaults
            if (txtdate != null) txtdate.Value = DateTime.Today;
            if (numfreqencymonth != null) { numfreqencymonth.Minimum = 1; numfreqencymonth.Maximum = 120; }
            if (numcount != null) { numcount.Minimum = 1; numcount.Maximum = 1000; }
            if (numperInstallation != null) { numperInstallation.Minimum = 1; numperInstallation.Maximum = decimal.MaxValue / 1000; numperInstallation.DecimalPlaces = 2; numperInstallation.ThousandsSeparator = true; }
            if (numGraceDays != null) { numGraceDays.Minimum = 0; numGraceDays.Maximum = 365; }

            LoadPlotsChoices(); // cbcustomer now shows plots
        }

        // Ctrl+S
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public override void btnClose_Click(object sender, EventArgs e)
        {
            SafeClose();
        }

        // --- Data sources -----------------------------------------------------

        // Populate combo with plots.id + plots.plot_no
        private void LoadPlotsChoices()
        {
            try
            {
                // Minimal label: only plot_no as requested
                string q = @"
                    SELECT p.id   AS plot_id,
                           p.plot_no AS label
                    FROM plots p
                    ORDER BY p.plot_no COLLATE NOCASE";
                DataTable dt = MainClass.ExecuteSelectQuery(q, null);

                cbcustomer.DataSource = dt;            // reuse existing control
                cbcustomer.DisplayMember = "label";
                cbcustomer.ValueMember = "plot_id";
                if (cbcustomer.Items.Count > 0) cbcustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Plot list load failure: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Resolve customer_plots.id for a given plots.id
        // Assumes customer_plots has a plot_id FK. If your schema differs, adjust the WHERE.
        private int? ResolveCustomerPlotId(int plotId)
        {
            string q = @"SELECT id FROM customer_plots WHERE plot_id=@pid LIMIT 1";
            Hashtable ht = new Hashtable { { "@pid", plotId } };
            object obj = MainClass.SQLScalar(q, ht);
            if (obj == null || obj == DBNull.Value) return null;
            int val;
            if (int.TryParse(obj.ToString(), out val)) return val;
            return null;
        }

        // In edit mode, map installment_plans.customer_plot_id -> plots.id to preselect combo
        private int? GetPlotIdForCustomerPlot(int customerPlotId)
        {
            string q = @"SELECT plot_id FROM customer_plots WHERE id=@cpid LIMIT 1";
            Hashtable ht = new Hashtable { { "@cpid", customerPlotId } };
            object obj = MainClass.SQLScalar(q, ht);
            if (obj == null || obj == DBNull.Value) return null;
            int val;
            if (int.TryParse(obj.ToString(), out val)) return val;
            return null;
        }

        // --- Save -------------------------------------------------------------

        public override void btnSave_Click(object sender, EventArgs e)
        {
            // Validate plot selection
            if (cbcustomer.SelectedValue == null)
            {
                MessageBox.Show("Plot required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbcustomer.Focus(); return;
            }

            if (numfreqencymonth.Value < 1)
            {
                MessageBox.Show("Frequency (months) must be >= 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numfreqencymonth.Focus(); return;
            }
            if (numcount.Value < 1)
            {
                MessageBox.Show("Installment count must be >= 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numcount.Focus(); return;
            }
            if (numperInstallation.Value <= 0)
            {
                MessageBox.Show("Per-installment amount must be > 0.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numperInstallation.Focus(); return;
            }
            if (numGraceDays.Value < 0)
            {
                MessageBox.Show("Grace days must be >= 0.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numGraceDays.Focus(); return;
            }

            // Data prep
            int plotId = Convert.ToInt32(cbcustomer.SelectedValue); // now holds plots.id
            int? customerPlotId = ResolveCustomerPlotId(plotId);
            if (customerPlotId == null)
            {
                MessageBox.Show("Is plot ke liye customer_plots record nahin mila. Pehle customer ko is plot se map karein.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbcustomer.Focus(); return;
            }

            string startDate = (txtdate?.Value ?? DateTime.Today).ToString("yyyy-MM-dd");
            int freq = Decimal.ToInt32(numfreqencymonth.Value);
            int count = Decimal.ToInt32(numcount.Value);
            double perAmt = Convert.ToDouble(numperInstallation.Value);
            int grace = Decimal.ToInt32(numGraceDays.Value);

            // Uniqueness check on customer_plot_id (exclude self on edit)
            string dupQry = "SELECT COUNT(1) FROM installment_plans WHERE customer_plot_id=@cpid " + (id > 0 ? "AND id<>@id" : "");
            Hashtable dupHt = new Hashtable { { "@cpid", customerPlotId.Value } };
            if (id > 0) dupHt.Add("@id", id);
            int dupCount = Convert.ToInt32(MainClass.SQLScalar(dupQry, dupHt) ?? 0);
            if (dupCount > 0)
            {
                MessageBox.Show("Is customer/plot ke liye plan already mojood hai.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cbcustomer.Focus(); return;
            }

            string qry;
            if (id == 0)
            {
                qry = @"
                    INSERT INTO installment_plans
                        (customer_plot_id, start_date, frequency_months, count_total, per_inst_amount, grace_days, created_at)
                    VALUES
                        (@cpid, @start_date, @freq, @count, @per_amt, @grace, @created_at)";
            }
            else
            {
                qry = @"
                    UPDATE installment_plans SET
                        customer_plot_id=@cpid,
                        start_date=@start_date,
                        frequency_months=@freq,
                        count_total=@count,
                        per_inst_amount=@per_amt,
                        grace_days=@grace
                    WHERE id=@id";
            }

            Hashtable ht = new Hashtable
            {
                { "@cpid", customerPlotId.Value },
                { "@start_date", startDate },
                { "@freq", freq },
                { "@count", count },
                { "@per_amt", perAmt },
                { "@grace", grace },
                { "@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
            if (id > 0) ht.Add("@id", id);

            try
            {
                if (MainClass.SQL(qry, ht) > 0)
                {
                    CustomeMessage msg = new CustomeMessage(id == 0 ? "Installment Plan saved." : "Installment Plan updated.", "RealEstate Builders");
                    msg.ShowDialog();
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetForm()
        {
            id = 0;
            if (cbcustomer.Items.Count > 0) cbcustomer.SelectedIndex = 0;
            if (txtdate != null) txtdate.Value = DateTime.Today;
            numfreqencymonth.Value = 1;
            numcount.Value = 1;
            numperInstallation.Value = 1;
            numGraceDays.Value = 0;
            cbcustomer.Focus();
        }

        // --- Edit-mode loader -------------------------------------------------
        public void LoadForEdit(int editId)
        {
            id = editId;
            string q = @"SELECT id, customer_plot_id, start_date, frequency_months, count_total, per_inst_amount, grace_days
                         FROM installment_plans WHERE id=@id";
            Hashtable ht = new Hashtable { { "@id", id } };
            DataTable dt = MainClass.ExecuteSelectQuery(q, ht);
            if (dt.Rows.Count != 1) return;

            var r = dt.Rows[0];

            // Rebind plots
            LoadPlotsChoices();

            // Map customer_plot_id -> plots.id for preselect
            int cpid = Convert.ToInt32(r["customer_plot_id"]);
            int? plotId = GetPlotIdForCustomerPlot(cpid);
            if (plotId != null)
            {
                try { cbcustomer.SelectedValue = plotId.Value; } catch { /* ignore */ }
            }

            DateTime d;
            if (DateTime.TryParse(Convert.ToString(r["start_date"]), out d)) txtdate.Value = d;
            else txtdate.Value = DateTime.Today;

            numfreqencymonth.Value = Math.Max(1, Convert.ToDecimal(r["frequency_months"]));
            numcount.Value = Math.Max(1, Convert.ToDecimal(r["count_total"]));
            numperInstallation.Value = Math.Max(1, Convert.ToDecimal(r["per_inst_amount"]));
            numGraceDays.Value = Math.Max(0, Convert.ToDecimal(r["grace_days"]));
        }
    }
}
