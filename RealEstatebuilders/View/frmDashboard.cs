using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace HubBuilders.View
{
    public partial class frmDashboard : Sample
    {
        public frmDashboard()
        {
            InitializeComponent();
        }

        private void frmDashboard_Load(object sender, EventArgs e)
        {
            try
            {
                InitChartAppearancePL();        // P&L chart styling
                InitChartAppearanceReceipts();  // Receipts chart styling
                LoadYears();                    // fill cbYear
                LoadChartForSelectedYear();     // render P&L
                LoadReceiptsForSelectedYear();  // render Receipts (Cash vs Bank)

                if (cbYear != null)
                    cbYear.SelectedIndexChanged += (s, ev) =>
                    {
                        LoadChartForSelectedYear();
                        LoadReceiptsForSelectedYear();
                    };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dashboard load error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------- SQL: Monthly P&L (Cash-basis) ----------
        private const string SqlMonthlyPL = @"
-- @Year (TEXT 'YYYY' or NULL)
WITH months(m) AS (
  VALUES('01'),('02'),('03'),('04'),('05'),('06'),
        ('07'),('08'),('09'),('10'),('11'),('12')
),
rc AS (
  SELECT strftime('%m', DATE(r.receipt_date)) AS m,
         SUM(COALESCE(r.amount,0))            AS revenue_cash
  FROM receipts r
  WHERE (@Year IS NULL OR strftime('%Y', DATE(r.receipt_date))=@Year)
  GROUP BY strftime('%m', DATE(r.receipt_date))
),
ex AS (
  SELECT strftime('%m', DATE(e.EDate)) AS m,
         SUM(CASE 
               WHEN LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'cogs%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'cost of sales%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'material%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'labor%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'wage%'
               THEN COALESCE(e.Amount,0) ELSE 0 END) AS cogs_cash,
         SUM(CASE 
               WHEN LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'cogs%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'cost of sales%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'material%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'labor%'
                 OR LOWER(TRIM(IFNULL(e.Category,''))) LIKE 'wage%'
               THEN 0 ELSE COALESCE(e.Amount,0) END) AS opex_cash
  FROM Expensetbl e
  WHERE (@Year IS NULL OR strftime('%Y', DATE(e.EDate))=@Year)
  GROUP BY strftime('%m', DATE(e.EDate))
)
SELECT 
  m.m                                                   AS month_num,
  COALESCE(rc.revenue_cash,0)                           AS revenue_cash,
  COALESCE(ex.cogs_cash,0)                              AS cogs_cash,
  COALESCE(ex.opex_cash,0)                              AS opex_cash,
  COALESCE(ex.cogs_cash,0) + COALESCE(ex.opex_cash,0)   AS expenses_total,
  ROUND(COALESCE(rc.revenue_cash,0) - COALESCE(ex.cogs_cash,0),2) AS gross_profit_cash,
  ROUND(COALESCE(rc.revenue_cash,0) - (COALESCE(ex.cogs_cash,0)+COALESCE(ex.opex_cash,0)),2) AS net_profit_cash
FROM months m
LEFT JOIN rc ON rc.m = m.m
LEFT JOIN ex ON ex.m = m.m
ORDER BY m.m;
";

        // ---------- SQL: Monthly receipts split (Cash vs Bank) ----------
        private const string SqlMonthlyReceiptsMix = @"
-- @Year (TEXT 'YYYY' or NULL)
WITH months(m) AS (
  VALUES('01'),('02'),('03'),('04'),('05'),('06'),
        ('07'),('08'),('09'),('10'),('11'),('12')
),
rc AS (
  SELECT strftime('%m', DATE(r.receipt_date)) AS m,
         SUM(CASE WHEN r.mode='Cash' THEN COALESCE(r.amount,0) ELSE 0 END) AS cash_in,
         SUM(CASE WHEN r.mode='Bank' THEN COALESCE(r.amount,0) ELSE 0 END) AS bank_in
  FROM receipts r
  WHERE (@Year IS NULL OR strftime('%Y', DATE(r.receipt_date))=@Year)
  GROUP BY strftime('%m', DATE(r.receipt_date))
)
SELECT 
  m.m                               AS month_num,
  COALESCE(rc.cash_in,0)            AS cash_in,
  COALESCE(rc.bank_in,0)            AS bank_in,
  COALESCE(rc.cash_in,0)+COALESCE(rc.bank_in,0) AS total_in
FROM months m
LEFT JOIN rc ON rc.m = m.m
ORDER BY m.m;
";

        // ---------- Load years into ComboBox ----------
        private void LoadYears()
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();

                var sqlYears = @"
SELECT y FROM (
  SELECT DISTINCT strftime('%Y', DATE(receipt_date)) AS y FROM receipts
  UNION
  SELECT DISTINCT strftime('%Y', DATE(EDate)) AS y FROM Expensetbl
) t
WHERE y IS NOT NULL
ORDER BY y DESC;";

                using (var da = new SQLiteDataAdapter(sqlYears, con))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cbYear.Items.Clear();
                    cbYear.Items.Add("All"); // NULL filter
                    foreach (DataRow r in dt.Rows)
                    {
                        cbYear.Items.Add(Convert.ToString(r["y"]));
                    }

                    var currentYear = DateTime.Today.Year.ToString();
                    int idx = cbYear.Items.IndexOf(currentYear);
                    cbYear.SelectedIndex = idx >= 0 ? idx : 0;
                }
            }
        }

        // ---------- P&L chart styling ----------
        private void InitChartAppearancePL()
        {
            chartPL.Series.Clear();
            chartPL.ChartAreas.Clear();
            chartPL.Legends.Clear();

            var area = new ChartArea("PL")
            {
                BackColor = Color.White
            };
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisX.Interval = 1;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.LabelStyle.Angle = -45;
            area.AxisY.LabelStyle.Format = "N0";
            chartPL.ChartAreas.Add(area);

            var legend = new Legend("Legend")
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center
            };
            chartPL.Legends.Add(legend);

            chartPL.Titles.Clear();
            chartPL.Titles.Add("Monthly Profit & Loss (Cash Basis)");
            chartPL.Titles[0].Font = new Font("Segoe UI", 11, FontStyle.Bold);
        }

        // ---------- Receipts (Cash vs Bank) chart styling ----------
        private void InitChartAppearanceReceipts()
        {
            chartReceipts.Series.Clear();
            chartReceipts.ChartAreas.Clear();
            chartReceipts.Legends.Clear();

            var area = new ChartArea("RC")
            {
                BackColor = Color.White
            };
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisX.Interval = 1;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.LabelStyle.Angle = -45;
            area.AxisY.LabelStyle.Format = "N0";
            chartReceipts.ChartAreas.Add(area);

            var legend = new Legend("Legend")
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center
            };
            chartReceipts.Legends.Add(legend);

            chartReceipts.Titles.Clear();
            chartReceipts.Titles.Add("Monthly Receipts Mix (Cash vs Bank)");
            chartReceipts.Titles[0].Font = new Font("Segoe UI", 11, FontStyle.Bold);
        }

        // ---------- Load P&L chart ----------
        private void LoadChartForSelectedYear()
        {
            string year = null;
            if (cbYear != null && cbYear.SelectedItem != null && cbYear.SelectedItem.ToString() != "All")
                year = cbYear.SelectedItem.ToString();

            var dt = GetMonthlyPL(year);

            chartPL.Series.Clear();

            var sRevenue = new Series("Revenue");
            sRevenue.ChartType = SeriesChartType.Column;
            sRevenue.ChartArea = "PL";
            sRevenue.IsValueShownAsLabel = true;
            sRevenue.LabelFormat = "N0";
            sRevenue.ToolTip = "#VALX\nRevenue: #VAL{N0}";
            chartPL.Series.Add(sRevenue);

            var sExpenses = new Series("Expenses");
            sExpenses.ChartType = SeriesChartType.Column;
            sExpenses.ChartArea = "PL";
            sExpenses.IsValueShownAsLabel = true;
            sExpenses.LabelFormat = "N0";
            sExpenses.ToolTip = "#VALX\nExpenses: #VAL{N0}";
            chartPL.Series.Add(sExpenses);

            var sNet = new Series("Net Profit");
            sNet.ChartType = SeriesChartType.Line;
            sNet.BorderWidth = 3;
            sNet.ChartArea = "PL";
            sNet.IsValueShownAsLabel = true;
            sNet.LabelFormat = "N0";
            sNet.MarkerStyle = MarkerStyle.Circle;
            sNet.MarkerSize = 7;
            sNet.ToolTip = "#VALX\nNet: #VAL{N0}";
            chartPL.Series.Add(sNet);

            string[] monthNames = Enumerable.Range(1, 12)
                .Select(i => CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i - 1])
                .ToArray();

            foreach (DataRow r in dt.Rows)
            {
                int m = int.Parse(Convert.ToString(r["month_num"]));
                string label = monthNames[m - 1];

                double revenue = Convert.ToDouble(r["revenue_cash"]);
                double expenses = Convert.ToDouble(r["expenses_total"]);
                double net = Convert.ToDouble(r["net_profit_cash"]);

                sRevenue.Points.AddXY(label, revenue);
                sExpenses.Points.AddXY(label, expenses);
                sNet.Points.AddXY(label, net);
            }

            chartPL.ChartAreas["PL"].AxisY.IsStartedFromZero = true;
            chartPL.Invalidate();
        }

        // ---------- Load Receipts chart (Cash vs Bank) ----------
        private void LoadReceiptsForSelectedYear()
        {
            string year = null;
            if (cbYear != null && cbYear.SelectedItem != null && cbYear.SelectedItem.ToString() != "All")
                year = cbYear.SelectedItem.ToString();

            var dt = GetMonthlyReceiptsMix(year);

            chartReceipts.Series.Clear();

            var sCash = new Series("Cash Receipts");
            sCash.ChartType = SeriesChartType.StackedColumn;
            sCash.ChartArea = "RC";
            sCash.IsValueShownAsLabel = true;
            sCash.LabelFormat = "N0";
            sCash.ToolTip = "#VALX\nCash: #VAL{N0}";
            chartReceipts.Series.Add(sCash);

            var sBank = new Series("Bank Receipts");
            sBank.ChartType = SeriesChartType.StackedColumn;
            sBank.ChartArea = "RC";
            sBank.IsValueShownAsLabel = true;
            sBank.LabelFormat = "N0";
            sBank.ToolTip = "#VALX\nBank: #VAL{N0}";
            chartReceipts.Series.Add(sBank);

            // Optional: total line on top
            var sTotal = new Series("Total");
            sTotal.ChartType = SeriesChartType.Line;
            sTotal.BorderWidth = 3;
            sTotal.ChartArea = "RC";
            sTotal.IsValueShownAsLabel = true;
            sTotal.LabelFormat = "N0";
            sTotal.MarkerStyle = MarkerStyle.Circle;
            sTotal.MarkerSize = 7;
            sTotal.ToolTip = "#VALX\nTotal: #VAL{N0}";
            chartReceipts.Series.Add(sTotal);

            string[] monthNames = Enumerable.Range(1, 12)
                .Select(i => CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i - 1])
                .ToArray();

            foreach (DataRow r in dt.Rows)
            {
                int m = int.Parse(Convert.ToString(r["month_num"]));
                string label = monthNames[m - 1];

                double cash = Convert.ToDouble(r["cash_in"]);
                double bank = Convert.ToDouble(r["bank_in"]);
                double total = Convert.ToDouble(r["total_in"]);

                sCash.Points.AddXY(label, cash);
                sBank.Points.AddXY(label, bank);
                sTotal.Points.AddXY(label, total);
            }

            chartReceipts.ChartAreas["RC"].AxisY.IsStartedFromZero = true;
            chartReceipts.Invalidate();
        }

        // ---------- Query helpers ----------
        private DataTable GetMonthlyPL(string yearOrNull)
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(SqlMonthlyPL, con))
            {
                if (string.IsNullOrWhiteSpace(yearOrNull))
                    cmd.Parameters.AddWithValue("@Year", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@Year", yearOrNull);

                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    con.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private DataTable GetMonthlyReceiptsMix(string yearOrNull)
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(SqlMonthlyReceiptsMix, con))
            {
                if (string.IsNullOrWhiteSpace(yearOrNull))
                    cmd.Parameters.AddWithValue("@Year", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@Year", yearOrNull);

                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    con.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
    }
}
