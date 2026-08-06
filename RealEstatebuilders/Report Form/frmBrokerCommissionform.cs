using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmBrokerCommissionform : Sample
    {
        // Date filters enable/disable flags
        private bool _useFrom = false;
        private bool _useTo = false;

        public frmBrokerCommissionform()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);

            this.Load += frmBrokerCommissionform_Load;

            // Live keyword search (sirf broker name / plot type code / plot no)
            txtSearch.TextChanged += (s, e) => GetData();

            // Date pickers events
            fromdate.ValueChanged += (s, e) => { _useFrom = true; GetData(); };
            todate.ValueChanged += (s, e) => { _useTo = true; GetData(); };

            // Double-click to clear (NULL) a date filter
            fromdate.MouseDoubleClick += (s, e) => { _useFrom = false; GetData(); };
            todate.MouseDoubleClick += (s, e) => { _useTo = false; GetData(); };
        }

        private void frmBrokerCommissionform_Load(object sender, EventArgs e)
        {
            // Default: Aaj–Aaj + both filters ON (upar wali forms jaisa)
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

                // Keyword (only on broker name / plot type code / plot no)
                string kw = (txtSearch.Text ?? "").Trim();
                string kwLike = $"%{kw}%";
                bool hasKw = kw.Length > 0;

                string sql = @"
SELECT
  b.name                               AS broker,
  pt.code                              AS plot_type,
  s.name                               AS society,
  bl.name                              AS block,
  p.plot_no,
  cp.price,
  IFNULL(cp.broker_commission, 0)      AS broker_commission,
  cp.booking_date
FROM customer_plots cp
JOIN brokers    b  ON b.id  = cp.broker_id
JOIN plots      p  ON p.id  = cp.plot_id
JOIN plot_types pt ON pt.id = p.plot_type_id
JOIN blocks     bl ON bl.id = p.block_id
JOIN societies  s  ON s.id  = p.society_id
WHERE cp.status = 'Active'
  AND (@from IS NULL OR DATE(cp.booking_date) >= DATE(@from))
  AND (@to   IS NULL OR DATE(cp.booking_date)  < DATE(@to, '+1 day'))
  AND (
       @kw = '' 
       OR b.name  LIKE @kwLike COLLATE NOCASE
       OR pt.code LIKE @kwLike COLLATE NOCASE
       OR p.plot_no LIKE @kwLike COLLATE NOCASE
  )
ORDER BY b.name, cp.booking_date DESC, p.plot_no;";

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
            var rds = new ReportDataSource("DataSet1", dt);
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "ReportCommisionBroker.rdlc");

            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found: " + reportPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
