using HubBuilders.ProjectConnection;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// VIP A4 Customer Receipt Statement — new report only.
    /// Pulls the same allocation ledger as Receipt Form (Opening Debit, Credits, Balance)
    /// plus Society / Block / Plot / Ref No. Does not modify other reports or Receipt Form UI.
    /// </summary>
    public partial class frmVipCustomerReceiptStatement : Sample
    {
        private static readonly Color Navy = Color.FromArgb(10, 35, 95);
        private static readonly Color NavyDeep = Color.FromArgb(6, 22, 64);
        private static readonly Color Gold = Color.FromArgb(184, 148, 68);
        private static readonly Color Ivory = Color.FromArgb(248, 246, 240);
        private static readonly Color SoftLine = Color.FromArgb(210, 215, 228);
        private static readonly Color Ink = Color.FromArgb(0, 0, 0);

        public frmVipCustomerReceiptStatement()
        {
            InitializeComponent();
        }

        private void frmVipCustomerReceiptStatement_Load(object sender, EventArgs e)
        {
            dtpFrom.Value = DateTime.Today.AddYears(-5);
            dtpTo.Value = DateTime.Today;
            chkDateFilter.Checked = false;
            LoadHeaderSocieties();
            LoadCustomerPlots();
        }

        private void LoadHeaderSocieties()
        {
            try
            {
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                // id=0 means default Hub Builders (no society selected)
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

        private string GetSelectedHeaderBrand()
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

        private void btnFilter_Click(object sender, EventArgs e)
        {
            LoadCustomerPlots();
        }

        private void LoadCustomerPlots()
        {
            try
            {
                string keyword = (txtNameSearch != null ? txtNameSearch.Text : "").Trim();
                string q = @"
SELECT 
  cp.id AS id,
  c.name || ' | Plot ' || p.plot_no || ' | ' || COALESCE(s.name,'') || ' / ' || COALESCE(b.name,'') AS disp
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p     ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b    ON b.id = p.block_id
WHERE cp.status = 'Active'
  AND (@kw = '' OR c.name LIKE '%' || @kw || '%' COLLATE NOCASE)
ORDER BY c.name, p.plot_no;";

                using (var con = new SQLiteConnection(projectconnection.conReturn()))
                using (var cmd = new SQLiteCommand(q, con))
                {
                    cmd.Parameters.AddWithValue("@kw", keyword);
                    con.Open();
                    var dt = new DataTable();
                    using (var da = new SQLiteDataAdapter(cmd))
                        da.Fill(dt);

                    cmbCustomerPlot.DisplayMember = "disp";
                    cmbCustomerPlot.ValueMember = "id";
                    cmbCustomerPlot.DataSource = dt;
                    if (dt.Rows.Count > 0) cmbCustomerPlot.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                HubBuilders.AppSafety.ShowSoft(ex);
            }
        }

        private int? CurrentCustomerPlotId
        {
            get
            {
                var v = cmbCustomerPlot?.SelectedValue;
                if (v == null || v == DBNull.Value) return null;
                try { return Convert.ToInt32(v); } catch { return null; }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (txtNameSearch != null) txtNameSearch.Clear();
            if (chkDateFilter != null) chkDateFilter.Checked = false;
            if (dtpFrom != null) dtpFrom.Value = DateTime.Today.AddYears(-5);
            if (dtpTo != null) dtpTo.Value = DateTime.Today;
            if (cmbHeaderSociety != null && cmbHeaderSociety.Items.Count > 0)
                cmbHeaderSociety.SelectedIndex = 0;
            LoadCustomerPlots();
            reportViewer1.Clear();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                var cpId = CurrentCustomerPlotId;
                if (cpId == null)
                {
                    MessageBox.Show("Please select a customer allocation.", "Customer Receipt Statement",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string reportsDir = Path.Combine(Application.StartupPath, "Reports");
                string reportPath = Path.Combine(reportsDir, "VipCustomerReceiptStatement.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found!", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var header = LoadHeader(cpId.Value);
                if (header == null)
                {
                    MessageBox.Show("Allocation details not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DateTime? from = null, to = null;
                if (chkDateFilter != null && chkDateFilter.Checked)
                {
                    from = dtpFrom.Value.Date;
                    to = dtpTo.Value.Date;
                    if (from > to)
                    {
                        MessageBox.Show("From date cannot be after To date.", "Customer Receipt Statement",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                var ledger = BuildLedger(cpId.Value, from, to);
                string headerBrand = GetSelectedHeaderBrand();
                byte[] png = ComposeVipA4(header, ledger, from, to, headerBrand);

                var dt = new DataTable();
                dt.Columns.Add("ReportImage", typeof(byte[]));
                dt.Rows.Add(png);

                reportViewer1.Reset();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Customer Receipt Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class HeaderInfo
        {
            public string CustomerName;
            public string FatherName;
            public string Mobile;
            public string Cnic;
            public string Society;
            public string Block;
            public string PlotNo;
            public string PlotType;
            public double Price;
        }

        private HeaderInfo LoadHeader(int customerPlotId)
        {
            const string q = @"
SELECT 
  c.name AS customer_name,
  COALESCE(c.father_name,'') AS father_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(c.cnic,'') AS cnic,
  COALESCE(s.name,'') AS society,
  COALESCE(b.name,'') AS block,
  COALESCE(p.plot_no,'') AS plot_no,
  COALESCE(pt.code,'') AS plot_type,
  COALESCE(cp.price,0) AS price
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.id = @cpid;";

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            using (var cmd = new SQLiteCommand(q, con))
            {
                cmd.Parameters.AddWithValue("@cpid", customerPlotId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new HeaderInfo
                    {
                        CustomerName = Convert.ToString(r["customer_name"]),
                        FatherName = Convert.ToString(r["father_name"]),
                        Mobile = Convert.ToString(r["mobile"]),
                        Cnic = Convert.ToString(r["cnic"]),
                        Society = Convert.ToString(r["society"]),
                        Block = Convert.ToString(r["block"]),
                        PlotNo = Convert.ToString(r["plot_no"]),
                        PlotType = Convert.ToString(r["plot_type"]),
                        Price = SafeDouble(r["price"])
                    };
                }
            }
        }

        private DataTable BuildLedger(int customerPlotId, DateTime? fromDate, DateTime? toDate)
        {
            // Same logic as Receipt Form: Opening Debit + Receipts (Credit) + running Balance
            var t = new DataTable();
            t.Columns.Add("RowType", typeof(string));
            t.Columns.Add("Date", typeof(string));
            t.Columns.Add("Description", typeof(string));
            t.Columns.Add("Debit", typeof(double));
            t.Columns.Add("Credit", typeof(double));
            t.Columns.Add("Balance", typeof(double));
            t.Columns.Add("Mode", typeof(string));
            t.Columns.Add("RefNo", typeof(string));

            using (var con = new SQLiteConnection(projectconnection.conReturn()))
            {
                con.Open();

                double plotPrice = 0d;
                using (var cmd = new SQLiteCommand("SELECT COALESCE(price,0) FROM customer_plots WHERE id=@cpid;", con))
                {
                    cmd.Parameters.AddWithValue("@cpid", customerPlotId);
                    plotPrice = SafeDouble(cmd.ExecuteScalar());
                }

                double tDebit = 0d;
                using (var cmd = new SQLiteCommand(@"
SELECT COALESCE(SUM(debit),0) FROM ledger WHERE customer_plot_id=@cpid;", con))
                {
                    cmd.Parameters.AddWithValue("@cpid", customerPlotId);
                    tDebit = SafeDouble(cmd.ExecuteScalar());
                }

                double opening = tDebit > 0 ? tDebit : plotPrice;
                double running = opening;

                var rOpen = t.NewRow();
                rOpen["RowType"] = "OPENING";
                rOpen["Date"] = "";
                rOpen["Description"] = "Opening (Debit)";
                rOpen["Debit"] = opening;
                rOpen["Credit"] = 0d;
                rOpen["Balance"] = running;
                rOpen["Mode"] = "";
                rOpen["RefNo"] = "";
                t.Rows.Add(rOpen);

                double totalCredit = 0d;
                string qRcpt = @"
SELECT id, receipt_date, amount, mode, COALESCE(ref_no,'') AS ref_no, COALESCE(notes,'') AS notes
FROM receipts
WHERE customer_plot_id=@cpid
  AND (@useDate = 0 OR (date(receipt_date) >= date(@from) AND date(receipt_date) <= date(@to)))
ORDER BY date(receipt_date) ASC, id ASC;";
                using (var cmd = new SQLiteCommand(qRcpt, con))
                {
                    cmd.Parameters.AddWithValue("@cpid", customerPlotId);
                    cmd.Parameters.AddWithValue("@useDate", fromDate.HasValue && toDate.HasValue ? 1 : 0);
                    cmd.Parameters.AddWithValue("@from", fromDate.HasValue ? fromDate.Value.ToString("yyyy-MM-dd") : "");
                    cmd.Parameters.AddWithValue("@to", toDate.HasValue ? toDate.Value.ToString("yyyy-MM-dd") : "");
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            double amt = SafeDouble(r["amount"]);
                            totalCredit += amt;
                            running -= amt;

                            var rw = t.NewRow();
                            rw["RowType"] = "RECEIPT";
                            rw["Date"] = r["receipt_date"] == DBNull.Value
                                ? ""
                                : Convert.ToDateTime(r["receipt_date"]).ToString("dd-MM-yyyy");
                            string notes = Convert.ToString(r["notes"]);
                            rw["Description"] = string.IsNullOrWhiteSpace(notes) ? "Receipt" : notes.Trim();
                            rw["Debit"] = 0d;
                            rw["Credit"] = amt;
                            rw["Balance"] = running;
                            rw["Mode"] = Convert.ToString(r["mode"]);
                            rw["RefNo"] = Convert.ToString(r["ref_no"]);
                            t.Rows.Add(rw);
                        }
                    }
                }

                var rTot = t.NewRow();
                rTot["RowType"] = "TOTAL";
                rTot["Date"] = "";
                rTot["Description"] = "Total Credit";
                rTot["Debit"] = 0d;
                rTot["Credit"] = totalCredit;
                rTot["Balance"] = DBNull.Value;
                rTot["Mode"] = "";
                rTot["RefNo"] = "";
                t.Rows.Add(rTot);

                var rClose = t.NewRow();
                rClose["RowType"] = "CLOSING";
                rClose["Date"] = "";
                rClose["Description"] = "Closing Balance";
                rClose["Debit"] = opening;
                rClose["Credit"] = totalCredit;
                rClose["Balance"] = opening - totalCredit;
                rClose["Mode"] = "";
                rClose["RefNo"] = "";
                t.Rows.Add(rClose);
            }

            return t;
        }

        private byte[] ComposeVipA4(HeaderInfo h, DataTable ledger, DateTime? fromDate, DateTime? toDate, string headerBrand)
        {
            // A4 @ 200 DPI
            const int W = 1654;
            const int H = 2339;
            float m = 70f;
            if (string.IsNullOrWhiteSpace(headerBrand)) headerBrand = "HUB BUILDERS";

            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Ivory);

                using (var pen = new Pen(Navy, 4f))
                    g.DrawRectangle(pen, 28, 28, W - 56, H - 56);
                using (var pen = new Pen(Gold, 2f))
                    g.DrawRectangle(pen, 38, 38, W - 76, H - 76);

                using (var br = new LinearGradientBrush(new Rectangle(50, 50, W - 100, 150), NavyDeep, Navy, LinearGradientMode.Horizontal))
                    g.FillRectangle(br, 50, 50, W - 100, 150);
                using (var br = new SolidBrush(Gold))
                    g.FillRectangle(br, 50, 196, W - 100, 6);

                using (var fBrand = new Font("Segoe UI", 36f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var fSub = new Font("Segoe UI", 22f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var fMeta = new Font("Segoe UI Semibold", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var white = new SolidBrush(Color.White))
                using (var goldBr = new SolidBrush(Gold))
                {
                    // Society selected → society name; otherwise Hub Builders
                    g.DrawString(headerBrand, fBrand, white, new PointF(70, 68));
                    g.DrawString("Customer Receipt Statement", fSub, goldBr, new PointF(72, 118));
                    string period = (fromDate.HasValue && toDate.HasValue)
                        ? ("Period: " + fromDate.Value.ToString("dd-MM-yyyy") + "  to  " + toDate.Value.ToString("dd-MM-yyyy"))
                        : ("Printed: " + DateTime.Now.ToString("dd MMM yyyy  HH:mm"));
                    g.DrawString(period, fMeta, white,
                        new RectangleF(W - 520, 128, 430, 30),
                        new StringFormat { Alignment = StringAlignment.Far });
                }

                float y = 230f;

                DrawSectionTitle(g, "CUSTOMER & PLOT DETAILS", m, ref y, W);
                y += 8;
                float panelTop = y;
                float panelH = 250f;
                using (var br = new SolidBrush(Color.White))
                    g.FillRectangle(br, m, panelTop, W - 2 * m, panelH);
                using (var pen = new Pen(SoftLine, 1.5f))
                    g.DrawRectangle(pen, m, panelTop, W - 2 * m, panelH);

                float col1 = m + 24;
                float col2 = m + 820;
                float rowY = panelTop + 18;
                float rowGap = 44f;

                DrawInfoPair(g, "Customer Name", h.CustomerName, col1, rowY);
                DrawInfoPair(g, "Father Name", h.FatherName, col2, rowY);
                rowY += rowGap;
                DrawInfoPair(g, "Society", h.Society, col1, rowY);
                DrawInfoPair(g, "Block", h.Block, col2, rowY);
                rowY += rowGap;
                DrawInfoPair(g, "Plot No", h.PlotNo, col1, rowY);
                DrawInfoPair(g, "Category", h.PlotType, col2, rowY);
                rowY += rowGap;
                DrawInfoPair(g, "Mobile", h.Mobile, col1, rowY);
                DrawInfoPair(g, "CNIC", h.Cnic, col2, rowY);
                rowY += rowGap;
                DrawInfoPair(g, "Plot Price", h.Price.ToString("N2"), col1, rowY);

                y = panelTop + panelH + 28;

                double opening = 0, credit = 0, closing = 0;
                foreach (DataRow r in ledger.Rows)
                {
                    string rt = Convert.ToString(r["RowType"]);
                    if (rt == "OPENING") opening = SafeDouble(r["Debit"]);
                    if (rt == "TOTAL") credit = SafeDouble(r["Credit"]);
                    if (rt == "CLOSING") closing = SafeDouble(r["Balance"]);
                }

                float cardW = (W - 2 * m - 40) / 3f;
                float cardH = 100f;
                DrawSummaryCard(g, m, y, cardW, cardH, "OPENING DEBIT", opening.ToString("N2"), Color.FromArgb(180, 55, 55));
                DrawSummaryCard(g, m + cardW + 20, y, cardW, cardH, "TOTAL CREDIT", credit.ToString("N2"), Color.FromArgb(20, 120, 80));
                DrawSummaryCard(g, m + 2 * (cardW + 20), y, cardW, cardH, "CLOSING BALANCE", closing.ToString("N2"), Navy);
                y += cardH + 30;

                DrawSectionTitle(g, "RECEIPT LEDGER  ·  DEBIT / CREDIT / BALANCE", m, ref y, W);
                y += 10;

                float usable = W - 2 * m;
                float[] cols = {
                    usable * 0.05f,
                    usable * 0.12f,
                    usable * 0.24f,
                    usable * 0.09f,
                    usable * 0.12f,
                    usable * 0.12f,
                    usable * 0.12f,
                    usable * 0.14f
                };

                float rowH = 40f;
                float tableTop = y;
                DrawTableHeader(g, m, tableTop, cols, rowH);
                y = tableTop + rowH;

                int sr = 0;
                using (var fCell = new Font("Segoe UI Semibold", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var fBold = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var ink = new SolidBrush(Color.Black))
                using (var white = new SolidBrush(Color.White))
                using (var alt = new SolidBrush(Color.FromArgb(236, 240, 248)))
                using (var sumBr = new SolidBrush(Color.FromArgb(18, 40, 90)))
                {
                    foreach (DataRow r in ledger.Rows)
                    {
                        if (y + rowH > H - 120) break;
                        sr++;
                        string rt = Convert.ToString(r["RowType"]);
                        bool summary = rt == "OPENING" || rt == "TOTAL" || rt == "CLOSING";

                        if (summary)
                            g.FillRectangle(sumBr, m, y, usable, rowH);
                        else if (sr % 2 == 0)
                            g.FillRectangle(alt, m, y, usable, rowH);

                        Brush textBr = summary ? white : ink;
                        Font ff = summary ? fBold : fCell;

                        string debit = SafeDouble(r["Debit"]) == 0 && rt == "RECEIPT" ? "" : FormatAmt(r["Debit"]);
                        string creditS = SafeDouble(r["Credit"]) == 0 && (rt == "OPENING") ? "" : FormatAmt(r["Credit"]);
                        if (rt == "TOTAL") debit = "";
                        string bal = r["Balance"] == DBNull.Value ? "" : FormatAmt(r["Balance"]);

                        float x = m;
                        DrawCell(g, sr.ToString(), x, y, cols[0], rowH, ff, textBr, StringAlignment.Center); x += cols[0];
                        DrawCell(g, Convert.ToString(r["Date"]), x, y, cols[1], rowH, ff, textBr, StringAlignment.Center); x += cols[1];
                        DrawCell(g, Convert.ToString(r["Description"]), x, y, cols[2], rowH, ff, textBr, StringAlignment.Near); x += cols[2];
                        DrawCell(g, Convert.ToString(r["Mode"]), x, y, cols[3], rowH, ff, textBr, StringAlignment.Center); x += cols[3];
                        DrawCell(g, Convert.ToString(r["RefNo"]), x, y, cols[4], rowH, ff, textBr, StringAlignment.Center); x += cols[4];
                        DrawCell(g, debit, x, y, cols[5], rowH, ff, textBr, StringAlignment.Far); x += cols[5];
                        DrawCell(g, creditS, x, y, cols[6], rowH, ff, textBr, StringAlignment.Far); x += cols[6];
                        DrawCell(g, bal, x, y, cols[7], rowH, ff, textBr, StringAlignment.Far);

                        using (var pen = new Pen(SoftLine, 1f))
                            g.DrawLine(pen, m, y + rowH, m + usable, y + rowH);

                        y += rowH;
                    }
                }

                using (var br = new SolidBrush(Navy))
                    g.FillRectangle(br, 50, H - 95, W - 100, 52);
                using (var f = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var white = new SolidBrush(Color.White))
                {
                    g.DrawString("Hub Builders", f, white,
                        new RectangleF(70, H - 85, W - 160, 36),
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }

                using (var ms = new MemoryStream())
                {
                    bmp.SetResolution(200f, 200f);
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        private static void DrawSectionTitle(Graphics g, string title, float m, ref float y, int W)
        {
            using (var f = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var br = new SolidBrush(Color.Black))
            using (var gold = new SolidBrush(Gold))
            {
                g.DrawString(title, f, br, m, y);
                y += 28;
                g.FillRectangle(gold, m, y, 140, 4);
                y += 8;
            }
        }

        private static void DrawInfoPair(Graphics g, string label, string value, float x, float y)
        {
            using (var fL = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var fV = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var black = new SolidBrush(Color.Black))
            {
                g.DrawString(label, fL, black, x, y);
                g.DrawString(string.IsNullOrWhiteSpace(value) ? "—" : value, fV, black, x, y + 18);
            }
        }

        private static void DrawSummaryCard(Graphics g, float x, float y, float w, float h, string title, string value, Color accent)
        {
            using (var br = new SolidBrush(Color.White))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(SoftLine, 1.5f))
                g.DrawRectangle(pen, x, y, w, h);
            using (var br = new SolidBrush(accent))
                g.FillRectangle(br, x, y, 8, h);
            using (var fT = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var fV = new Font("Segoe UI", 28f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var black = new SolidBrush(Color.Black))
            {
                g.DrawString(title, fT, black, x + 22, y + 16);
                g.DrawString(value, fV, black, x + 22, y + 42);
            }
        }

        private static void DrawTableHeader(Graphics g, float m, float y, float[] cols, float rowH)
        {
            float usable = 0;
            foreach (var c in cols) usable += c;
            using (var br = new SolidBrush(Navy))
                g.FillRectangle(br, m, y, usable, rowH);
            using (var f = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var white = new SolidBrush(Color.White))
            {
                string[] heads = { "Sr#", "Date", "Description", "Mode", "Ref No", "Debit", "Credit", "Balance" };
                float x = m;
                for (int i = 0; i < heads.Length; i++)
                {
                    var align = (i >= 5) ? StringAlignment.Far : (i == 2 ? StringAlignment.Near : StringAlignment.Center);
                    DrawCell(g, heads[i], x, y, cols[i], rowH, f, white, align);
                    x += cols[i];
                }
            }
        }

        private static void DrawCell(Graphics g, string text, float x, float y, float w, float h, Font font, Brush brush, StringAlignment align)
        {
            using (var sf = new StringFormat())
            {
                sf.Alignment = align;
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(text ?? "", font, brush, new RectangleF(x + 6, y, w - 12, h), sf);
            }
        }

        private static string FormatAmt(object o)
        {
            if (o == null || o == DBNull.Value) return "";
            return SafeDouble(o).ToString("N2");
        }

        private static double SafeDouble(object o)
        {
            if (o == null || o == DBNull.Value) return 0d;
            double v; return double.TryParse(o.ToString(), out v) ? v : 0d;
        }
    }
}
