using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmmonthlywise : Sample
    {
        public frmmonthlywise()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmmonthlywise_Load(object sender, EventArgs e)
        {
            // Default: current year show karo
            txtSearch.Text = DateTime.Today.Year.ToString();

            try { this.reportViewer1.RefreshReport(); } catch { }

            GetData();          // current year
            // Agar first load pe ALL chahiye ho to upar ki line hata ke LoadAllRecords() call kar do.
            // LoadAllRecords();
        }

        // === NEW: Search button (same pattern) ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();
        }

        // === NEW: Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            LoadAllRecords(); // sab saal
        }

        // ===== A) All records (no YEAR filter) =====
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @Year (e.g. '2025' ya NULL)
SELECT 
  strftime('%Y-%m', r.receipt_date) AS yyyymm,
  SUM(r.amount) AS total_amount
FROM receipts r
WHERE (@Year IS NULL OR strftime('%Y', r.receipt_date) = @Year)
GROUP BY strftime('%Y-%m', r.receipt_date)
ORDER BY yyyymm;";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        // ALL years
                        cmd.Parameters.AddWithValue("@Year", DBNull.Value);

                        using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
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

        // OPTIONAL: Live search (year type karte hi refresh)
        // private void txtSearch_TextChanged(object sender, EventArgs e) => GetData();

        // ===== B) YEAR filtered =====
        public void GetData()
        {
            try
            {
                // Year ko txtSearch se parse karein (e.g., "2025"); empty => ALL
                object yearParam = DBNull.Value;
                string y = (txtSearch.Text ?? "").Trim();
                if (y.Length == 4 && int.TryParse(y, out int yInt) && yInt >= 1900 && yInt <= 3000)
                    yearParam = y; // TEXT param for strftime('%Y', ...)

                string qry = @"
-- @Year (e.g. '2025' ya NULL)
SELECT 
  strftime('%Y-%m', r.receipt_date) AS yyyymm,
  SUM(r.amount) AS total_amount
FROM receipts r
WHERE (@Year IS NULL OR strftime('%Y', r.receipt_date) = @Year)
GROUP BY strftime('%Y-%m', r.receipt_date)
ORDER BY yyyymm;";

                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@Year", yearParam);

                    SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
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
            ReportDataSource rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "monthlywiseReport.rdlc");

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
