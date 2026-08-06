using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// NEW VIP report only: Partner Cash Book —
    /// partner In/Out + linked broker / customer / plot / society.
    /// Does not modify Partner Cash Book form or other UI.
    /// </summary>
    public partial class frmVipPartnerCashBook : Sample
    {
        private DataTable _dtReport;
        private bool _busy;

        public frmVipPartnerCashBook()
        {
            InitializeComponent();
        }

        private void frmVipPartnerCashBook_Load(object sender, EventArgs e)
        {
            try
            {
                MainClass.EnsurePartnerMoneySchema();
                dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
                dtpTo.Value = DateTime.Today;
                LoadFilterSocieties();
                LoadHeaderSocieties();
                LoadPartners();
                LoadDirections();
                StyleVipGrid(dgvList);
                dgvList.DataBindingComplete -= DgvList_DataBindingComplete;
                dgvList.DataBindingComplete += DgvList_DataBindingComplete;
                dgvList.CellFormatting -= DgvList_CellFormatting;
                dgvList.CellFormatting += DgvList_CellFormatting;
                this.Text = "Partner Cash Book (VIP)";
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void LoadHeaderSocieties()
        {
            try
            {
                var dt = NewIdNameTable();
                dt.Rows.Add(0, "(Hub Builders — default)");
                FillSocieties(dt);
                cmbHeaderSociety.DisplayMember = "name";
                cmbHeaderSociety.ValueMember = "id";
                cmbHeaderSociety.DataSource = dt;
                cmbHeaderSociety.SelectedIndex = 0;
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private void LoadFilterSocieties()
        {
            try
            {
                var dt = NewIdNameTable();
                dt.Rows.Add(0, "(All Societies)");
                FillSocieties(dt);
                cmbFilterSociety.DisplayMember = "name";
                cmbFilterSociety.ValueMember = "id";
                cmbFilterSociety.DataSource = dt;
                cmbFilterSociety.SelectedIndex = 0;
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private void LoadPartners()
        {
            try
            {
                var dt = NewIdNameTable();
                dt.Rows.Add(0, "(All Partners)");
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(
                    "SELECT person_id AS id, name FROM Person WHERE is_partner=1 ORDER BY name;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                    }
                }
                cmbPartner.DisplayMember = "name";
                cmbPartner.ValueMember = "id";
                cmbPartner.DataSource = dt;
                cmbPartner.SelectedIndex = 0;
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private void LoadDirections()
        {
            try
            {
                cmbDirection.Items.Clear();
                cmbDirection.Items.Add("(All In / Out)");
                cmbDirection.Items.Add("In");
                cmbDirection.Items.Add("Out");
                cmbDirection.SelectedIndex = 0;
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private static DataTable NewIdNameTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));
            return dt;
        }

        private static void FillSocieties(DataTable dt)
        {
            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand("SELECT id, name FROM societies ORDER BY name;", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                }
            }
        }

        private string GetHeaderBrand()
        {
            try
            {
                if (cmbHeaderSociety?.SelectedValue == null) return "HUB BUILDERS";
                int id = Convert.ToInt32(cmbHeaderSociety.SelectedValue);
                if (id <= 0) return "HUB BUILDERS";
                string name = Convert.ToString(cmbHeaderSociety.Text);
                return string.IsNullOrWhiteSpace(name) ? "HUB BUILDERS" : name.Trim().ToUpperInvariant();
            }
            catch { return "HUB BUILDERS"; }
        }

        private int GetFilterSocietyId()
        {
            try
            {
                if (cmbFilterSociety?.SelectedValue == null || cmbFilterSociety.SelectedValue == DBNull.Value) return 0;
                return Convert.ToInt32(cmbFilterSociety.SelectedValue);
            }
            catch { return 0; }
        }

        private int GetPartnerId()
        {
            try
            {
                if (cmbPartner?.SelectedValue == null || cmbPartner.SelectedValue == DBNull.Value) return 0;
                return Convert.ToInt32(cmbPartner.SelectedValue);
            }
            catch { return 0; }
        }

        private string GetDirection()
        {
            try
            {
                string s = Convert.ToString(cmbDirection?.SelectedItem) ?? "";
                if (string.Equals(s, "In", StringComparison.OrdinalIgnoreCase)) return "In";
                if (string.Equals(s, "Out", StringComparison.OrdinalIgnoreCase)) return "Out";
                return "";
            }
            catch { return ""; }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Cursor = Cursors.WaitCursor;
                if (dtpFrom.Value.Date > dtpTo.Value.Date)
                {
                    MessageBox.Show("From date To date se bara nahi ho sakta.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MainClass.EnsurePartnerMoneySchema();
                string summary;
                _dtReport = BuildCashBookData(out summary);
                if (_dtReport == null || _dtReport.Rows.Count == 0)
                {
                    MessageBox.Show("Is filter mein Partner Cash Book records nahi mile.", "Partner Cash Book VIP",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvList.DataSource = null;
                    reportViewer1.Clear();
                    return;
                }

                BindList(_dtReport);
                BindReport(_dtReport, summary);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            finally
            {
                Cursor = Cursors.Default;
                _busy = false;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
                dtpTo.Value = DateTime.Today;
                if (cmbFilterSociety != null && cmbFilterSociety.Items.Count > 0) cmbFilterSociety.SelectedIndex = 0;
                if (cmbHeaderSociety != null && cmbHeaderSociety.Items.Count > 0) cmbHeaderSociety.SelectedIndex = 0;
                if (cmbPartner != null && cmbPartner.Items.Count > 0) cmbPartner.SelectedIndex = 0;
                if (cmbDirection != null && cmbDirection.Items.Count > 0) cmbDirection.SelectedIndex = 0;
                if (txtSearch != null) txtSearch.Clear();
                _dtReport = null;
                dgvList.DataSource = null;
                reportViewer1.Clear();
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private void BindList(DataTable dt)
        {
            try
            {
                StyleVipGrid(dgvList);
                dgvList.AutoGenerateColumns = true;
                dgvList.DataSource = dt.Copy();
                StyleVipGrid(dgvList);
                ForceWhiteColumns(dgvList);
                HideCol("row_type");
                HideCol("partner_id");
            }
            catch (Exception ex) { HubBuilders.AppSafety.ShowSoft(ex); }
        }

        private static void StyleVipGrid(DataGridView gv)
        {
            if (gv == null) return;
            try
            {
                Color dark = Color.FromArgb(32, 36, 66);
                Color darkAlt = Color.FromArgb(37, 41, 74);
                Color header = Color.FromArgb(20, 45, 95);
                Color sel = Color.FromArgb(45, 52, 90);
                gv.ForeColor = Color.White;
                gv.BackgroundColor = dark;
                gv.GridColor = Color.FromArgb(50, 56, 76);
                gv.BorderStyle = BorderStyle.None;
                gv.EnableHeadersVisualStyles = false;
                gv.ColumnHeadersDefaultCellStyle.BackColor = header;
                gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                gv.ColumnHeadersDefaultCellStyle.SelectionBackColor = header;
                gv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                gv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                gv.DefaultCellStyle.BackColor = dark;
                gv.DefaultCellStyle.ForeColor = Color.White;
                gv.DefaultCellStyle.SelectionBackColor = sel;
                gv.DefaultCellStyle.SelectionForeColor = Color.White;
                gv.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                gv.RowsDefaultCellStyle.BackColor = dark;
                gv.RowsDefaultCellStyle.ForeColor = Color.White;
                gv.RowsDefaultCellStyle.SelectionBackColor = sel;
                gv.RowsDefaultCellStyle.SelectionForeColor = Color.White;
                gv.AlternatingRowsDefaultCellStyle.BackColor = darkAlt;
                gv.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
                gv.AlternatingRowsDefaultCellStyle.SelectionBackColor = sel;
                gv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
                ForceWhiteColumns(gv);
            }
            catch { }
        }

        private static void ForceWhiteColumns(DataGridView gv)
        {
            if (gv == null) return;
            try
            {
                Color dark = Color.FromArgb(32, 36, 66);
                Color header = Color.FromArgb(20, 45, 95);
                Color sel = Color.FromArgb(45, 52, 90);
                foreach (DataGridViewColumn col in gv.Columns)
                {
                    col.DefaultCellStyle.ForeColor = Color.White;
                    col.DefaultCellStyle.BackColor = dark;
                    col.DefaultCellStyle.SelectionForeColor = Color.White;
                    col.DefaultCellStyle.SelectionBackColor = sel;
                    col.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    col.HeaderCell.Style.ForeColor = Color.White;
                    col.HeaderCell.Style.BackColor = header;
                    col.HeaderCell.Style.SelectionForeColor = Color.White;
                    col.HeaderCell.Style.SelectionBackColor = header;
                    col.HeaderCell.Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
                foreach (DataGridViewRow row in gv.Rows)
                {
                    row.DefaultCellStyle.ForeColor = Color.White;
                    row.DefaultCellStyle.SelectionForeColor = Color.White;
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.ForeColor = Color.White;
                        cell.Style.SelectionForeColor = Color.White;
                    }
                }
            }
            catch { }
        }

        private void DgvList_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                StyleVipGrid(dgvList);
                ForceWhiteColumns(dgvList);
            }
            catch { }
        }

        private void DgvList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.CellStyle == null) return;
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.SelectionForeColor = Color.White;
                e.CellStyle.SelectionBackColor = Color.FromArgb(45, 52, 90);
                if (e.RowIndex >= 0 && (e.RowIndex % 2) == 1)
                    e.CellStyle.BackColor = Color.FromArgb(37, 41, 74);
                else
                    e.CellStyle.BackColor = Color.FromArgb(32, 36, 66);
            }
            catch { }
        }

        private void HideCol(string name)
        {
            try
            {
                if (dgvList.Columns.Contains(name))
                    dgvList.Columns[name].Visible = false;
            }
            catch { }
        }

        private void BindReport(DataTable dt, string summary)
        {
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipPartnerCashBook.rdlc");
            if (!File.Exists(reportPath))
            {
                MessageBox.Show("Report file not found:\n" + reportPath, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            reportViewer1.Reset();
            reportViewer1.ProcessingMode = ProcessingMode.Local;
            reportViewer1.LocalReport.ReportPath = reportPath;
            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
            try
            {
                reportViewer1.LocalReport.SetParameters(new ReportParameter("HeaderBrand", GetHeaderBrand()));
                reportViewer1.LocalReport.SetParameters(new ReportParameter("PeriodText",
                    dtpFrom.Value.ToString("dd-MMM-yyyy") + "  →  " + dtpTo.Value.ToString("dd-MMM-yyyy")));
                reportViewer1.LocalReport.SetParameters(new ReportParameter("SummaryText", summary ?? ""));
            }
            catch { }
            reportViewer1.RefreshReport();
        }

        private static DataTable CreateResultTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("sr_no", typeof(int));
            dt.Columns.Add("row_type", typeof(string));
            dt.Columns.Add("partner_id", typeof(int));
            dt.Columns.Add("txn_date", typeof(string));
            dt.Columns.Add("partner_name", typeof(string));
            dt.Columns.Add("direction", typeof(string));
            dt.Columns.Add("amount_in", typeof(double));
            dt.Columns.Add("amount_out", typeof(double));
            dt.Columns.Add("category", typeof(string));
            dt.Columns.Add("mode", typeof(string));
            dt.Columns.Add("broker_name", typeof(string));
            dt.Columns.Add("customer_name", typeof(string));
            dt.Columns.Add("plot_no", typeof(string));
            dt.Columns.Add("society_name", typeof(string));
            dt.Columns.Add("note", typeof(string));
            dt.Columns.Add("balance", typeof(double));
            return dt;
        }

        private DataTable BuildCashBookData(out string summary)
        {
            summary = "";
            var result = CreateResultTable();
            string dateFrom = dtpFrom.Value.ToString("yyyy-MM-dd");
            string dateTo = dtpTo.Value.ToString("yyyy-MM-dd");
            int societyId = GetFilterSocietyId();
            int partnerId = GetPartnerId();
            string dir = GetDirection();
            string kw = (txtSearch?.Text ?? "").Trim();
            string kwLike = "%" + kw + "%";

            string sql = @"
SELECT
  pm.id,
  pm.partner_id,
  IFNULL(pm.txn_date,'') AS txn_date,
  IFNULL(p.name,'') AS partner_name,
  IFNULL(pm.direction,'') AS direction,
  IFNULL(pm.amount,0) AS amount,
  IFNULL(pm.category,'') AS category,
  IFNULL(pm.mode,'') AS mode,
  IFNULL(pm.note,'') AS note,
  IFNULL(br.name,'') AS broker_name,
  IFNULL(c.name,'') AS customer_name,
  IFNULL(CAST(pl.plot_no AS TEXT),'') AS plot_no,
  IFNULL(s.name,'') AS society_name
FROM partner_money pm
JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN brokers br ON br.id = cp.broker_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
LEFT JOIN societies s ON s.id = COALESCE(pm.society_id, pl.society_id)
WHERE DATE(pm.txn_date) >= DATE(@from)
  AND DATE(pm.txn_date) <= DATE(@to)
  AND (@societyId = 0 OR COALESCE(pm.society_id, pl.society_id, 0) = @societyId)
  AND (@partnerId = 0 OR pm.partner_id = @partnerId)
  AND (@dir = '' OR pm.direction = @dir)
  AND (
       @kw = ''
       OR p.name LIKE @kwLike COLLATE NOCASE
       OR IFNULL(pm.category,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(pm.note,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(pm.mode,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(pm.direction,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(br.name,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(c.name,'') LIKE @kwLike COLLATE NOCASE
       OR IFNULL(s.name,'') LIKE @kwLike COLLATE NOCASE
       OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE @kwLike COLLATE NOCASE
  )
ORDER BY DATE(pm.txn_date) ASC, pm.id ASC;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@from", dateFrom);
                cmd.Parameters.AddWithValue("@to", dateTo);
                cmd.Parameters.AddWithValue("@societyId", societyId);
                cmd.Parameters.AddWithValue("@partnerId", partnerId);
                cmd.Parameters.AddWithValue("@dir", dir ?? "");
                cmd.Parameters.AddWithValue("@kw", kw);
                cmd.Parameters.AddWithValue("@kwLike", string.IsNullOrEmpty(kw) ? "%" : kwLike);

                con.Open();
                var raw = new DataTable();
                using (var da = new SQLiteDataAdapter(cmd))
                    da.Fill(raw);

                int sr = 0;
                double totalIn = 0;
                double totalOut = 0;
                foreach (DataRow r in raw.Rows)
                {
                    sr++;
                    string direction = Convert.ToString(r["direction"]);
                    double amt = 0;
                    try { amt = Convert.ToDouble(r["amount"]); } catch { amt = 0; }
                    double ain = string.Equals(direction, "In", StringComparison.OrdinalIgnoreCase) ? amt : 0d;
                    double aout = string.Equals(direction, "Out", StringComparison.OrdinalIgnoreCase) ? amt : 0d;
                    totalIn += ain;
                    totalOut += aout;
                    result.Rows.Add(
                        sr,
                        "DETAIL",
                        Convert.ToInt32(r["partner_id"]),
                        Convert.ToString(r["txn_date"]),
                        Convert.ToString(r["partner_name"]),
                        direction,
                        ain,
                        aout,
                        Convert.ToString(r["category"]),
                        Convert.ToString(r["mode"]),
                        Convert.ToString(r["broker_name"]),
                        Convert.ToString(r["customer_name"]),
                        Convert.ToString(r["plot_no"]),
                        Convert.ToString(r["society_name"]),
                        Convert.ToString(r["note"]),
                        totalIn - totalOut
                    );
                }

                if (result.Rows.Count > 0)
                {
                    result.Rows.Add(
                        0,
                        "TOTAL",
                        0,
                        "",
                        "TOTAL / BALANCE",
                        "",
                        totalIn,
                        totalOut,
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        totalIn - totalOut
                    );
                }

                summary = "Total In: " + totalIn.ToString("N2")
                    + "     Total Out: " + totalOut.ToString("N2")
                    + "     Balance (In − Out): " + (totalIn - totalOut).ToString("N2");
            }

            return result;
        }
    }
}
