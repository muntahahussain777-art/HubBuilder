using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmCosgsExpense_Receipts : Sample
    {
        public frmCosgsExpense_Receipts()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmCosgsExpense_Receipts_Load(object sender, EventArgs e)
        {
            // Aaj–Aaj default, upar wali requirement ke mutabiq
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords(); // load (without date/mode filters)
        }

        // ===== A) LOAD: Date/Mode filters OFF (all NULL) =====
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @DateFrom, @DateTo, @Mode  -- @Mode: NULL (all), 'Cash', 'Bank'
WITH rc AS (
  SELECT SUM(r.amount) AS cash_in
  FROM receipts r
  WHERE (@Mode     IS NULL OR r.mode = @Mode)
    AND (@DateFrom IS NULL OR DATE(r.receipt_date) >= DATE(@DateFrom))
    AND (@DateTo   IS NULL OR DATE(r.receipt_date)  < DATE(@DateTo, '+1 day'))
),
op AS (
  SELECT SUM(e.Amount) AS opex
  FROM Expensetbl e
  WHERE (@DateFrom IS NULL OR DATE(e.EDate) >= DATE(@DateFrom))
    AND (@DateTo   IS NULL OR DATE(e.EDate)  < DATE(@DateTo, '+1 day'))
)
SELECT 
  ROUND(COALESCE(rc.cash_in, 0), 2) AS cash_collected,
  ROUND(COALESCE(op.opex,    0), 2) AS operating_expense,
  ROUND(COALESCE(rc.cash_in, 0) - COALESCE(op.opex, 0), 2) AS net_cash_profit
FROM rc
CROSS JOIN op;";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        // All filters OFF
                        cmd.Parameters.AddWithValue("@DateFrom", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateTo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Mode", DBNull.Value);

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

        // (Agar chaho to kisi control change par GetData call kara lo)
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            GetData(); // date-to-date + optional mode
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();
        }

        // ===== B) GETDATA: Date/Mode filters ON (inclusive ToDate) =====
        public void GetData()
        {
            try
            {
                // Dates as ISO (TEXT), end inclusive via < ToDate+1day
                string fromStr = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string toStr = todate.Value.Date.ToString("yyyy-MM-dd");

                // Mode filter (optional): NULL = All, warna "Cash"/"Bank"
                object modeParam = DBNull.Value;
                // Example (agar ComboBox ho):
                // var mode = cbMode.SelectedItem?.ToString();
                // if (mode == "Cash" || mode == "Bank") modeParam = mode;

                string qry = @"
-- @DateFrom, @DateTo, @Mode  -- @Mode: NULL (all), 'Cash', 'Bank'
WITH rc AS (
  SELECT SUM(r.amount) AS cash_in
  FROM receipts r
  WHERE (@Mode     IS NULL OR r.mode = @Mode)
    AND DATE(r.receipt_date) >= DATE(@DateFrom)
    AND DATE(r.receipt_date)  < DATE(@DateTo, '+1 day')
),
op AS (
  SELECT SUM(e.Amount) AS opex
  FROM Expensetbl e
  WHERE DATE(e.EDate) >= DATE(@DateFrom)
    AND DATE(e.EDate)  < DATE(@DateTo, '+1 day')
)
SELECT 
  ROUND(COALESCE(rc.cash_in, 0), 2) AS cash_collected,
  ROUND(COALESCE(op.opex,    0), 2) AS operating_expense,
  ROUND(COALESCE(rc.cash_in, 0) - COALESCE(op.opex, 0), 2) AS net_cash_profit
FROM rc
CROSS JOIN op;";

                string connectionString = projectconnection.conReturn();

                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@DateFrom", fromStr);
                    cmd.Parameters.AddWithValue("@DateTo", toStr);
                    cmd.Parameters.AddWithValue("@Mode", modeParam);

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

        // === Common: Report bind ===
        private void BindReport(DataTable dt)
        {
            ReportDataSource rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "CogsExpense_Receipts.rdlc");

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
