using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// NEW report only: Complete Dues Tracker (Mode A — gap months + remaining dues).
    /// WhatsApp: PDF + message to client. Does not change other forms/DB/UI.
    /// </summary>
    public partial class frmVipCompleteDuesTracker : Sample
    {
        private DataTable _dtReport;
        private bool _busy;

        public frmVipCompleteDuesTracker()
        {
            InitializeComponent();
        }

        private void frmVipCompleteDuesTracker_Load(object sender, EventArgs e)
        {
            try
            {
                dtpAsOf.Value = DateTime.Today;
                LoadFilterSocieties();
                LoadHeaderSocieties();
                StyleVipGrid(dgvList);
                dgvList.DataBindingComplete -= DgvList_DataBindingComplete;
                dgvList.DataBindingComplete += DgvList_DataBindingComplete;
                dgvList.CellFormatting -= DgvList_CellFormatting;
                dgvList.CellFormatting += DgvList_CellFormatting;
                this.Text = "Complete Dues Tracker";
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
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Rows.Add(0, "(Hub Builders — default)");

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

                cmbHeaderSociety.DisplayMember = "name";
                cmbHeaderSociety.ValueMember = "id";
                cmbHeaderSociety.DataSource = dt;
                cmbHeaderSociety.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void LoadFilterSocieties()
        {
            try
            {
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Rows.Add(0, "(All Societies)");

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

                cmbFilterSociety.DisplayMember = "name";
                cmbFilterSociety.ValueMember = "id";
                cmbFilterSociety.DataSource = dt;
                cmbFilterSociety.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private string GetHeaderBrand()
        {
            try
            {
                if (cmbHeaderSociety == null || cmbHeaderSociety.SelectedValue == null)
                    return "HUB BUILDERS";
                int id = Convert.ToInt32(cmbHeaderSociety.SelectedValue);
                if (id <= 0) return "HUB BUILDERS";
                string name = Convert.ToString(cmbHeaderSociety.Text);
                return string.IsNullOrWhiteSpace(name) ? "HUB BUILDERS" : name.Trim().ToUpperInvariant();
            }
            catch
            {
                return "HUB BUILDERS";
            }
        }

        private int GetFilterSocietyId()
        {
            try
            {
                if (cmbFilterSociety?.SelectedValue == null || cmbFilterSociety.SelectedValue == DBNull.Value)
                    return 0;
                return Convert.ToInt32(cmbFilterSociety.SelectedValue);
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
                _dtReport = BuildDuesData();
                if (_dtReport == null || _dtReport.Rows.Count == 0)
                {
                    MessageBox.Show("No dues / allocation records found.", "Complete Dues Tracker",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvList.DataSource = null;
                    reportViewer1.Clear();
                    return;
                }

                BindListGrid(_dtReport);
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
                if (cmbFilterSociety != null && cmbFilterSociety.Items.Count > 0)
                    cmbFilterSociety.SelectedIndex = 0;
                if (cmbHeaderSociety != null && cmbHeaderSociety.Items.Count > 0)
                    cmbHeaderSociety.SelectedIndex = 0;
                if (txtNameSearch != null) txtNameSearch.Clear();
                dtpAsOf.Value = DateTime.Today;
                _dtReport = null;
                dgvList.DataSource = null;
                reportViewer1.Clear();
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private void btnWhatsApp_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                DataRow row = GetSelectedListRow();
                if (row == null)
                {
                    MessageBox.Show("Pehle list se customer select karein, phir WhatsApp dabayein.",
                        "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string mobile = Convert.ToString(row["mobile"]);
                string digits = NormalizePhoneToDigits(mobile);
                if (string.IsNullOrWhiteSpace(digits))
                {
                    MessageBox.Show("Customer ka mobile number nahi mila.", "WhatsApp",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Single-customer PDF
                var one = _dtReport.Clone();
                one.ImportRow(row);
                string pdfPath = ExportPdf(one, Convert.ToString(row["customer_name"]));
                if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                {
                    MessageBox.Show("PDF ban nahi saki.", "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string msg = BuildWhatsAppMessage(row, pdfPath);
                OpenWhatsAppWithPrefill(digits, msg);

                // Open PDF + folder so client attach easy (WhatsApp URL cannot attach file itself)
                try { Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true }); } catch { }
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + pdfPath + "\"",
                        UseShellExecute = true
                    });
                }
                catch { }

                MessageBox.Show(
                    "WhatsApp open ho gaya.\nPDF bhi ready hai — WhatsApp mein paperclip se yeh PDF attach kar dein:\n\n" + pdfPath,
                    "WhatsApp + PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
            finally
            {
                _busy = false;
            }
        }

        private DataRow GetSelectedListRow()
        {
            try
            {
                if (dgvList.CurrentRow == null || dgvList.CurrentRow.IsNewRow) return null;
                var drv = dgvList.CurrentRow.DataBoundItem as DataRowView;
                return drv?.Row;
            }
            catch { return null; }
        }

        private void BindListGrid(DataTable dt)
        {
            try
            {
                StyleVipGrid(dgvList);
                dgvList.AutoGenerateColumns = true;
                dgvList.DataSource = dt.Copy();
                StyleVipGrid(dgvList);
                ForceWhiteColumns(dgvList);
                // Hide bulky / internal cols if present
                HideCol("cnic");
                HideCol("customer_plot_id");
                if (dgvList.Columns.Contains("missed_months"))
                    dgvList.Columns["missed_months"].Width = 160;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
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
            string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipCompleteDuesTracker.rdlc");
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
                reportViewer1.LocalReport.SetParameters(new ReportParameter("AsOfDate", dtpAsOf.Value.ToString("dd-MMM-yyyy")));
            }
            catch { }
            reportViewer1.RefreshReport();
        }

        private DataTable BuildDuesData()
        {
            var result = CreateResultTable();
            int societyId = GetFilterSocietyId();
            string kw = (txtNameSearch?.Text ?? "").Trim();
            DateTime asOf = dtpAsOf.Value.Date;

            string sql = @"
SELECT
  cp.id AS customer_plot_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(c.cnic,'') AS cnic,
  COALESCE(s.name,'') AS society_name,
  COALESCE(b.name,'') AS block_name,
  COALESCE(p.plot_no,'') AS plot_no,
  cp.booking_date AS booking_date,
  COALESCE(cp.price, 0) AS plot_price,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id), 0) AS total_paid,
  COALESCE((SELECT MAX(receipt_date) FROM receipts r2 WHERE r2.customer_plot_id = cp.id), '') AS last_payment_date
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
WHERE cp.status = 'Active'
  AND (@sid = 0 OR p.society_id = @sid)
  AND (@kw = '' OR c.name LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR c.mobile LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR c.cnic LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR CAST(p.plot_no AS TEXT) LIKE '%' || @kw || '%' COLLATE NOCASE
                 OR s.name LIKE '%' || @kw || '%' COLLATE NOCASE)
ORDER BY s.name, c.name, p.plot_no;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@sid", societyId);
                    cmd.Parameters.AddWithValue("@kw", kw);
                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        var raw = new DataTable();
                        da.Fill(raw);

                        int sr = 0;
                        foreach (DataRow r in raw.Rows)
                        {
                            sr++;
                            int cpId = Convert.ToInt32(r["customer_plot_id"]);
                            double price = SafeDouble(r["plot_price"]);
                            double paid = SafeDouble(r["total_paid"]);
                            double due = Math.Round(price - paid, 2);
                            if (due < 0) due = 0;

                            DateTime booking = ParseDateSafe(r["booking_date"], asOf);
                            var paidMonths = LoadPaidMonths(con, cpId);
                            string missed;
                            int missedCount;
                            ComputeMissedMonths(booking, asOf, paidMonths, out missed, out missedCount);

                            var nr = result.NewRow();
                            nr["sr_no"] = sr;
                            nr["customer_plot_id"] = cpId;
                            nr["customer_name"] = Convert.ToString(r["customer_name"]);
                            nr["mobile"] = Convert.ToString(r["mobile"]);
                            nr["cnic"] = Convert.ToString(r["cnic"]);
                            nr["society_name"] = Convert.ToString(r["society_name"]);
                            nr["block_name"] = Convert.ToString(r["block_name"]);
                            nr["plot_no"] = Convert.ToString(r["plot_no"]);
                            nr["booking_date"] = booking.ToString("yyyy-MM-dd");
                            nr["plot_price"] = price;
                            nr["total_paid"] = paid;
                            nr["remaining_due"] = due;
                            nr["missed_months"] = missed;
                            nr["missed_count"] = missedCount;
                            nr["last_payment_date"] = Convert.ToString(r["last_payment_date"]);
                            result.Rows.Add(nr);
                        }
                    }
                }
            }

            return result;
        }

        private static HashSet<string> LoadPaidMonths(SQLiteConnection con, int customerPlotId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var cmd = new SQLiteCommand(@"
SELECT DISTINCT strftime('%Y-%m', receipt_date) AS ym
FROM receipts
WHERE customer_plot_id = @id
  AND amount > 0
  AND receipt_date IS NOT NULL
  AND TRIM(receipt_date) <> '';", con))
                {
                    cmd.Parameters.AddWithValue("@id", customerPlotId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string ym = Convert.ToString(r["ym"]);
                            if (!string.IsNullOrWhiteSpace(ym))
                                set.Add(ym);
                        }
                    }
                }
            }
            catch { }
            return set;
        }

        /// <summary>
        /// Mode A: months AFTER booking month through AsOf where no receipt — missed.
        /// </summary>
        private static void ComputeMissedMonths(
            DateTime booking, DateTime asOf, HashSet<string> paidMonths,
            out string missedCsv, out int missedCount)
        {
            var missed = new List<string>();
            try
            {
                // Start = first day of month AFTER booking (Aug allocate → track from Sep)
                DateTime cursor = new DateTime(booking.Year, booking.Month, 1).AddMonths(1);
                DateTime end = new DateTime(asOf.Year, asOf.Month, 1);
                if (cursor > end)
                {
                    missedCsv = "—";
                    missedCount = 0;
                    return;
                }

                while (cursor <= end)
                {
                    string key = cursor.ToString("yyyy-MM");
                    if (paidMonths == null || !paidMonths.Contains(key))
                        missed.Add(cursor.ToString("MMM-yyyy", CultureInfo.InvariantCulture));
                    cursor = cursor.AddMonths(1);
                }
            }
            catch { }

            missedCount = missed.Count;
            missedCsv = missedCount == 0 ? "None" : string.Join(", ", missed);
        }

        private static DataTable CreateResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("sr_no", typeof(int));
            t.Columns.Add("customer_plot_id", typeof(int));
            t.Columns.Add("customer_name", typeof(string));
            t.Columns.Add("mobile", typeof(string));
            t.Columns.Add("cnic", typeof(string));
            t.Columns.Add("society_name", typeof(string));
            t.Columns.Add("block_name", typeof(string));
            t.Columns.Add("plot_no", typeof(string));
            t.Columns.Add("booking_date", typeof(string));
            t.Columns.Add("plot_price", typeof(double));
            t.Columns.Add("total_paid", typeof(double));
            t.Columns.Add("remaining_due", typeof(double));
            t.Columns.Add("missed_months", typeof(string));
            t.Columns.Add("missed_count", typeof(int));
            t.Columns.Add("last_payment_date", typeof(string));
            return t;
        }

        private string ExportPdf(DataTable dt, string customerName)
        {
            try
            {
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "VipCompleteDuesTracker.rdlc");
                if (!File.Exists(reportPath)) return null;

                var local = new LocalReport();
                local.ReportPath = reportPath;
                local.DataSources.Clear();
                local.DataSources.Add(new ReportDataSource("DataSet1", dt));
                try
                {
                    local.SetParameters(new ReportParameter("HeaderBrand", GetHeaderBrand()));
                    local.SetParameters(new ReportParameter("AsOfDate", dtpAsOf.Value.ToString("dd-MMM-yyyy")));
                }
                catch { }

                string mime, enc, ext;
                Warning[] warnings;
                string[] streams;
                byte[] bytes = local.Render("PDF", null, out mime, out enc, out ext, out streams, out warnings);

                string folder = Path.Combine(Path.GetTempPath(), "HubBuilders");
                Directory.CreateDirectory(folder);
                string safe = MakeFileSafe(customerName);
                string path = Path.Combine(folder, "Dues_" + safe + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
                return null;
            }
        }

        private static string MakeFileSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Customer";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim();
            if (name.Length > 40) name = name.Substring(0, 40);
            return string.IsNullOrWhiteSpace(name) ? "Customer" : name;
        }

        private static string BuildWhatsAppMessage(DataRow row, string pdfPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum,");
            sb.AppendLine();
            sb.AppendLine("*" + Convert.ToString(row["customer_name"]) + "* — Dues Summary");
            sb.AppendLine("Society: " + Convert.ToString(row["society_name"]));
            sb.AppendLine("Block/Plot: " + Convert.ToString(row["block_name"]) + " / " + Convert.ToString(row["plot_no"]));
            sb.AppendLine("Booking: " + Convert.ToString(row["booking_date"]));
            sb.AppendLine("Plot Price: " + FormatMoney(row["plot_price"]));
            sb.AppendLine("Total Paid: " + FormatMoney(row["total_paid"]));
            sb.AppendLine("*Remaining Due: " + FormatMoney(row["remaining_due"]) + "*");
            sb.AppendLine("Missed Months (" + Convert.ToString(row["missed_count"]) + "): " + Convert.ToString(row["missed_months"]));
            string last = Convert.ToString(row["last_payment_date"]);
            if (!string.IsNullOrWhiteSpace(last))
                sb.AppendLine("Last Payment: " + last);
            sb.AppendLine();
            sb.AppendLine("PDF ready — please attach from:");
            sb.AppendLine(pdfPath);
            sb.AppendLine();
            sb.AppendLine("Hub Builders");
            return sb.ToString();
        }

        private static string FormatMoney(object o)
        {
            double v = SafeDouble(o);
            return v.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string NormalizePhoneToDigits(string input)
        {
            try
            {
                var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
                if (digits.StartsWith("03") && digits.Length == 11)
                    digits = "92" + digits.Substring(1);
                return digits;
            }
            catch { return ""; }
        }

        private static void OpenWhatsAppWithPrefill(string digitsE164, string message)
        {
            try
            {
                string encoded = Uri.EscapeDataString(message ?? "");
                if (!TryOpenUrl("whatsapp://send?phone=" + digitsE164 + "&text=" + encoded))
                    TryOpenUrl("https://wa.me/" + digitsE164 + "?text=" + encoded);
            }
            catch { }
        }

        private static bool TryOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(Convert.ToString(o), NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v;
            return 0d;
        }

        private static DateTime ParseDateSafe(object o, DateTime fallback)
        {
            try
            {
                if (o == null || o == DBNull.Value) return fallback;
                DateTime d;
                if (DateTime.TryParse(Convert.ToString(o), out d)) return d.Date;
                if (DateTime.TryParseExact(Convert.ToString(o), new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d.Date;
            }
            catch { }
            return fallback;
        }
    }
}
