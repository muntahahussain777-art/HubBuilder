using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmCustomerPlotBookingAllocationlist : Sample
    {
        public frmCustomerPlotBookingAllocationlist()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmCustomerPlotBookingAllocationlist_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData(); // from–to + keyword filter
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            LoadAllRecords();
        }

        // ===== A) LOAD: All records (no date / no search) =====
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
SELECT 
  cp.id AS customer_plot_id, cp.customer_id, cp.plot_id, cp.plan_id,
  cp.booking_date, cp.price, cp.status, cp.notes,
  s.name AS society_name, b.name AS block_name, 
  pt.code AS plot_type_code, p.plot_no,
  c.name AS customer_name
FROM customer_plots cp
JOIN customers  c ON c.id = cp.customer_id
JOIN plots      p ON p.id = cp.plot_id
JOIN blocks     b ON b.id = p.block_id
JOIN societies  s ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE
  (
    @Keyword = '' OR
    c.name    LIKE @KeywordLike COLLATE NOCASE OR
    pt.code   LIKE @KeywordLike COLLATE NOCASE OR
    p.plot_no LIKE @KeywordLike COLLATE NOCASE
  )
ORDER BY cp.booking_date, cp.id;";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        string keyword = (txtSearch.Text ?? "").Trim();
                        cmd.Parameters.AddWithValue("@Keyword", keyword);
                        cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
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
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===== B) GETDATA: From–To date + keyword filter =====
        public void GetData()
        {
            try
            {
                string fromStr = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string toStr = todate.Value.Date.ToString("yyyy-MM-dd");
                string keyword = (txtSearch.Text ?? "").Trim();

                string qry = @"
SELECT 
  cp.id AS customer_plot_id, cp.customer_id, cp.plot_id, cp.plan_id,
  cp.booking_date, cp.price, cp.status, cp.notes,
  s.name AS society_name, b.name AS block_name, 
  pt.code AS plot_type_code, p.plot_no,
  c.name AS customer_name
FROM customer_plots cp
JOIN customers  c ON c.id = cp.customer_id
JOIN plots      p ON p.id = cp.plot_id
JOIN blocks     b ON b.id = p.block_id
JOIN societies  s ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE
  date(cp.booking_date) >= date(@FromDate)
  AND date(cp.booking_date)  < date(@ToDate, '+1 day')
  AND (
    @Keyword = '' OR
    c.name    LIKE @KeywordLike COLLATE NOCASE OR
    pt.code   LIKE @KeywordLike COLLATE NOCASE OR
    p.plot_no LIKE @KeywordLike COLLATE NOCASE
  )
ORDER BY cp.booking_date, cp.id;";

                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                using (var cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromStr);
                    cmd.Parameters.AddWithValue("@ToDate", toStr);
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                    using (var sd = new SQLiteDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        sd.Fill(dt);

                        if (dt.Rows.Count == 0)
                        {
                            MessageBox.Show("Is date range ya keyword par koi record nahi mila.",
                                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        BindReport(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // === Common Report Binding ===
        private void BindReport(DataTable dt)
        {
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerBookingPlotAllocationListReport.rdlc");

            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found: " + reportPath, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(rds);
            reportViewer1.RefreshReport();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Live search enable karna ho to uncomment:
            // GetData();
        }
    }
}
