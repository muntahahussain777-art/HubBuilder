using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmReciptDetails : Sample
    {
        public frmReciptDetails()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmReciptDetails_Load(object sender, EventArgs e)
        {
            // Default: aaj–aaj
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;

            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords();
        }

        // === NEW: Search button ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();
        }

        // === NEW: Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            LoadAllRecords();
        }

        // ===== A) Initial/all load: NO date filter; keyword optional (auto-off when empty) =====
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @Keyword, @KeywordLike
SELECT 
    r.*,
    c.name AS customer_name
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c       ON c.id = cp.customer_id
LEFT JOIN plots p           ON p.id = cp.plot_id
WHERE
  (@Keyword = '' OR
     c.name  LIKE @KeywordLike COLLATE NOCASE
  OR p.plot_no LIKE @KeywordLike COLLATE NOCASE)
ORDER BY r.receipt_date, r.id;";

                    string keyword = (txtSearch.Text ?? "").Trim();

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", keyword);
                        cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count == 0)
                            {
                                MessageBox.Show("No records found!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            BindReport(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // OPTIONAL: Live search chahiye to on kar lo
        // private void txtSearch_TextChanged(object sender, EventArgs e) => GetData();

        // ===== B) Filtered load: From/To (inclusive) + optional CustomerPlotId + optional Mode + keyword =====
        public void GetData()
        {
            try
            {
                string dateFrom = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string dateTo = todate.Value.Date.ToString("yyyy-MM-dd");

                // Try numeric CustomerPlotId
                object customerPlotParam = DBNull.Value;
                if (!string.IsNullOrWhiteSpace(txtSearch.Text) && long.TryParse(txtSearch.Text.Trim(), out long plotId))
                    customerPlotParam = plotId;

                // Keyword for name/plot_no
                string keyword = (txtSearch.Text ?? "").Trim();

                // Optional Mode (Cash/Bank) — plug your control if any
                object modeParam = DBNull.Value;
                // Example:
                // var m = cbMode.SelectedItem?.ToString();
                // if (m == "Cash" || m == "Bank") modeParam = m;

                string qry = @"
-- @CustomerPlotId, @DateFrom, @DateTo, @Mode, @Keyword, @KeywordLike
SELECT 
    r.*,
    c.name AS customer_name
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c       ON c.id = cp.customer_id
LEFT JOIN plots p           ON p.id = cp.plot_id
WHERE
  (@CustomerPlotId IS NULL OR r.customer_plot_id = @CustomerPlotId)
  AND DATE(r.receipt_date) >= DATE(@DateFrom)
  AND DATE(r.receipt_date)  < DATE(@DateTo, '+1 day')     -- inclusive ToDate
  AND (@Mode IS NULL OR r.mode = @Mode)
  AND (
       @Keyword = '' OR
       c.name    LIKE @KeywordLike COLLATE NOCASE OR
       p.plot_no LIKE @KeywordLike COLLATE NOCASE
  )
ORDER BY r.receipt_date, r.id;";

                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                using (var cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@CustomerPlotId", customerPlotParam);
                    cmd.Parameters.AddWithValue("@DateFrom", dateFrom);
                    cmd.Parameters.AddWithValue("@DateTo", dateTo);
                    cmd.Parameters.AddWithValue("@Mode", modeParam);
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                    var sd = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    sd.Fill(dt);

                    BindReport(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // === Common binder ===
        private void BindReport(DataTable dt)
        {
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "ReceiptsDetailsReports.rdlc");

            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found! (" + reportPath + ")", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
