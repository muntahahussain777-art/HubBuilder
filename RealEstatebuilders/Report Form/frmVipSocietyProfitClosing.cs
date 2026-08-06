using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// NEW VIP report only: Society-wise Profit Closing
    /// Allocated + Installments/Receipts + Expenses → Gross & Net per society.
    /// Does not modify other forms / DB / UI.
    /// </summary>
    public partial class frmVipSocietyProfitClosing : Sample
    {
        private DataTable _dtReport;
        private bool _busy;

        public frmVipSocietyProfitClosing()
        {
            InitializeComponent();
        }

        private void frmVipSocietyProfitClosing_Load(object sender, EventArgs e)
        {
            try
            {
                dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
                dtpTo.Value = DateTime.Today;
                if (cmbMode.Items.Count == 0)
                    cmbMode.Items.AddRange(new object[] { "(All Modes)", "Cash", "Bank" });
                cmbMode.SelectedIndex = 0;
                LoadFilterSocieties();
                LoadHeaderSocieties();
                StyleVipGrid(dgvList);
                dgvList.DataBindingComplete -= DgvList_DataBindingComplete;
                dgvList.DataBindingComplete += DgvList_DataBindingComplete;
                dgvList.CellFormatting -= DgvList_CellFormatting;
                dgvList.CellFormatting += DgvList_CellFormatting;
                this.Text = "Society Profit Closing";
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

        private string GetModeFilter()
        {
            try
            {
                string t = Convert.ToString(cmbMode.SelectedItem);
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
                if (dtpFrom.Value.Date > dtpTo.Value.Date)
                {
                    MessageBox.Show("From date To date se bara nahi ho sakta.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _dtReport = BuildProfitClosingData();
                if (_dtReport == null || _dtReport.Rows.Count == 0)
                {
                    MessageBox.Show("Is period / filter mein records nahi mile.", "Society Profit Closing",
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
                if (cmbMode != null && cmbMode.Items.Count > 0) cmbMode.SelectedIndex = 0;
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
                HideCol("society_id");
                HideCol("row_type");
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
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipSocietyProfitClosing.rdlc");
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

        private DataTable BuildProfitClosingData()
        {
            var result = CreateResultTable();
            string dateFrom = dtpFrom.Value.ToString("yyyy-MM-dd");
            string dateTo = dtpTo.Value.ToString("yyyy-MM-dd");
            int societyId = GetFilterSocietyId();
            string mode = GetModeFilter();
            string kw = (txtSearch?.Text ?? "").Trim();

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();

                // Company-wide expense buckets (no society column in Expensetbl)
                double companyCogs = 0, companyOpex = 0;
                using (var cmd = new SQLiteCommand(@"
SELECT
  SUM(CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN COALESCE(Amount,0) ELSE 0 END) AS cogs_sum,
  SUM(CASE 
        WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%' 
          OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
        THEN 0 ELSE COALESCE(Amount,0) END) AS opex_sum
FROM Expensetbl e
WHERE DATE(e.EDate) >= DATE(@df)
  AND DATE(e.EDate) < DATE(@dt, '+1 day');", con))
                {
                    cmd.Parameters.AddWithValue("@df", dateFrom);
                    cmd.Parameters.AddWithValue("@dt", dateTo);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            companyCogs = SafeDouble(r["cogs_sum"]);
                            companyOpex = SafeDouble(r["opex_sum"]);
                        }
                    }
                }

                // Per-society allocated + receipts + broker + plot inventory
                string sql = @"
SELECT
  COALESCE(s.id, 0) AS society_id,
  COALESCE(s.name, '(Unknown Society)') AS society_name,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id), 0) AS plots_total,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Available'), 0) AS plots_available,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Hold'), 0) AS plots_booked,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Sold'), 0) AS plots_sold,
  COALESCE((
      SELECT COUNT(1)
      FROM customer_plots cp2
      JOIN plots p2 ON p2.id = cp2.plot_id
      WHERE p2.society_id = s.id
        AND cp2.status = 'Active'
        AND DATE(cp2.booking_date) >= DATE(@df)
        AND DATE(cp2.booking_date) < DATE(@dt, '+1 day')
  ), 0) AS alloc_count,
  COALESCE((
      SELECT SUM(COALESCE(cp3.price,0))
      FROM customer_plots cp3
      JOIN plots p3 ON p3.id = cp3.plot_id
      WHERE p3.society_id = s.id
        AND cp3.status = 'Active'
        AND DATE(cp3.booking_date) >= DATE(@df)
        AND DATE(cp3.booking_date) < DATE(@dt, '+1 day')
  ), 0) AS allocated_amount,
  COALESCE((
      SELECT SUM(COALESCE(cp4.broker_commission,0))
      FROM customer_plots cp4
      JOIN plots p4 ON p4.id = cp4.plot_id
      WHERE p4.society_id = s.id
        AND cp4.status = 'Active'
        AND DATE(cp4.booking_date) >= DATE(@df)
        AND DATE(cp4.booking_date) < DATE(@dt, '+1 day')
  ), 0) AS broker_commission,
  COALESCE((
      SELECT SUM(COALESCE(r.amount,0))
      FROM receipts r
      JOIN customer_plots cp ON cp.id = r.customer_plot_id
      JOIN plots p ON p.id = cp.plot_id
      WHERE p.society_id = s.id
        AND (@mode IS NULL OR r.mode = @mode)
        AND DATE(r.receipt_date) >= DATE(@df)
        AND DATE(r.receipt_date) < DATE(@dt, '+1 day')
  ), 0) AS receipts_amount
FROM societies s
WHERE (@sid = 0 OR s.id = @sid)
  AND (@kw = '' OR s.name LIKE '%' || @kw || '%' COLLATE NOCASE)
ORDER BY s.name;";

                var rows = new List<SocRow>();
                double totalReceipts = 0;

                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@df", dateFrom);
                    cmd.Parameters.AddWithValue("@dt", dateTo);
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    cmd.Parameters.AddWithValue("@kw", kw);
                    if (mode == null)
                        cmd.Parameters.AddWithValue("@mode", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@mode", mode);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var row = new SocRow
                            {
                                SocietyId = Convert.ToInt32(r["society_id"]),
                                SocietyName = Convert.ToString(r["society_name"]),
                                PlotsTotal = Convert.ToInt32(r["plots_total"]),
                                PlotsAvailable = Convert.ToInt32(r["plots_available"]),
                                PlotsBooked = Convert.ToInt32(r["plots_booked"]),
                                PlotsSold = Convert.ToInt32(r["plots_sold"]),
                                AllocCount = Convert.ToInt32(r["alloc_count"]),
                                Allocated = SafeDouble(r["allocated_amount"]),
                                Broker = SafeDouble(r["broker_commission"]),
                                Receipts = SafeDouble(r["receipts_amount"])
                            };
                            // Keep societies with inventory or period activity
                            if (row.PlotsTotal > 0 || row.AllocCount > 0 || row.Allocated > 0 || row.Receipts > 0 || row.Broker > 0 || societyId > 0)
                            {
                                rows.Add(row);
                                totalReceipts += row.Receipts;
                            }
                        }
                    }
                }

                // Prorate expenses by receipt share (same cash-basis idea as existing P&L)
                int sr = 0;
                double sumAlloc = 0, sumRec = 0, sumCogs = 0, sumOpex = 0, sumBroker = 0, sumGross = 0, sumNet = 0;
                int sumAllocCnt = 0, sumTot = 0, sumAvail = 0, sumBook = 0, sumSold = 0;

                foreach (var row in rows)
                {
                    double share = (totalReceipts > 0) ? (row.Receipts / totalReceipts) : 0d;
                    double cogs = Math.Round(companyCogs * share, 2);
                    double opex = Math.Round(companyOpex * share, 2);
                    double gross = Math.Round(row.Receipts - cogs, 2);
                    double net = Math.Round(gross - opex - row.Broker, 2);

                    sr++;
                    var nr = result.NewRow();
                    nr["sr_no"] = sr;
                    nr["row_type"] = "DETAIL";
                    nr["society_id"] = row.SocietyId;
                    nr["society_name"] = row.SocietyName;
                    nr["plots_total"] = row.PlotsTotal;
                    nr["plots_available"] = row.PlotsAvailable;
                    nr["plots_booked"] = row.PlotsBooked;
                    nr["plots_sold"] = row.PlotsSold;
                    nr["alloc_count"] = row.AllocCount;
                    nr["allocated_amount"] = row.Allocated;
                    nr["receipts_amount"] = row.Receipts;
                    nr["cogs_amount"] = cogs;
                    nr["gross_profit"] = gross;
                    nr["opex_amount"] = opex;
                    nr["broker_commission"] = row.Broker;
                    nr["net_profit"] = net;
                    result.Rows.Add(nr);

                    sumAllocCnt += row.AllocCount;
                    sumTot += row.PlotsTotal;
                    sumAvail += row.PlotsAvailable;
                    sumBook += row.PlotsBooked;
                    sumSold += row.PlotsSold;
                    sumAlloc += row.Allocated;
                    sumRec += row.Receipts;
                    sumCogs += cogs;
                    sumOpex += opex;
                    sumBroker += row.Broker;
                    sumGross += gross;
                    sumNet += net;
                }

                // Rounding leftovers for expenses → attach to last detail or CLOSING adjustment
                double cogsLeft = Math.Round(companyCogs - sumCogs, 2);
                double opexLeft = Math.Round(companyOpex - sumOpex, 2);
                if ((Math.Abs(cogsLeft) > 0.009 || Math.Abs(opexLeft) > 0.009) && result.Rows.Count > 0)
                {
                    DataRow target = null;
                    foreach (DataRow r in result.Rows)
                    {
                        if (Convert.ToString(r["row_type"]) == "DETAIL")
                            target = r;
                    }
                    if (target != null && totalReceipts > 0)
                    {
                        target["cogs_amount"] = SafeDouble(target["cogs_amount"]) + cogsLeft;
                        target["opex_amount"] = SafeDouble(target["opex_amount"]) + opexLeft;
                        double rec = SafeDouble(target["receipts_amount"]);
                        double cogs2 = SafeDouble(target["cogs_amount"]);
                        double opex2 = SafeDouble(target["opex_amount"]);
                        double br = SafeDouble(target["broker_commission"]);
                        double gross2 = Math.Round(rec - cogs2, 2);
                        target["gross_profit"] = gross2;
                        target["net_profit"] = Math.Round(gross2 - opex2 - br, 2);
                        sumCogs += cogsLeft;
                        sumOpex += opexLeft;
                        sumGross = 0; sumNet = 0;
                        foreach (DataRow r in result.Rows)
                        {
                            if (Convert.ToString(r["row_type"]) != "DETAIL") continue;
                            sumGross += SafeDouble(r["gross_profit"]);
                            sumNet += SafeDouble(r["net_profit"]);
                        }
                    }
                    else if (totalReceipts <= 0 && (companyCogs > 0 || companyOpex > 0))
                    {
                        var nr = result.NewRow();
                        nr["sr_no"] = ++sr;
                        nr["row_type"] = "DETAIL";
                        nr["society_id"] = 0;
                        nr["society_name"] = "(Shared Expenses — no receipts in period)";
                        nr["plots_total"] = 0;
                        nr["plots_available"] = 0;
                        nr["plots_booked"] = 0;
                        nr["plots_sold"] = 0;
                        nr["alloc_count"] = 0;
                        nr["allocated_amount"] = 0d;
                        nr["receipts_amount"] = 0d;
                        nr["cogs_amount"] = companyCogs;
                        nr["gross_profit"] = Math.Round(0 - companyCogs, 2);
                        nr["opex_amount"] = companyOpex;
                        nr["broker_commission"] = 0d;
                        nr["net_profit"] = Math.Round(0 - companyCogs - companyOpex, 2);
                        result.Rows.Add(nr);
                        sumCogs = companyCogs;
                        sumOpex = companyOpex;
                        sumGross = SafeDouble(nr["gross_profit"]);
                        sumNet = SafeDouble(nr["net_profit"]);
                    }
                }

                // CLOSING totals row
                var close = result.NewRow();
                close["sr_no"] = 0;
                close["row_type"] = "CLOSING";
                close["society_id"] = -1;
                close["society_name"] = "CLOSING TOTAL";
                close["plots_total"] = sumTot;
                close["plots_available"] = sumAvail;
                close["plots_booked"] = sumBook;
                close["plots_sold"] = sumSold;
                close["alloc_count"] = sumAllocCnt;
                close["allocated_amount"] = Math.Round(sumAlloc, 2);
                close["receipts_amount"] = Math.Round(sumRec, 2);
                close["cogs_amount"] = Math.Round(sumCogs, 2);
                close["gross_profit"] = Math.Round(sumGross, 2);
                close["opex_amount"] = Math.Round(sumOpex, 2);
                close["broker_commission"] = Math.Round(sumBroker, 2);
                close["net_profit"] = Math.Round(sumNet, 2);
                result.Rows.Add(close);
            }

            return result;
        }

        private class SocRow
        {
            public int SocietyId;
            public string SocietyName;
            public int PlotsTotal;
            public int PlotsAvailable;
            public int PlotsBooked;
            public int PlotsSold;
            public int AllocCount;
            public double Allocated;
            public double Receipts;
            public double Broker;
        }

        private static DataTable CreateResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("sr_no", typeof(int));
            t.Columns.Add("row_type", typeof(string));
            t.Columns.Add("society_id", typeof(int));
            t.Columns.Add("society_name", typeof(string));
            t.Columns.Add("plots_total", typeof(int));
            t.Columns.Add("plots_available", typeof(int));
            t.Columns.Add("plots_booked", typeof(int));
            t.Columns.Add("plots_sold", typeof(int));
            t.Columns.Add("alloc_count", typeof(int));
            t.Columns.Add("allocated_amount", typeof(double));
            t.Columns.Add("receipts_amount", typeof(double));
            t.Columns.Add("cogs_amount", typeof(double));
            t.Columns.Add("gross_profit", typeof(double));
            t.Columns.Add("opex_amount", typeof(double));
            t.Columns.Add("broker_commission", typeof(double));
            t.Columns.Add("net_profit", typeof(double));
            return t;
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            return 0d;
        }
    }
}
