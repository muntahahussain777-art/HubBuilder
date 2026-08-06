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
    /// Overlay print for pre-printed blank Application Forms (1k stock).
    /// Same field seats/scale as filled Application Form — white page, text only (no design reprint).
    /// Does not change the filled Application Form report.
    /// </summary>
    public partial class frmApplicationFormBlank : Sample
    {
        public frmApplicationFormBlank()
        {
            InitializeComponent();
            BuildFields();
        }

        private void frmApplicationFormBlank_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDate.Text))
                txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            if (string.IsNullOrWhiteSpace(txtAcceptDate.Text))
                txtAcceptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            if (string.IsNullOrWhiteSpace(txtDay.Text))
                txtDay.Text = DateTime.Now.Day.ToString();
            if (string.IsNullOrWhiteSpace(txtMonth.Text))
                txtMonth.Text = DateTime.Now.ToString("MMMM");
            if (string.IsNullOrWhiteSpace(txtYear.Text))
                txtYear.Text = DateTime.Now.Year.ToString();
            if (string.IsNullOrWhiteSpace(txtNationality.Text))
                txtNationality.Text = "Pakistani";
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

            AddLabel("--- Front Page ---");
            AddLabel("Plot No / پلاٹ نمبر");
            txtPlotNo = AddBox("txtPlotNo");
            AddLabel("Size / سائز");
            txtSize = AddBox("txtSize");
            AddLabel("Commercial Plot / کمرشل پلاٹ");
            txtCommercialPlot = AddBox("txtCommercialPlot");
            AddLabel("Residential Plot / رہائشی پلاٹ");
            txtResidentialPlot = AddBox("txtResidentialPlot");
            AddLabel("Name / نام");
            txtName = AddBox("txtName");
            AddLabel("Father / Husband / والد / شوہر");
            txtFather = AddBox("txtFather");
            AddLabel("Postal Address / پوسٹل ایڈریس");
            txtPostal = AddBox("txtPostal");
            AddLabel("Residential Address / رہائشی پتہ");
            txtResidential = AddBox("txtResidential");
            AddLabel("Office Phone / فون دفتر");
            txtPhoneOffice = AddBox("txtPhoneOffice");
            AddLabel("Residence Phone / رہائش");
            txtPhoneHome = AddBox("txtPhoneHome");
            AddLabel("Care Of / بمعرفت");
            txtCareOf = AddBox("txtCareOf");
            AddLabel("Mobile / موبائل");
            txtMobile = AddBox("txtMobile");
            AddLabel("Overseas Number / بیرونی ملک");
            txtOverseas = AddBox("txtOverseas");
            AddLabel("Nationality / قومیت");
            txtNationality = AddBox("txtNationality");
            AddLabel("NIC / این آئی سی");
            txtNic = AddBox("txtNic");
            AddLabel("Heir Name & Address / وارث");
            txtHeir = AddBox("txtHeir");
            AddLabel("Place / جگہ");
            txtPlace = AddBox("txtPlace");
            AddLabel("Date / تاریخ");
            txtDate = AddBox("txtDate");

            AddLabel("--- Back Page ---");
            AddLabel("Day / مورخہ");
            txtDay = AddBox("txtDay");
            AddLabel("Month / مہینہ");
            txtMonth = AddBox("txtMonth");
            AddLabel("Year / سال");
            txtYear = AddBox("txtYear");
            AddLabel("Accept Date / تاریخ");
            txtAcceptDate = AddBox("txtAcceptDate");
            AddLabel("Booker Name / بک کرنے والے کا نام");
            txtBooker = AddBox("txtBooker");
            AddLabel("Plot Price / پلاٹ کی قیمت");
            txtPlotPrice = AddBox("txtPlotPrice");
            AddLabel("Corner Plot / کارنر پلاٹ");
            txtCorner = AddBox("txtCorner");
            AddLabel("Roadside Plot / روڈ سائیڈ");
            txtRoadside = AddBox("txtRoadside");
            AddLabel("Other Charges / اضافی اخراجات");
            txtOtherCharges = AddBox("txtOtherCharges");
            AddLabel("Grand Total / گرانڈ ٹوٹل");
            txtGrandTotal = AddBox("txtGrandTotal");

            panelFields.Height = y + 20;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                string reportsDir = Path.Combine(Application.StartupPath, "Reports");
                string frontPath = Path.Combine(reportsDir, "ApplicationFormFront.png");
                string backPath = Path.Combine(reportsDir, "ApplicationFormBack.png");
                string reportPath = Path.Combine(reportsDir, "ApplicationFormBlank.rdlc");

                // PNGs only used for exact page size (same as filled form) — design is NOT printed
                if (!File.Exists(frontPath) || !File.Exists(backPath))
                {
                    MessageBox.Show("Application form images not found in Reports folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Blank overlay report file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Size frontSize;
                Size backSize;
                using (var img = Image.FromFile(frontPath))
                    frontSize = img.Size;
                using (var img = Image.FromFile(backPath))
                    backSize = img.Size;

                byte[] frontBytes = ComposeFrontOverlay(frontSize.Width, frontSize.Height);
                byte[] backBytes = ComposeBackOverlay(backSize.Width, backSize.Height);

                var dt = new DataTable();
                dt.Columns.Add("FrontImage", typeof(byte[]));
                dt.Columns.Add("BackImage", typeof(byte[]));
                dt.Rows.Add(frontBytes, backBytes);

                reportViewer1.Reset();
                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", dt));
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Application Form Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            foreach (Control c in panelFields.Controls)
            {
                if (c is TextBox tb) tb.Clear();
            }
            txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            txtAcceptDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            txtDay.Text = DateTime.Now.Day.ToString();
            txtMonth.Text = DateTime.Now.ToString("MMMM");
            txtYear.Text = DateTime.Now.Year.ToString();
            txtNationality.Text = "Pakistani";
            reportViewer1.Clear();
        }

        /// <summary>White page + field text only (same seats as filled Application Form).</summary>
        private byte[] ComposeFrontOverlay(int width, int height)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                float fs = Math.Max(13f, h * 0.0145f);
                float fsSm = Math.Max(11f, h * 0.0125f);
                using (Font f = CreateFormFont(fs))
                using (Font fSmall = CreateFormFont(fsSm))
                {
                    Color ink = Color.FromArgb(20, 15, 8);

                    // Top row: Size locked to same underline as Plot No
                    DrawOnLine(g, txtPlotNo.Text, 0.70f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtSize.Text, 0.50f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtCommercialPlot.Text, 0.28f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtResidentialPlot.Text, 0.08f, 0.3166f, 0.12f, w, h, f, ink); // 1mm up

                    DrawOnLine(g, txtName.Text, 0.10f, 0.3939f, 0.60f, w, h, f, ink);
                    DrawOnLine(g, txtFather.Text, 0.10f, 0.4339f, 0.52f, w, h, f, ink); // +0.5mm up from 0.4356
                    DrawOnLine(g, txtPostal.Text, 0.10f, 0.4709f, 0.52f, w, h, f, ink); // 0.8mm up
                    DrawOnLine(g, txtResidential.Text, 0.10f, 0.5109f, 0.52f, w, h, f, ink); // 0.8mm up

                    DrawOnLine(g, txtPhoneOffice.Text, 0.70f, 0.5482f, 0.14f, w, h, fSmall, ink);
                    DrawOnLine(g, txtPhoneHome.Text, 0.42f, 0.5482f, 0.14f, w, h, fSmall, ink);
                    // بمعرفت — +20mm right, text CENTER fitted (underline pe)
                    DrawCenteredFitted(g, txtCareOf.Text, 0.10f + (20f / 210f), 0.5482f, 0.14f, w, h, fSmall, ink);

                    DrawOnLine(g, txtMobile.Text, 0.68f, 0.5882f, 0.16f, w, h, f, ink); // 0.8mm up
                    DrawOnLine(g, txtOverseas.Text, 0.12f, 0.5892f, 0.28f, w, h, fSmall, ink);

                    DrawOnLine(g, txtNationality.Text, 0.68f, 0.6299f, 0.14f, w, h, f, ink);
                    DrawOnLine(g, txtNic.Text, 0.12f, 0.6289f, 0.32f, w, h, f, ink); // 0.8mm up
                    DrawOnLine(g, txtHeir.Text, 0.10f, 0.6645f, 0.48f, w, h, fSmall, ink); // 0.8mm up

                    // جگہ — +20mm right, text CENTER fitted (underline pe)
                    DrawCenteredFitted(g, txtPlace.Text, 0.55f + (20f / 210f), 0.9019f, 0.29f, w, h, fSmall, ink);
                    DrawOnLine(g, txtDate.Text, 0.68f, 0.9419f, 0.16f, w, h, fSmall, ink);
                }

                return ToPngBytesWithPrinterSafeBottom(bmp, 0.048f);
            }
        }

        private byte[] ComposeBackOverlay(int width, int height)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                float fs = Math.Max(13f, h * 0.0145f);
                float fsSm = Math.Max(11f, h * 0.0125f);
                float fsDate = Math.Max(14f, h * 0.0165f);
                using (Font f = CreateFormFont(fs))
                using (Font fSmall = CreateFormFont(fsSm))
                using (Font fDate = CreateFormFont(fsDate))
                {
                    Color ink = Color.FromArgb(20, 15, 8);

                    DrawOnLine(g, txtDay.Text, 0.76f, 0.3434f, 0.08f, w, h, fDate, ink); // 1mm down
                    DrawOnLine(g, txtMonth.Text, 0.52f, 0.3434f, 0.10f, w, h, fDate, ink); // 1mm down
                    DrawOnLine(g, txtYear.Text, 0.24f, 0.3434f, 0.12f, w, h, fDate, ink); // 1mm down

                    DrawOnLine(g, txtAcceptDate.Text, 0.68f, 0.5114f, 0.16f, w, h, fDate, ink); // 1mm down

                    DrawOnLine(g, txtBooker.Text, 0.18f, 0.680f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtPlotPrice.Text, 0.18f, 0.722f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtCorner.Text, 0.18f, 0.764f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtRoadside.Text, 0.18f, 0.805f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtOtherCharges.Text, 0.18f, 0.847f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtGrandTotal.Text, 0.18f, 0.889f, 0.52f, w, h, f, ink);
                }

                return ToPngBytesWithPrinterSafeBottom(bmp, 0.048f);
            }
        }

        private static Font CreateFormFont(float size)
        {
            string[] preferred = { "Segoe UI Semibold", "Segoe UI", "Urdu Typesetting", "Microsoft Uighur", "Arabic Typesetting" };
            foreach (string name in preferred)
            {
                try { return new Font(name, size, FontStyle.Bold, GraphicsUnit.Pixel); }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        // Client blank-stock print: tuned so overlay text sits on underlines (A4 297mm).
        private const float BlankUnderlineShiftMm = 7.5f; // was 8.5; −1mm = all fields up
        private const float BlankPageHeightMm = 297f;

        private static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color)
        {
            DrawOnLine(g, text, xPct, lineYPct, wPct, imgW, imgH, font, color, StringAlignment.Far);
        }

        /// <summary>
        /// Seat same — text true CENTER in underline box; shrink + clip so text never goes outside.
        /// </summary>
        private static void DrawCenteredFitted(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();

            float shiftedYPct = lineYPct + (BlankUnderlineShiftMm / BlankPageHeightMm);
            float lineY = shiftedYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.018f);
            float top = lineY - boxH - 2f;
            if (top < 0f) top = 0f;
            if (top + boxH > imgH) top = Math.Max(0f, imgH - boxH);
            var box = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Far;
                sf.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
                sf.Trimming = StringTrimming.None;

                Font drawFont = font;
                Font shrunk = null;
                var state = g.Save();
                try
                {
                    float size = font.Size;
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW - 1f && size > 6f)
                    {
                        size -= 0.35f;
                        if (shrunk != null) shrunk.Dispose();
                        shrunk = new Font(font.FontFamily, size, font.Style, font.Unit);
                        drawFont = shrunk;
                        measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    }

                    // True visual center inside the seat box
                    float textW = Math.Min(measured.Width, maxW);
                    float x = box.X + (maxW - textW) * 0.5f;
                    if (x < box.X) x = box.X;

                    g.SetClip(box);
                    var drawRect = new RectangleF(x, box.Y, textW, box.Height);
                    g.DrawString(t, drawFont, brush, drawRect, sf);
                }
                finally
                {
                    g.Restore(state);
                    if (shrunk != null) shrunk.Dispose();
                }
            }
        }

        private static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color, StringAlignment hAlign)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();
            // Move text down so it sits on the printed blank form underlines
            float shiftedYPct = lineYPct + (BlankUnderlineShiftMm / BlankPageHeightMm);
            float lineY = shiftedYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.018f);
            float top = lineY - boxH - 2f;
            if (top < 0f) top = 0f;
            // Keep within page if shift pushes past bottom
            if (top + boxH > imgH) top = Math.Max(0f, imgH - boxH);
            var rect = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.Alignment = hAlign;
                sf.LineAlignment = StringAlignment.Far;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                Font drawFont = font;
                Font shrunk = null;
                var state = g.Save();
                try
                {
                    float size = font.Size;
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW && size > 6.5f)
                    {
                        size -= 0.4f;
                        if (shrunk != null) shrunk.Dispose();
                        shrunk = new Font(font.FontFamily, size, font.Style, font.Unit);
                        drawFont = shrunk;
                        measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    }
                    g.SetClip(rect);
                    g.DrawString(t, drawFont, brush, rect, sf);
                }
                finally
                {
                    g.Restore(state);
                    if (shrunk != null) shrunk.Dispose();
                }
            }
        }

        private static byte[] ToPngBytesWithPrinterSafeBottom(Bitmap composed, float bottomPadPct)
        {
            int w = composed.Width;
            int h = composed.Height;
            using (var final = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(final))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                float pad = h * bottomPadPct;
                float drawH = h - pad;
                float drawW = w * (drawH / h);
                float left = (w - drawW) * 0.5f;
                g.DrawImage(composed, left, 0f, drawW, drawH);
                return ToPngBytes(final);
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
