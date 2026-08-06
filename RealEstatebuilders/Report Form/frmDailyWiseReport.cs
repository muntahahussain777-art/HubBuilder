using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmDailyWiseReport : Sample
    {
        public frmDailyWiseReport()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmDailyWiseReport_Load(object sender, EventArgs e)
        {
            // Default aaj–aaj (inclusive logic GetData me lagay ga)
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;

            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords(); // first load with NULL dates
        }

        // === NEW: Search button (same pattern) ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData(); // From–To + keyword (customer name)
        }

        // === NEW: Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            LoadAllRecords(); // date OFF, keyword empty ⇒ all
        }

        // ===== A) LOAD: all records (no date filter), keyword optional =====
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @Keyword, @KeywordLike (auto-off when empty)
SELECT 
  DATE(r.receipt_date)                                  AS receipt_date,   -- daily bucket
  c.name                                                AS customer_name,  -- customer ka naam
  SUM(r.amount)                                         AS total_amount,
  SUM(CASE WHEN r.mode='Cash' THEN r.amount ELSE 0 END) AS cash_amount,
  SUM(CASE WHEN r.mode='Bank' THEN r.amount ELSE 0 END) AS bank_amount,
  COUNT(*)                                              AS receipt_count
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c       ON c.id = cp.customer_id
WHERE
  (@Keyword = '' OR c.name LIKE @KeywordLike COLLATE NOCASE)
GROUP BY DATE(r.receipt_date), c.name
ORDER BY DATE(r.receipt_date), c.name;";

                    string keyword = (txtSearch.Text ?? "").Trim();

                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", keyword);
                        cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No records found!", "Info",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            BindReport(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                    "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===== B) GETDATA: From–To (inclusive) + customer name keyword =====
        public void GetData()
        {
            try
            {
                string fromStr = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string toStr = todate.Value.Date.ToString("yyyy-MM-dd");
                string keyword = (txtSearch.Text ?? "").Trim();

                string qry = @"
-- @DateFrom, @DateTo, @Keyword, @KeywordLike
SELECT 
  DATE(r.receipt_date)                                  AS receipt_date,   -- daily bucket
  c.name                                                AS customer_name,  -- customer ka naam
  SUM(r.amount)                                         AS total_amount,
  SUM(CASE WHEN r.mode='Cash' THEN r.amount ELSE 0 END) AS cash_amount,
  SUM(CASE WHEN r.mode='Bank' THEN r.amount ELSE 0 END) AS bank_amount,
  COUNT(*)                                              AS receipt_count
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c       ON c.id = cp.customer_id
WHERE 
  DATE(r.receipt_date) >= DATE(@DateFrom)
  AND DATE(r.receipt_date)  < DATE(@DateTo, '+1 day')   -- inclusive ToDate
  AND (@Keyword = '' OR c.name LIKE @KeywordLike COLLATE NOCASE)
GROUP BY DATE(r.receipt_date), c.name
ORDER BY DATE(r.receipt_date), c.name;";

                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@DateFrom", fromStr);
                    cmd.Parameters.AddWithValue("@DateTo", toStr);
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                    SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    sd.Fill(dt);

                    BindReport(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                    "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // (OPTIONAL) Live search: sirf customer name par
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Live chahiye to ON kar lo:
            // GetData();
        }

        // === Common binder ===
        private void BindReport(DataTable dt)
        {
            ReportDataSource rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "dailywiseReport.rdlc");

            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found!", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
