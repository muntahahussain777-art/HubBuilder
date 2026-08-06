using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmCustomerTotalsRollUp : Sample
    {
        public frmCustomerTotalsRollUp()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmCustomerTotalsRollUp_Load(object sender, EventArgs e)
        {
            LoadAllRecords();
        }

        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @CustomerId
WITH bal AS (
  WITH bill AS (
    SELECT customer_plot_id,
           SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge') THEN debit ELSE 0 END) AS billed_debit
    FROM ledger GROUP BY customer_plot_id
  ),
  rc AS (
    SELECT customer_plot_id, SUM(amount) AS total_receipts
    FROM receipts GROUP BY customer_plot_id
  )
  SELECT cp.customer_id,
         COALESCE(bill.billed_debit,0) AS billed_debit,
         COALESCE(rc.total_receipts,0) AS total_receipts,
         CASE WHEN COALESCE(bill.billed_debit,0)>0
              THEN COALESCE(bill.billed_debit,0)-COALESCE(rc.total_receipts,0)
              ELSE COALESCE(cp.price,0)-COALESCE(rc.total_receipts,0)
         END AS due_amount
  FROM customer_plots cp
  LEFT JOIN bill ON bill.customer_plot_id=cp.id
  LEFT JOIN rc   ON rc.customer_plot_id=cp.id
)
SELECT c.id AS customer_id, c.name, c.mobile, c.cnic,
       SUM(bal.billed_debit)    AS billed_total,
       SUM(bal.total_receipts)  AS receipts_total,
       SUM(bal.due_amount)      AS due_total
FROM customers c
LEFT JOIN bal ON bal.customer_id=c.id
WHERE (@CustomerId IS NULL OR c.id=@CustomerId)
GROUP BY c.id;
";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        // NULL => all customers. Bind a specific ID when needed.
                        cmd.Parameters.AddWithValue("@CustomerId", DBNull.Value);

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No records found!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            var rds = new ReportDataSource("DataSet1", dt);
                            string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerTotalsRollUpReport.rdlc");

                            if (!File.Exists(reportPath))
                            {
                                MessageBox.Show("Report file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
                            reportViewer1.LocalReport.DataSources.Clear();
                            reportViewer1.LocalReport.DataSources.Add(rds);
                            reportViewer1.RefreshReport();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
