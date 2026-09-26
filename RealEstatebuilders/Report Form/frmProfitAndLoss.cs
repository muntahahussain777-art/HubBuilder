using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmProfitAndLoss : Sample
    {
        public frmProfitAndLoss()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmProfitAndLoss_Load(object sender, EventArgs e)
        {
            // Default: aaj–aaj
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;

            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords(); // No filters initially
        }

        // === NEW: Search button ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();
        }

        // === NEW: Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            // Mode ko bhi "All" pe reset karna ho to yahan set kar lo
            // if (cbMode != null) cbMode.SelectedIndex = 0;

            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            LoadAllRecords();
        }

        // A) First load (filters OFF)
        private void LoadAllRecords()
        {
            try
            {
                string connectionString = projectconnection.conReturn();
                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();

                    string query = @"
-- @DateFrom, @DateTo, @Mode   -- @Mode: NULL (all), 'Cash', 'Bank'
WITH ex AS (
  SELECT
    SUM(
      CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN COALESCE(Amount,0) ELSE 0 END
    ) AS cogs_sum,
    SUM(
      CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN 0 ELSE COALESCE(Amount,0) END
    ) AS opex_sum
  FROM Expensetbl e
  WHERE (@DateFrom IS NULL OR DATE(e.EDate) >= DATE(@DateFrom))
    AND (@DateTo   IS NULL OR DATE(e.EDate)  < DATE(@DateTo, '+1 day'))
),
rc AS (
  SELECT SUM(COALESCE(r.amount,0)) AS cash_in
  FROM receipts r
  WHERE (@Mode     IS NULL OR r.mode = @Mode)
    AND (@DateFrom IS NULL OR DATE(r.receipt_date) >= DATE(@DateFrom))
    AND (@DateTo   IS NULL OR DATE(r.receipt_date)  < DATE(@DateTo, '+1 day'))
),
bc AS (
  SELECT SUM(COALESCE(cp.broker_commission,0)) AS broker_sum
  FROM customer_plots cp
  WHERE IFNULL(cp.status,'Active') = 'Active'
    AND (@DateFrom IS NULL OR DATE(cp.booking_date) >= DATE(@DateFrom))
    AND (@DateTo   IS NULL OR DATE(cp.booking_date)  < DATE(@DateTo, '+1 day'))
)
SELECT
  ROUND(COALESCE(rc.cash_in,0), 2)                                        AS revenue_cash,
  ROUND(COALESCE(ex.cogs_sum,0), 2)                                       AS cogs_cash,
  ROUND(COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0), 2)              AS gross_profit_cash,
  ROUND(COALESCE(ex.opex_sum,0), 2)                                        AS operating_expense,
  ROUND(COALESCE(bc.broker_sum,0), 2)                                      AS broker_commission,
  ROUND((COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0))
        - COALESCE(ex.opex_sum,0) - COALESCE(bc.broker_sum,0), 2)         AS net_profit_cash
FROM ex CROSS JOIN rc CROSS JOIN bc;";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        // Filters OFF
                        cmd.Parameters.AddWithValue("@DateFrom", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateTo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Mode", DBNull.Value);

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

        // B) Filtered load (Date range + Mode) — ToDate inclusive
        public void GetData()
        {
            try
            {
                string fromStr = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string toStr = todate.Value.Date.ToString("yyyy-MM-dd");

                // Mode (optional): NULL = All, warna "Cash"/"Bank"
                object modeParam = DBNull.Value;
                // Example if you add a ComboBox named cbMode:
                // var m = cbMode.SelectedItem?.ToString();
                // if (m == "Cash" || m == "Bank") modeParam = m;

                string qry = @"
-- @DateFrom, @DateTo, @Mode   -- @Mode: NULL (all), 'Cash', 'Bank'
WITH ex AS (
  SELECT
    SUM(
      CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN COALESCE(Amount,0) ELSE 0 END
    ) AS cogs_sum,
    SUM(
      CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN 0 ELSE COALESCE(Amount,0) END
    ) AS opex_sum
  FROM Expensetbl e
  WHERE DATE(e.EDate) >= DATE(@DateFrom)
    AND DATE(e.EDate)  < DATE(@DateTo, '+1 day')
),
rc AS (
  SELECT SUM(COALESCE(r.amount,0)) AS cash_in
  FROM receipts r
  WHERE (@Mode IS NULL OR r.mode = @Mode)
    AND DATE(r.receipt_date) >= DATE(@DateFrom)
    AND DATE(r.receipt_date)  < DATE(@DateTo, '+1 day')
),
bc AS (
  SELECT SUM(COALESCE(cp.broker_commission,0)) AS broker_sum
  FROM customer_plots cp
  WHERE IFNULL(cp.status,'Active') = 'Active'
    AND DATE(cp.booking_date) >= DATE(@DateFrom)
    AND DATE(cp.booking_date)  < DATE(@DateTo, '+1 day')
)
SELECT
  ROUND(COALESCE(rc.cash_in,0), 2)                                        AS revenue_cash,
  ROUND(COALESCE(ex.cogs_sum,0), 2)                                       AS cogs_cash,
  ROUND(COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0), 2)              AS gross_profit_cash,
  ROUND(COALESCE(ex.opex_sum,0), 2)                                        AS operating_expense,
  ROUND(COALESCE(bc.broker_sum,0), 2)                                      AS broker_commission,
  ROUND((COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0))
        - COALESCE(ex.opex_sum,0) - COALESCE(bc.broker_sum,0), 2)         AS net_profit_cash
FROM ex CROSS JOIN rc CROSS JOIN bc;";

                string connectionString = projectconnection.conReturn();

                using (var con = new SQLiteConnection(connectionString))
                using (var cmd = new SQLiteCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@DateFrom", fromStr);
                    cmd.Parameters.AddWithValue("@DateTo", toStr);
                    cmd.Parameters.AddWithValue("@Mode", modeParam);

                    var sd = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    sd.Fill(dt);

                    BindReport(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // === Common binder ===
        private void BindReport(DataTable dt)
        {
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "Profit_Lost.rdlc");

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
