using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmDuesperPlot : Sample
    {
        public frmDuesperPlot()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmDuesperPlot_Load(object sender, EventArgs e)
        {
            LoadAllRecords(); // date/filter OFF
        }

        // A) LOAD: filters NULL (CustomerId/CustomerPlotId dono NULL)
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @CustomerId, @CustomerPlotId
WITH bill AS (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                  THEN debit ELSE 0 END) AS billed_debit
  FROM ledger
  GROUP BY customer_plot_id
),
rc AS (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts
  GROUP BY customer_plot_id
)
SELECT 
  cp.id          AS customer_plot_id,
  cp.customer_id,
  cp.plot_id,
  cp.plan_id,
  cp.booking_date,
  cp.price,
  cp.status,
  c.name         AS customer_name,
  COALESCE(bill.billed_debit,0)  AS billed_debit,
  COALESCE(rc.total_receipts,0)  AS collected_cash,
  CASE 
    WHEN COALESCE(bill.billed_debit,0) > 0
         THEN ROUND(COALESCE(bill.billed_debit,0) - COALESCE(rc.total_receipts,0), 2)
    ELSE ROUND(COALESCE(cp.price,0) - COALESCE(rc.total_receipts,0), 2)
  END AS due_amount
FROM customer_plots cp
JOIN customers c    ON c.id = cp.customer_id
LEFT JOIN bill      ON bill.customer_plot_id = cp.id
LEFT JOIN rc        ON rc.customer_plot_id   = cp.id
WHERE (@CustomerId     IS NULL OR cp.customer_id = @CustomerId)
  AND (@CustomerPlotId IS NULL OR cp.id          = @CustomerPlotId)
ORDER BY cp.booking_date, cp.id;";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerPlotId", DBNull.Value);

                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No records found!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                            string reportPath = Path.Combine(Application.StartupPath, "Reports", "DuesPerPlotReports.rdlc");

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

        // B) GETDATA: filters ON (agar UI se values do; warna NULL hi bhej do)
        public void GetData(int? customerId, int? customerPlotId)
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    string qry = @"
-- @CustomerId, @CustomerPlotId
WITH bill AS (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                  THEN debit ELSE 0 END) AS billed_debit
  FROM ledger
  GROUP BY customer_plot_id
),
rc AS (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts
  GROUP BY customer_plot_id
)
SELECT 
  cp.id          AS customer_plot_id,
  cp.customer_id,
  cp.plot_id,
  cp.plan_id,
  cp.booking_date,
  cp.price,
  cp.status,
  c.name         AS customer_name,
  COALESCE(bill.billed_debit,0)  AS billed_debit,
  COALESCE(rc.total_receipts,0)  AS collected_cash,
  CASE 
    WHEN COALESCE(bill.billed_debit,0) > 0
         THEN ROUND(COALESCE(bill.billed_debit,0) - COALESCE(rc.total_receipts,0), 2)
    ELSE ROUND(COALESCE(cp.price,0) - COALESCE(rc.total_receipts,0), 2)
  END AS due_amount
FROM customer_plots cp
JOIN customers c    ON c.id = cp.customer_id
LEFT JOIN bill      ON bill.customer_plot_id = cp.id
LEFT JOIN rc        ON rc.customer_plot_id   = cp.id
WHERE (@CustomerId     IS NULL OR cp.customer_id = @CustomerId)
  AND (@CustomerPlotId IS NULL OR cp.id          = @CustomerPlotId)
ORDER BY cp.booking_date, cp.id;";

                    using (SQLiteCommand cmd = new SQLiteCommand(qry, con))
                    {
                        if (customerId.HasValue)
                            cmd.Parameters.AddWithValue("@CustomerId", customerId.Value);
                        else
                            cmd.Parameters.AddWithValue("@CustomerId", DBNull.Value);

                        if (customerPlotId.HasValue)
                            cmd.Parameters.AddWithValue("@CustomerPlotId", customerPlotId.Value);
                        else
                            cmd.Parameters.AddWithValue("@CustomerPlotId", DBNull.Value);

                        using (SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            con.Open();
                            sd.Fill(dt);

                            ReportDataSource rds = new ReportDataSource("DataSet1", dt);

                            string reportPath = Path.Combine(Application.StartupPath, "Reports", "DuesPerPlotReports.rdlc");
                            if (!File.Exists(reportPath))
                            {
                                MessageBox.Show("Report file not found:\n" + reportPath, "Report",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }
    }
}
