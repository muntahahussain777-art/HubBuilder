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
    /// NEW VIP report only: Broker complete details —
    /// broker ↔ customer ↔ plot ↔ commission, with society filter + header society.
    /// Does not modify other forms / DB / UI.
    /// </summary>
    public partial class frmVipBrokerDetails : Sample
    {
        private DataTable _dtReport;
        private bool _busy;

        public frmVipBrokerDetails()
        {
            InitializeComponent();
        }

        private void frmVipBrokerDetails_Load(object sender, EventArgs e)
        {
            try
            {
                dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
                dtpTo.Value = DateTime.Today;
                LoadFilterSocieties();
                LoadHeaderSocieties();
                LoadBrokers();
                StyleVipGrid(dgvList);
                dgvList.DataBindingComplete -= DgvList_DataBindingComplete;
                dgvList.DataBindingComplete += DgvList_DataBindingComplete;
                dgvList.CellFormatting -= DgvList_CellFormatting;
                dgvList.CellFormatting += DgvList_CellFormatting;
                this.Text = "Broker Complete Details (VIP)";
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

        private void LoadBrokers()
        {
            try
            {
                var dt = NewIdNameTable();
                dt.Rows.Add(0, "(All Brokers)");
                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand("SELECT id, name FROM brokers ORDER BY name;", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            dt.Rows.Add(Convert.ToInt32(r["id"]), Convert.ToString(r["name"]));
                    }
                }
                cmbBroker.DisplayMember = "name";
                cmbBroker.ValueMember = "id";
                cmbBroker.DataSource = dt;
                cmbBroker.SelectedIndex = 0;
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

        private int GetBrokerId()
        {
            try
            {
                if (cmbBroker?.SelectedValue == null || cmbBroker.SelectedValue == DBNull.Value) return 0;
                return Convert.ToInt32(cmbBroker.SelectedValue);
            }
            catch { return 0; }
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

                _dtReport = BuildBrokerData();
                if (_dtReport == null || _dtReport.Rows.Count == 0)
                {
                    MessageBox.Show("Is filter mein broker records nahi mile.", "Broker Details VIP",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvList.DataSource = null;
                    reportViewer1.Clear();
                    return;
                }

                BindList(_dtReport);
                BindReport(_dtReport);
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
                if (cmbBroker != null && cmbBroker.Items.Count > 0) cmbBroker.SelectedIndex = 0;
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
                HideCol("broker_id");
                HideCol("row_type");
                HideCol("broker_phone");
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

        private void BindReport(DataTable dt)
        {
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipBrokerDetails.rdlc");
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
            }
            catch { }
            reportViewer1.RefreshReport();
        }

        private static DataTable CreateResultTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("sr_no", typeof(int));
            dt.Columns.Add("row_type", typeof(string));
            dt.Columns.Add("broker_id", typeof(int));
            dt.Columns.Add("broker_name", typeof(string));
            dt.Columns.Add("broker_phone", typeof(string));
            dt.Columns.Add("customer_name", typeof(string));
            dt.Columns.Add("customer_mobile", typeof(string));
            dt.Columns.Add("society_name", typeof(string));
            dt.Columns.Add("block_name", typeof(string));
            dt.Columns.Add("plot_type", typeof(string));
            dt.Columns.Add("plot_no", typeof(string));
            dt.Columns.Add("plot_price", typeof(double));
            dt.Columns.Add("broker_commission", typeof(double));
            dt.Columns.Add("booking_date", typeof(string));
            dt.Columns.Add("broker_total_commission", typeof(double));
            return dt;
        }

        private DataTable BuildBrokerData()
        {
            var result = CreateResultTable();
            string dateFrom = dtpFrom.Value.ToString("yyyy-MM-dd");
            string dateTo = dtpTo.Value.ToString("yyyy-MM-dd");
            int societyId = GetFilterSocietyId();
            int brokerId = GetBrokerId();
            string kw = (txtSearch?.Text ?? "").Trim();
            string kwLike = "%" + kw + "%";

            string sql = @"
SELECT
  IFNULL(b.id, 0) AS broker_id,
  IFNULL(b.name,'(No Broker)') AS broker_name,
  IFNULL(b.phone,'') AS broker_phone,
  c.name AS customer_name,
  IFNULL(c.mobile,'') AS customer_mobile,
  s.name AS society_name,
  bl.name AS block_name,
  pt.code AS plot_type,
  p.plot_no,
  IFNULL(cp.price,0) AS plot_price,
  IFNULL(cp.broker_commission,0) AS broker_commission,
  IFNULL(cp.booking_date,'') AS booking_date
FROM customer_plots cp
LEFT JOIN brokers    b  ON b.id  = cp.broker_id
JOIN customers  c  ON c.id  = cp.customer_id
JOIN plots      p  ON p.id  = cp.plot_id
JOIN plot_types pt ON pt.id = p.plot_type_id
JOIN blocks     bl ON bl.id = p.block_id
JOIN societies  s  ON s.id  = p.society_id
WHERE cp.status = 'Active'
  AND (IFNULL(cp.broker_id,0) > 0 OR IFNULL(cp.broker_commission,0) <> 0)
  AND DATE(cp.booking_date) >= DATE(@from)
  AND DATE(cp.booking_date) <= DATE(@to)
  AND (@societyId = 0 OR p.society_id = @societyId)
  AND (@brokerId = 0 OR cp.broker_id = @brokerId)
  AND (
       @kw = ''
       OR IFNULL(b.name,'') LIKE @kwLike COLLATE NOCASE
       OR c.name LIKE @kwLike COLLATE NOCASE
       OR c.mobile LIKE @kwLike COLLATE NOCASE
       OR s.name LIKE @kwLike COLLATE NOCASE
       OR p.plot_no LIKE @kwLike COLLATE NOCASE
       OR pt.code LIKE @kwLike COLLATE NOCASE
  )
ORDER BY b.name, s.name, c.name, p.plot_no;";

            var brokerTotals = new System.Collections.Generic.Dictionary<int, double>();

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@from", dateFrom);
                cmd.Parameters.AddWithValue("@to", dateTo);
                cmd.Parameters.AddWithValue("@societyId", societyId);
                cmd.Parameters.AddWithValue("@brokerId", brokerId);
                cmd.Parameters.AddWithValue("@kw", kw);
                cmd.Parameters.AddWithValue("@kwLike", string.IsNullOrEmpty(kw) ? "%" : kwLike);

                con.Open();
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var raw = new DataTable();
                    da.Fill(raw);

                    foreach (DataRow r in raw.Rows)
                    {
                        int bid = Convert.ToInt32(r["broker_id"]);
                        double comm = Convert.ToDouble(r["broker_commission"]);
                        if (!brokerTotals.ContainsKey(bid)) brokerTotals[bid] = 0;
                        brokerTotals[bid] += comm;
                    }

                    int sr = 0;
                    double grand = 0;
                    foreach (DataRow r in raw.Rows)
                    {
                        sr++;
                        int bid = Convert.ToInt32(r["broker_id"]);
                        double comm = Convert.ToDouble(r["broker_commission"]);
                        grand += comm;
                        result.Rows.Add(
                            sr,
                            "DETAIL",
                            bid,
                            Convert.ToString(r["broker_name"]),
                            Convert.ToString(r["broker_phone"]),
                            Convert.ToString(r["customer_name"]),
                            Convert.ToString(r["customer_mobile"]),
                            Convert.ToString(r["society_name"]),
                            Convert.ToString(r["block_name"]),
                            Convert.ToString(r["plot_type"]),
                            Convert.ToString(r["plot_no"]),
                            Convert.ToDouble(r["plot_price"]),
                            comm,
                            Convert.ToString(r["booking_date"]),
                            brokerTotals.ContainsKey(bid) ? brokerTotals[bid] : 0
                        );
                    }

                    if (result.Rows.Count > 0)
                    {
                        result.Rows.Add(
                            0,
                            "TOTAL",
                            0,
                            "TOTAL COMMISSION",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            0d,
                            grand,
                            "",
                            grand
                        );
                    }
                }
            }

            return result;
        }
    }
}
