using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmSeprateCustomer : Sample
    {
        // Date filters enable/disable flags
        private bool _useFrom = false;
        private bool _useTo = false;

        public frmSeprateCustomer()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);

            this.Load += frmSeprateCustomer_Load;

            // Live keyword search (RefNo / CustomerName / PlotTypeOrSize / Society / PlotNo)
            txtSearch.TextChanged += (s, e) => GetData();

            // Date pickers events
            fromdate.ValueChanged += (s, e) => { _useFrom = true; GetData(); };
            todate.ValueChanged += (s, e) => { _useTo = true; GetData(); };

            // Double-click to clear (NULL) a date filter
            fromdate.MouseDoubleClick += (s, e) => { _useFrom = false; GetData(); };
            todate.MouseDoubleClick += (s, e) => { _useTo = false; GetData(); };
        }

        private void frmSeprateCustomer_Load(object sender, EventArgs e)
        {
            // Default: Aaj–Aaj + both filters ON (upar wali form ke mutabiq)
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            _useFrom = true;
            _useTo = true;

            try { this.reportViewer1.RefreshReport(); } catch { }

            GetData();
        }

        // OPTIONAL: Button handlers (agar UI me hon)
        private void btnSearch_Click(object sender, EventArgs e) => GetData();

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            fromdate.Value = DateTime.Today;
            todate.Value = DateTime.Today;
            _useFrom = true;
            _useTo = true;
            GetData();
        }

        public void GetData()
        {
            try
            {
                // Date params (nullable by flags), ISO date string only
                object fromParam = _useFrom ? (object)fromdate.Value.Date.ToString("yyyy-MM-dd") : DBNull.Value;
                object toParam = _useTo ? (object)todate.Value.Date.ToString("yyyy-MM-dd") : DBNull.Value;

                // Swap if reversed
                if (fromParam is string fs && toParam is string ts &&
                    DateTime.Parse(fs) > DateTime.Parse(ts))
                {
                    var tmp = fromParam; fromParam = toParam; toParam = tmp;
                }

                // Keyword (RefNo / CustomerName / PlotTypeOrSize / Society / PlotNo)
                string kw = (txtSearch.Text ?? "").Trim();
                string kwLike = $"%{kw}%";
                bool hasKw = kw.Length > 0;

                // ====== MAIN RECEIPTS REPORT QUERY (date inclusive) ======
                string sql = @"
SELECT
    r.id                    AS ReceiptID,
    r.receipt_date          AS ReceiptDate,
    r.amount                AS Amount,
    r.mode                  AS Mode,
    r.ref_no                AS RefNo,
    c.name                  AS CustomerName,
    c.father_name           AS FatherName,
    s.name                  AS Society,
    b.name                  AS Block,
    pt.code                 AS PlotTypeOrSize,
    p.plot_no               AS PlotNo,
    cp.booking_date         AS BookingDate,
    cp.price                AS PlotPrice
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN customers c       ON c.id = cp.customer_id
JOIN plots p           ON p.id = cp.plot_id
JOIN blocks b          ON b.id = p.block_id
JOIN plot_types pt     ON pt.id = p.plot_type_id
JOIN societies s       ON s.id = p.society_id
WHERE
    -- Date range (inclusive); flags _useFrom/_useTo control NULLs in @from/@to
    (@from IS NULL OR DATE(r.receipt_date) >= DATE(@from))
    AND (@to IS NULL OR DATE(r.receipt_date) < DATE(@to, '+1 day'))
    -- Live keyword filter (RefNo / CustomerName / PlotTypeOrSize / Society / PlotNo)
    AND (
        @kw = ''
        OR r.ref_no   LIKE @kwLike COLLATE NOCASE
        OR c.name     LIKE @kwLike COLLATE NOCASE
        OR pt.code    LIKE @kwLike COLLATE NOCASE
        OR s.name     LIKE @kwLike COLLATE NOCASE
        OR p.plot_no  LIKE @kwLike COLLATE NOCASE
    )
ORDER BY r.receipt_date DESC, r.id DESC;";

                string cs = projectconnection.conReturn();
                using (var con = new SQLiteConnection(cs))
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@from", fromParam);
                    cmd.Parameters.AddWithValue("@to", toParam);
                    cmd.Parameters.AddWithValue("@kw", hasKw ? kw : "");
                    cmd.Parameters.AddWithValue("@kwLike", hasKw ? kwLike : "%");

                    var da = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    BindToReport(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindToReport(DataTable dt)
        {
            // RDLC bind
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "SeprateReportCustomer.rdlc"); // <-- apna file name yahan

            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found: " + reportPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            reportViewer1.LocalReport.ReportPath = reportPath;
            ReportHeaderSocietyHelper.Apply(reportViewer1.LocalReport, this);
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(rds);

            // OPTIONAL: pass report parameters (From/To/Keyword) agar RDLC header me dikhana ho
            // var ps = new[]
            // {
            //     new ReportParameter("FromDateLabel", _useFrom ? fromdate.Value.ToString("yyyy-MM-dd") : "All"),
            //     new ReportParameter("ToDateLabel",   _useTo   ? todate.Value.ToString("yyyy-MM-dd")   : "All"),
            //     new ReportParameter("KeywordLabel",  string.IsNullOrWhiteSpace(txtSearch.Text) ? "All" : txtSearch.Text.Trim())
            // };
            // reportViewer1.LocalReport.SetParameters(ps);

            reportViewer1.RefreshReport();
        }
    }
}
