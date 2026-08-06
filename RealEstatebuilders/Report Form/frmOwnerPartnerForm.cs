using Microsoft.Reporting.WinForms;
using HubBuilders.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    public partial class frmOwnerPartnerForm : Sample
    {
        // Guna2DateTimePicker support: “nullable” via flags
        private bool _useFrom = false;
        private bool _useTo = false;
        public frmOwnerPartnerForm()
        {
            InitializeComponent();
            ReportHeaderSocietyHelper.Attach(this);
            this.Load += frmOwnerPartnerForm_Load;

            txtSearch.TextChanged += (s, e) => GetData();

            // Guna2DateTimePicker events
            fromdate.ValueChanged += (s, e) => { _useFrom = true; GetData(); };
            todate.ValueChanged += (s, e) => { _useTo = true; GetData(); };

            // Optional: double–click to clear a date (remove filter)
            fromdate.MouseDoubleClick += (s, e) => { _useFrom = false; GetData(); };
            todate.MouseDoubleClick += (s, e) => { _useTo = false; GetData(); };
        }

        private void frmOwnerPartnerForm_Load(object sender, EventArgs e)
        {
            GetData();
        }

        public void GetData()
        {
            try
            {
                // Nullable dates from flags
                object fromParam = _useFrom ? (object)fromdate.Value.Date.ToString("yyyy-MM-dd") : DBNull.Value;
                object toParam = _useTo ? (object)todate.Value.Date.ToString("yyyy-MM-dd") : DBNull.Value;

                // Swap if reversed
                if (fromParam is string fs && toParam is string ts && DateTime.Parse(fs) > DateTime.Parse(ts))
                { var tmp = fromParam; fromParam = toParam; toParam = tmp; }

                // Partner-only text filter
                string term = (txtSearch.Text ?? "").Trim();
                object textParam = string.IsNullOrEmpty(term) ? (object)DBNull.Value : term;

                // Optional ID filters (no UI → NULL)
                object societyIdParam = DBNull.Value;
                object partnerIdParam = DBNull.Value;
                object ownerIdParam = DBNull.Value;

                string sql = @"
SELECT
    s.id          AS societyId,
    s.name        AS society,
    o.person_id   AS ownerId,
    o.name        AS owner,
    p.person_id   AS partnerId,
    p.name        AS partner,
    c.pay_date    AS payDate,
    c.amount      AS amount,
    c.note        AS note,
    c.credit_id   AS creditId,
    d.deal_id     AS dealId
FROM Credit c
JOIN Deal      d ON d.deal_id    = c.deal_id
JOIN Person    o ON o.person_id  = d.owner_id   AND o.is_owner   = 1
JOIN Person    p ON p.person_id  = c.partner_id AND p.is_partner = 1
JOIN societies s ON s.id         = d.society_id
WHERE (@from     IS NULL OR DATE(c.pay_date) >= DATE(@from))
  AND (@to       IS NULL OR DATE(c.pay_date) <  DATE(@to, '+1 day'))
  AND (COALESCE(@societyId, 0) = 0 OR d.society_id = @societyId)
  AND (COALESCE(@partnerId, 0) = 0 OR c.partner_id = @partnerId)
  AND (COALESCE(@ownerId,   0) = 0 OR d.owner_id   = @ownerId)
  AND (@text IS NULL OR @text = '' OR p.name LIKE '%' || @text || '%' COLLATE NOCASE)
ORDER BY partner, owner, payDate, creditId;";

                string cs = projectconnection.conReturn();
                using (var con = new SQLiteConnection(cs))
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@from", fromParam);
                    cmd.Parameters.AddWithValue("@to", toParam);
                    cmd.Parameters.AddWithValue("@societyId", societyIdParam);
                    cmd.Parameters.AddWithValue("@partnerId", partnerIdParam);
                    cmd.Parameters.AddWithValue("@ownerId", ownerIdParam);
                    cmd.Parameters.AddWithValue("@text", textParam);

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
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "OwnerPartnerReport.rdlc");
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
