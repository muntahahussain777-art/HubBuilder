using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmAging : Sample
    {
        public frmAging()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
            this.Load += frmAging_Load;
        }

        private void frmAging_Load(object sender, EventArgs e)
        {
            // Default: AsOf = today
            asOfDate.Value = DateTime.Today;     // <- DateTimePicker (rename agar aapka naam different hai)
            try { this.reportViewer1.RefreshReport(); } catch { }

            GetData();                           // Initial load
        }

        // === Search button (same pattern) ===
        private void btnSearch_Click(object sender, EventArgs e)
        {
            GetData();
        }

        // === Clear button (optional) ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            asOfDate.Value = DateTime.Today;
            GetData();
        }

        // (OPTIONAL) Live search — chahein to enable karein
        // private void txtSearch_TextChanged(object sender, EventArgs e) => GetData();

        // ===== Core: AsOf (single date) + keyword on (Customer Name / Plot No / Plot Type Code) =====
        public void GetData()
        {
            try
            {
                string asOfStr = asOfDate.Value.Date.ToString("yyyy-MM-dd");
                string keyword = (txtSearch.Text ?? "").Trim();
                string keywordLike = $"%{keyword}%";

                string agingSql = @"
-- Params: @AsOfDate (YYYY-MM-DD), @Keyword, @KeywordLike
WITH deb AS (
  SELECT 
    l.customer_plot_id,
    MIN(l.txn_date) AS oldest_unpaid_date,
    SUM(CASE WHEN l.txn_type IN ('Booking','InstallmentDue','Surcharge')
             THEN l.debit ELSE 0 END) AS billed
  FROM ledger l
  WHERE date(l.txn_date) <= date(@AsOfDate)
  GROUP BY l.customer_plot_id
),
rc AS (
  SELECT r.customer_plot_id,
         SUM(r.amount) AS paid
  FROM receipts r
  WHERE date(r.receipt_date) <= date(@AsOfDate)
  GROUP BY r.customer_plot_id
),
q AS (
  SELECT 
    cp.id  AS customer_plot_id,
    cp.customer_id,
    cp.plot_id,
    COALESCE(deb.billed,0) AS billed,
    COALESCE(rc.paid,0)    AS paid,
    CASE 
      WHEN COALESCE(deb.billed,0) > 0
        THEN COALESCE(deb.billed,0) - COALESCE(rc.paid,0)
      ELSE COALESCE(cp.price,0)     - COALESCE(rc.paid,0)
    END AS due_amount,
    COALESCE(deb.oldest_unpaid_date, cp.booking_date) AS oldest_unpaid_date
  FROM customer_plots cp
  LEFT JOIN deb ON deb.customer_plot_id = cp.id
  LEFT JOIN rc  ON rc.customer_plot_id  = cp.id
),
base AS (
  SELECT
    q.customer_plot_id,
    q.customer_id,
    CASE 
      WHEN p.plot_no IS NOT NULL AND p.plot_no <> '' 
        THEN c.name || '/' || p.plot_no
      ELSE c.name
    END AS customer_name,            -- Name/PlotNo
    q.plot_id,
    p.plot_no,
    pt.code       AS plot_type_code, -- add for keyword search
    q.billed,
    q.paid,
    q.due_amount,
    q.oldest_unpaid_date,
    CASE
      WHEN q.due_amount <= 0 THEN 'Current'
      WHEN (julianday(@AsOfDate) - julianday(q.oldest_unpaid_date)) <= 30 THEN '0-30'
      WHEN (julianday(@AsOfDate) - julianday(q.oldest_unpaid_date)) <= 60 THEN '31-60'
      WHEN (julianday(@AsOfDate) - julianday(q.oldest_unpaid_date)) <= 90 THEN '61-90'
      ELSE '>90'
    END AS aging_bucket
  FROM q
  LEFT JOIN customers  c  ON c.id = q.customer_id
  LEFT JOIN plots      p  ON p.id = q.plot_id
  LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
)
SELECT *
FROM base
WHERE
  -- keyword auto-off when empty (NOCASE)
  (@Keyword = '' OR 
     customer_name LIKE @KeywordLike COLLATE NOCASE
  OR plot_no       LIKE @KeywordLike COLLATE NOCASE
  OR plot_type_code LIKE @KeywordLike COLLATE NOCASE)
ORDER BY aging_bucket DESC, due_amount DESC, customer_name;";

                string cs = projectconnection.conReturn();

                using (var con = new SQLiteConnection(cs))
                using (var cmd = new SQLiteCommand(agingSql, con))
                {
                    con.Open();

                    cmd.Parameters.AddWithValue("@AsOfDate", asOfStr);
                    cmd.Parameters.AddWithValue("@Keyword", keyword);
                    cmd.Parameters.AddWithValue("@KeywordLike", keywordLike);

                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);

                        string reportPath = Path.Combine(Application.StartupPath, "Reports", "AgingReports.rdlc");
                        if (!File.Exists(reportPath))
                        {
                            MessageBox.Show("Report file not found: " + reportPath, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var rds = new ReportDataSource("DataSet1", dt);
                        reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(rds);

                        // RDLC parameter (optional)
                        try
                        {
                            reportViewer1.LocalReport.SetParameters(new[]
                            {
                                new ReportParameter("AsOfDate", asOfStr)
                            });
                        }
                        catch { /* RDLC me param optional ho to ignore */ }

                        reportViewer1.RefreshReport();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
