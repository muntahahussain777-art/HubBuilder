using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace HubBuilders.Report_Form
{
    /// <summary>
    /// Lasbela Market Bhawani Receipt (رسید) — same flow as Lasbela Village Receipt.
    /// New report only; does not change Lasbela Village Receipt.
    /// </summary>
    public partial class frmLasbelaMarketBhawaniReceipt : Sample
    {
        public frmLasbelaMarketBhawaniReceipt()
        {
            InitializeComponent();
            BuildFields();
        }

        private void frmLasbelaMarketBhawaniReceipt_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReceiptDate.Text))
                txtReceiptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            if (string.IsNullOrWhiteSpace(txtPaymentDate.Text))
                txtPaymentDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
        }

        private void BuildFields()
        {
            int y = 4;
            const int labelW = 360;
            const int boxW = 360;
            const int boxH = 28;
            const int gap = 6;

            void AddLabel(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    AutoSize = false,
                    Width = labelW,
                    Height = 22,
                    Left = 4,
                    Top = y,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
                };
                panelFields.Controls.Add(lbl);
                y += 22;
            }

            TextBox AddBox(string name)
            {
                var tb = new TextBox
                {
                    Name = name,
                    Width = boxW,
                    Height = boxH,
                    Left = 4,
                    Top = y,
                    Font = new Font("Segoe UI", 10f)
                };
                panelFields.Controls.Add(tb);
                y += boxH + gap;
                return tb;
            }

            AddLabel("Receipt No / رسید نمبر");
            txtReceiptNo = AddBox("txtReceiptNo");
            AddLabel("Date / تاریخ");
            txtReceiptDate = AddBox("txtReceiptDate");
            AddLabel("Name / محترم محترمہ مسماۃ");
            txtPersonName = AddBox("txtPersonName");
            AddLabel("Father-Husband / والد شوہر");
            txtFatherOrHusband = AddBox("txtFatherOrHusband");
            AddLabel("Received Amount / رقم");
            txtReceivedAmount = AddBox("txtReceivedAmount");
            AddLabel("Amount in Words / رقم الفاظ میں");
            txtAmountInWords = AddBox("txtAmountInWords");
            AddLabel("Payment Mode / کیش چیک ڈرافٹ");
            txtPaymentMode = AddBox("txtPaymentMode");
            AddLabel("Payment Date / بتاریخ");
            txtPaymentDate = AddBox("txtPaymentDate");
            AddLabel("Plot No / پلاٹ نمبر");
            txtPlotNo = AddBox("txtPlotNo");
            AddLabel("Size / سائز");
            txtPlotSize = AddBox("txtPlotSize");
            AddLabel("Block / بلاک");
            txtBlock = AddBox("txtBlock");
            AddLabel("Category / کیٹیگری");
            txtCategory = AddBox("txtCategory");
            AddLabel("Current Amount / نیچے والی رقم");
            txtCurrentAmount = AddBox("txtCurrentAmount");
            AddLabel("Balance / بقایا رقم");
            txtBalanceAmount = AddBox("txtBalanceAmount");

            panelFields.Height = y + 20;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                string reportsDir = Path.Combine(Application.StartupPath, "Reports");
                string templatePath = Path.Combine(reportsDir, "LasbelaMarketBhawaniReceipt.png");
                string reportPath = Path.Combine(reportsDir, "LasbelaMarketBhawaniReceipt.rdlc");

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("Receipt template image not found in Reports folder.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found!", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] imageBytes = ComposeDualPortraitA4(templatePath);

                var dt = new DataTable();
                dt.Columns.Add("ReceiptImage", typeof(byte[]));
                dt.Rows.Add(imageBytes);

                reportViewer1.Reset();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Lasbela Market Bhawani",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            foreach (Control c in panelFields.Controls)
            {
                if (c is TextBox tb) tb.Clear();
            }
            txtReceiptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            txtPaymentDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            reportViewer1.Clear();
        }

        private byte[] ComposeDualPortraitA4(string templatePath)
        {
            using (Bitmap single = ComposeReceiptBitmap(templatePath))
            {
                const int pageW = 1654;
                const int pageH = 2339;
                using (var page = new Bitmap(pageW, pageH, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(page))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    float half = pageH / 2f;
                    float marginX = 28f;
                    float marginY = 16f;
                    float maxW = pageW - 2f * marginX;
                    float maxH = half - 2f * marginY;

                    float scale = Math.Min(maxW / single.Width, maxH / single.Height);
                    float dw = single.Width * scale;
                    float dh = single.Height * scale;
                    float x = (pageW - dw) / 2f;

                    float yTop = marginY + (maxH - dh) / 2f;
                    g.DrawImage(single, x, yTop, dw, dh);

                    float yBot = half + marginY + (maxH - dh) / 2f;
                    g.DrawImage(single, x, yBot, dw, dh);

                    using (var pen = new Pen(Color.FromArgb(170, 170, 180), 1.2f) { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, 40, half, pageW - 40, half);

                    return ToPngBytes(page);
                }
            }
        }

        private Bitmap ComposeReceiptBitmap(string templatePath)
        {
            using (var msIn = new MemoryStream(File.ReadAllBytes(templatePath)))
            using (var src = Image.FromStream(msIn))
            {
                var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(src, 0, 0, src.Width, src.Height);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    float w = bmp.Width;
                    float h = bmp.Height;
                    float fs = Math.Max(18f, h * 0.028f) + 2f;
                    using (Font f = CreateFormFont(fs))
                    {
                        Color ink = Color.FromArgb(15, 20, 90);

                        // Seats calibrated for LasbelaMarketBhawaniReceipt.png underlines.
                        DrawOnLine(g, txtReceiptDate.Text, 0.04f, 0.293f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtReceiptNo.Text, 0.72f, 0.293f, 0.17f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPersonName.Text, 0.34f, 0.368f, 0.24f, w, h, f, ink, true, StringAlignment.Center);
                        DrawOnLine(g, txtFatherOrHusband.Text, 0.44f, 0.438f, 0.22f, w, h, f, ink, true, StringAlignment.Center);
                        DrawOnLine(g, txtAmountInWords.Text, 0.20f, 0.506f, 0.32f, w, h, f, ink, true, StringAlignment.Far);
                        // رقم + پلاٹ نمبر: seat on actual underline, center (slightly right)
                        DrawOnLine(g, txtReceivedAmount.Text, 0.70f, 0.506f, 0.22f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPaymentDate.Text, 0.05f, 0.584f, 0.20f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPaymentMode.Text, 0.34f, 0.584f, 0.28f, w, h, f, ink, true, StringAlignment.Center);
                        DrawOnLine(g, txtCategory.Text, 0.06f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtBlock.Text, 0.30f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPlotSize.Text, 0.50f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtPlotNo.Text, 0.76f, 0.663f, 0.15f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtBalanceAmount.Text, 0.40f, 0.750f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
                        DrawOnLine(g, txtCurrentAmount.Text, 0.74f, 0.750f, 0.12f, w, h, f, ink, false, StringAlignment.Center);
                    }
                }
                return bmp;
            }
        }

        private static Font CreateFormFont(float size)
        {
            string[] preferred =
            {
                "Jameel Noori Nastaleeq",
                "Urdu Typesetting",
                "Microsoft Uighur",
                "Segoe UI Semibold",
                "Segoe UI",
                "Arabic Typesetting"
            };
            foreach (string name in preferred)
            {
                try { return new Font(name, size, FontStyle.Bold, GraphicsUnit.Pixel); }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private static void DrawOnLine(
            Graphics g, string text,
            float xPct, float lineYPct, float wPct,
            float imgW, float imgH, Font font, Color color,
            bool rtl, StringAlignment align)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();
            float lineY = lineYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.032f);
            float top = lineY - boxH - 1f;
            if (top < 0f) top = 0f;
            var rect = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                if (rtl)
                    sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                sf.Alignment = align;
                sf.LineAlignment = StringAlignment.Far;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                Font drawFont = font;
                Font shrunk = null;
                try
                {
                    float size = font.Size;
                    float minSize = Math.Max(11f, font.Size - 6f);
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW && size > minSize)
                    {
                        size -= 0.5f;
                        if (shrunk != null) shrunk.Dispose();
                        shrunk = new Font(font.FontFamily, size, font.Style, font.Unit);
                        drawFont = shrunk;
                        measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    }
                    g.DrawString(t, drawFont, brush, rect, sf);
                }
                finally
                {
                    if (shrunk != null) shrunk.Dispose();
                }
            }
        }

        private static byte[] ToPngBytes(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}
