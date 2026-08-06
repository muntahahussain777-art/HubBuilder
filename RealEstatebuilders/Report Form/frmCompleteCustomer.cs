using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmCompleteCustomer : Sample
    {
        public frmCompleteCustomer()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
        }

        private void frmCompleteCustomer_Load(object sender, EventArgs e)
        {
            // Default aaj–aaj
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;

            try { this.reportViewer1.RefreshReport(); } catch { }

            LoadAllRecords();
        }

        // === NEW: Search button exactly like other forms ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();  // From–To + keyword filter
        }

        // === NEW: Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            LoadAllRecords(); // date filter OFF, keyword empty ⇒ full data
        }

        // === A) All records (date filter OFF), keyword optional (auto-off when empty) ===
        private void LoadAllRecords()
        {
            try
            {
                string cs = projectconnection.conReturn();

                using (var con = new SQLiteConnection(cs))
                {
                    con.Open();

                    string sql = @"
-- @Keyword, @KeywordLike (auto-off when empty)
SELECT 
  c.id                                 AS customer_id,
  c.name,
  c.father_name,
  c.mobile,
  c.cnic,
  c.notes,
  c.created_at,
  COUNT(cp.id)                          AS plots_count,
  SUM(COALESCE(r.total_receipts,0))     AS total_received,
  SUM(
    CASE 
      WHEN COALESCE(l_bill.billed_debit,0) > 0 
        THEN COALESCE(l_bill.billed_debit,0) - COALESCE(r.total_receipts,0)
      ELSE COALESCE(cp.price,0) - COALESCE(r.total_receipts,0)
    END
  )                                     AS total_due,
  GROUP_CONCAT(p.plot_no, ', ')         AS plots
FROM customers c
LEFT JOIN customer_plots cp ON cp.customer_id=c.id
LEFT JOIN plots p           ON p.id=cp.plot_id
LEFT JOIN plot_types pt     ON pt.id=p.plot_type_id
LEFT JOIN (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                  THEN debit ELSE 0 END) AS billed_debit
  FROM ledger
  GROUP BY customer_plot_id
) l_bill ON l_bill.customer_plot_id=cp.id
LEFT JOIN (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts
  GROUP BY customer_plot_id
) r ON r.customer_plot_id=cp.id
WHERE
  (
    @Keyword = '' OR
    c.name    LIKE @KeywordLike COLLATE NOCASE OR
    pt.code   LIKE @KeywordLike COLLATE NOCASE OR
    p.plot_no LIKE @KeywordLike COLLATE NOCASE
  )
GROUP BY c.id
ORDER BY c.name;";

                    string keyword = (txtSearch.Text ?? "").Trim();

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", keyword);
                        cmd.Parameters.AddWithValue("@KeywordLike", $"%{keyword}%");

                        var da = new SQLiteDataAdapter(cmd);
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
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Live keyword search ko OFF rakha — agar chahiye to is line ko ON kar do:
        // private void txtSearch_TextChanged(object sender, EventArgs e) => GetData();

        // === B) From–To (inclusive) + Keyword (sirf: name / plot type code / plot no) ===
        public void GetData()
        {
            try
            {
                string fromStr = fromdate.Value.Date.ToString("yyyy-MM-dd");
                string toStr = todate.Value.Date.ToString("yyyy-MM-dd");
                string keyword = (txtSearch.Text ?? "").Trim();

                string sql = @"
-- Date on customer created_at (inclusive ToDate), keyword limited to 3 fields
SELECT 
  c.id                                 AS customer_id,
  c.name,
  c.father_name,
  c.mobile,
  c.cnic,
  c.notes,
  c.created_at,
  COUNT(cp.id)                           AS plots_count,
  SUM(COALESCE(l_bill.billed_debit,0))   AS total_billed,
  SUM(COALESCE(l_disc.discounts,0))      AS total_discounts,
  SUM(COALESCE(r.total_receipts,0))      AS total_received,
  SUM(
    CASE 
      WHEN COALESCE(l_bill.billed_debit,0) > 0 
        THEN COALESCE(l_bill.billed_debit,0) - COALESCE(r.total_receipts,0)
      ELSE COALESCE(cp.price,0) - COALESCE(r.total_receipts,0)
    END
  )                                       AS total_due,
  GROUP_CONCAT(p.plot_no, ', ')           AS plots
FROM customers c
LEFT JOIN customer_plots cp ON cp.customer_id=c.id
LEFT JOIN plots p           ON p.id=cp.plot_id
LEFT JOIN plot_types pt     ON pt.id=p.plot_type_id
LEFT JOIN (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge') THEN debit ELSE 0 END) AS billed_debit
  FROM ledger GROUP BY customer_plot_id
) l_bill ON l_bill.customer_plot_id=cp.id
LEFT JOIN (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type='Discount' THEN credit ELSE 0 END) AS discounts
  FROM ledger GROUP BY customer_plot_id
) l_disc ON l_disc.customer_plot_id=cp.id
LEFT JOIN (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts GROUP BY customer_plot_id
) r ON r.customer_plot_id=cp.id
WHERE 
  DATE(c.created_at) >= DATE(@FromDate)
  AND DATE(c.created_at)  < DATE(@ToDate, '+1 day')
  AND (
    @Keyword = '' OR
    c.name    LIKE @KeywordLike COLLATE NOCASE OR
    pt.code   LIKE @KeywordLike COLLATE NOCASE OR
    p.plot_no LIKE @KeywordLike COLLATE NOCASE
  )
GROUP BY c.id
ORDER BY c.name;";

                string cs = projectconnection.conReturn();

                using (var con = new SQLiteConnection(cs))
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromStr);
                    cmd.Parameters.AddWithValue("@ToDate", toStr);
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
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindReport(DataTable dt)
        {
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "CustomerCompleteReports.rdlc");

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
