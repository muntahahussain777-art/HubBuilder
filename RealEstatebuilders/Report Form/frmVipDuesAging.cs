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
    /// NEW VIP report only: Dues Aging with buckets
    /// Current / 0-30 / 31-60 / 61-90 / >90 — same ledger formula as frmAging.
    /// Society filter + header society. Does not modify other forms / DB / UI.
    /// </summary>
    public partial class frmVipDuesAging : Sample
    {
        private DataTable _dtReport;
        private bool _busy;

        public frmVipDuesAging()
        {
            InitializeComponent();
        }

        private void frmVipDuesAging_Load(object sender, EventArgs e)
        {
            try
            {
                dtpAsOf.Value = DateTime.Today;
                dtpAsOf.Width = 400;
                if (dtpAsOfDummy != null) dtpAsOfDummy.Visible = false;
                if (lblBucketHint != null) lblBucketHint.Visible = false;

                if (cmbBucket.Items.Count == 0)
                    cmbBucket.Items.AddRange(new object[] {
                        "(All Buckets)", "Current", "0-30", "31-60", "61-90", ">90", "(Due Only)"
                    });
                cmbBucket.SelectedIndex = 0;

                LoadFilterSocieties();
                LoadHeaderSocieties();
                StyleVipGrid(dgvList);
                dgvList.DataBindingComplete -= DgvList_DataBindingComplete;
                dgvList.DataBindingComplete += DgvList_DataBindingComplete;
                dgvList.CellFormatting -= DgvList_CellFormatting;
                dgvList.CellFormatting += DgvList_CellFormatting;
                this.Text = "Dues Aging (VIP)";
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

        private string GetBucketFilter()
        {
            try
            {
                string t = Convert.ToString(cmbBucket.SelectedItem);
                if (string.IsNullOrWhiteSpace(t) || t.StartsWith("(All", StringComparison.OrdinalIgnoreCase))
                    return null;
                return t;
            }
            catch { return null; }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Cursor = Cursors.WaitCursor;
                _dtReport = BuildAgingData();
                if (_dtReport == null || _dtReport.Rows.Count == 0)
                {
                    MessageBox.Show("Is filter mein aging records nahi mile.", "Dues Aging VIP",
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
                dtpAsOf.Value = DateTime.Today;
                if (cmbFilterSociety != null && cmbFilterSociety.Items.Count > 0) cmbFilterSociety.SelectedIndex = 0;
                if (cmbHeaderSociety != null && cmbHeaderSociety.Items.Count > 0) cmbHeaderSociety.SelectedIndex = 0;
                if (cmbBucket != null && cmbBucket.Items.Count > 0) cmbBucket.SelectedIndex = 0;
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
                HideCol("customer_plot_id");
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
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipDuesAging.rdlc");
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
                reportViewer1.LocalReport.SetParameters(new ReportParameter("AsOfDate",
                    dtpAsOf.Value.ToString("dd-MMM-yyyy")));
            }
            catch { }
            reportViewer1.RefreshReport();
        }

        private static DataTable CreateResultTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("sr_no", typeof(int));
            dt.Columns.Add("row_type", typeof(string));
            dt.Columns.Add("customer_plot_id", typeof(int));
            dt.Columns.Add("customer_name", typeof(string));
            dt.Columns.Add("society_name", typeof(string));
            dt.Columns.Add("block_name", typeof(string));
            dt.Columns.Add("plot_no", typeof(string));
            dt.Columns.Add("plot_type", typeof(string));
            dt.Columns.Add("paid", typeof(double));
            dt.Columns.Add("due_amount", typeof(double));
            dt.Columns.Add("oldest_unpaid_date", typeof(string));
            dt.Columns.Add("days_overdue", typeof(int));
            dt.Columns.Add("aging_bucket", typeof(string));
            return dt;
        }

        private DataTable BuildAgingData()
        {
            var result = CreateResultTable();
            string asOfStr = dtpAsOf.Value.Date.ToString("yyyy-MM-dd");
            int societyId = GetFilterSocietyId();
            string bucket = GetBucketFilter();
            string kw = (txtSearch?.Text ?? "").Trim();
            string kwLike = "%" + kw + "%";

            string sql = @"
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
    cp.id AS customer_plot_id,
    c.name AS customer_name,
    s.name AS society_name,
    bl.name AS block_name,
    p.plot_no,
    pt.code AS plot_type,
    COALESCE(rc.paid,0) AS paid,
    CASE 
      WHEN COALESCE(deb.billed,0) > 0
        THEN COALESCE(deb.billed,0) - COALESCE(rc.paid,0)
      ELSE COALESCE(cp.price,0) - COALESCE(rc.paid,0)
    END AS due_amount,
    COALESCE(deb.oldest_unpaid_date, cp.booking_date) AS oldest_unpaid_date,
    CAST(ROUND(julianday(@AsOfDate) - julianday(COALESCE(deb.oldest_unpaid_date, cp.booking_date))) AS INTEGER) AS days_overdue,
    CASE
      WHEN (CASE 
              WHEN COALESCE(deb.billed,0) > 0
                THEN COALESCE(deb.billed,0) - COALESCE(rc.paid,0)
              ELSE COALESCE(cp.price,0) - COALESCE(rc.paid,0)
            END) <= 0 THEN 'Current'
      WHEN (julianday(@AsOfDate) - julianday(COALESCE(deb.oldest_unpaid_date, cp.booking_date))) <= 30 THEN '0-30'
      WHEN (julianday(@AsOfDate) - julianday(COALESCE(deb.oldest_unpaid_date, cp.booking_date))) <= 60 THEN '31-60'
      WHEN (julianday(@AsOfDate) - julianday(COALESCE(deb.oldest_unpaid_date, cp.booking_date))) <= 90 THEN '61-90'
      ELSE '>90'
    END AS aging_bucket
  FROM customer_plots cp
  LEFT JOIN deb ON deb.customer_plot_id = cp.id
  LEFT JOIN rc  ON rc.customer_plot_id  = cp.id
  JOIN customers  c  ON c.id = cp.customer_id
  JOIN plots      p  ON p.id = cp.plot_id
  JOIN plot_types pt ON pt.id = p.plot_type_id
  JOIN blocks     bl ON bl.id = p.block_id
  JOIN societies  s  ON s.id = p.society_id
  WHERE IFNULL(cp.status,'Active') = 'Active'
    AND (@societyId = 0 OR p.society_id = @societyId)
    AND (
         @kw = ''
         OR c.name LIKE @kwLike COLLATE NOCASE
         OR p.plot_no LIKE @kwLike COLLATE NOCASE
         OR pt.code LIKE @kwLike COLLATE NOCASE
         OR s.name LIKE @kwLike COLLATE NOCASE
    )
)
SELECT * FROM q
ORDER BY
  CASE aging_bucket
    WHEN '>90' THEN 1 WHEN '61-90' THEN 2 WHEN '31-60' THEN 3 WHEN '0-30' THEN 4 ELSE 5 END,
  due_amount DESC,
  customer_name;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AsOfDate", asOfStr);
                cmd.Parameters.AddWithValue("@societyId", societyId);
                cmd.Parameters.AddWithValue("@kw", kw);
                cmd.Parameters.AddWithValue("@kwLike", string.IsNullOrEmpty(kw) ? "%" : kwLike);

                con.Open();
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var raw = new DataTable();
                    da.Fill(raw);

                    int sr = 0;
                    double totalDue = 0;
                    double totalPaid = 0;

                    foreach (DataRow r in raw.Rows)
                    {
                        double due = Convert.ToDouble(r["due_amount"]);
                        string bkt = Convert.ToString(r["aging_bucket"]);

                        if (bucket == "(Due Only)" && due <= 0) continue;
                        if (bucket != null && bucket != "(Due Only)" &&
                            !string.Equals(bkt, bucket, StringComparison.OrdinalIgnoreCase))
                            continue;

                        sr++;
                        double paid = Convert.ToDouble(r["paid"]);
                        totalDue += Math.Max(0, due);
                        totalPaid += paid;

                        result.Rows.Add(
                            sr,
                            "DETAIL",
                            Convert.ToInt32(r["customer_plot_id"]),
                            Convert.ToString(r["customer_name"]),
                            Convert.ToString(r["society_name"]),
                            Convert.ToString(r["block_name"]),
                            Convert.ToString(r["plot_no"]),
                            Convert.ToString(r["plot_type"]),
                            paid,
                            due,
                            Convert.ToString(r["oldest_unpaid_date"]),
                            Convert.ToInt32(r["days_overdue"]),
                            bkt
                        );
                    }

                    if (result.Rows.Count > 0)
                    {
                        result.Rows.Add(
                            0,
                            "TOTAL",
                            0,
                            "TOTAL",
                            "",
                            "",
                            "",
                            "",
                            totalPaid,
                            totalDue,
                            "",
                            0,
                            ""
                        );
                    }
                }
            }

            return result;
        }
    }
}
