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
    public partial class frmApplicationForm : Sample
    {
        public frmApplicationForm()
        {
            InitializeComponent();
            BuildFields();
        }

        private void frmApplicationForm_Load(object sender, EventArgs e)
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
                string reportPath = Path.Combine(reportsDir, "ApplicationForm.rdlc");

                if (!File.Exists(frontPath) || !File.Exists(backPath))
                {
                    MessageBox.Show("Application form images not found in Reports folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] frontBytes = ComposeFrontPage(frontPath);
                byte[] backBytes = ComposeBackPage(backPath);

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
                MessageBox.Show("Error: " + ex.Message, "Application Form", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private byte[] ComposeFrontPage(string templatePath)
        {
            using (var msIn = new MemoryStream(File.ReadAllBytes(templatePath)))
            using (var src = Image.FromStream(msIn))
            using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(src, 0, 0, src.Width, src.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                // Keep font proportional to scan so text locks to blank lines after RDLC fit
                float fs = Math.Max(13f, h * 0.0145f);
                float fsSm = Math.Max(11f, h * 0.0125f);
                using (Font f = CreateFormFont(fs))
                using (Font fSmall = CreateFormFont(fsSm))
                {
                    Color ink = Color.FromArgb(20, 15, 8);

                    // RTL blanks: Plot | Size | Commercial | Residential
                    DrawOnLine(g, txtPlotNo.Text, 0.70f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtSize.Text, 0.50f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtCommercialPlot.Text, 0.28f, 0.320f, 0.12f, w, h, f, ink);
                    DrawOnLine(g, txtResidentialPlot.Text, 0.08f, 0.320f, 0.12f, w, h, f, ink);

                    DrawOnLine(g, txtName.Text, 0.10f, 0.399f, 0.60f, w, h, f, ink);
                    DrawOnLine(g, txtFather.Text, 0.10f, 0.440f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtPostal.Text, 0.10f, 0.477f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtResidential.Text, 0.10f, 0.517f, 0.52f, w, h, f, ink);

                    // RTL: Office | Residence | Care Of
                    DrawOnLine(g, txtPhoneOffice.Text, 0.70f, 0.556f, 0.14f, w, h, fSmall, ink);
                    DrawOnLine(g, txtPhoneHome.Text, 0.42f, 0.556f, 0.14f, w, h, fSmall, ink);
                    DrawOnLine(g, txtCareOf.Text, 0.10f, 0.556f, 0.14f, w, h, fSmall, ink);

                    DrawOnLine(g, txtMobile.Text, 0.68f, 0.596f, 0.16f, w, h, f, ink);
                    DrawOnLine(g, txtOverseas.Text, 0.12f, 0.596f, 0.28f, w, h, fSmall, ink);

                    DrawOnLine(g, txtNationality.Text, 0.68f, 0.635f, 0.14f, w, h, f, ink);
                    DrawOnLine(g, txtNic.Text, 0.12f, 0.635f, 0.32f, w, h, f, ink);
                    DrawOnLine(g, txtHeir.Text, 0.10f, 0.674f, 0.48f, w, h, fSmall, ink);

                    DrawOnLine(g, txtPlace.Text, 0.55f, 0.908f, 0.29f, w, h, fSmall, ink);
                    DrawOnLine(g, txtDate.Text, 0.68f, 0.948f, 0.16f, w, h, fSmall, ink);
                }

                // Shrink slightly + leave bottom gap so A4 printer hard-margin does not clip
                // Place date + "درخواست دہندہ کے دستخط" signature line on page 1
                return ToPngBytesWithPrinterSafeBottom(bmp, 0.048f);
            }
        }

        private byte[] ComposeBackPage(string templatePath)
        {
            using (var msIn = new MemoryStream(File.ReadAllBytes(templatePath)))
            using (var src = Image.FromStream(msIn))
            using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(src, 0, 0, src.Width, src.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                float fs = Math.Max(13f, h * 0.0145f);
                float fsSm = Math.Max(11f, h * 0.0125f);
                // Slightly larger for day/month/year + accept date (red-marked fields)
                float fsDate = Math.Max(14f, h * 0.0165f);
                using (Font f = CreateFormFont(fs))
                using (Font fSmall = CreateFormFont(fsSm))
                using (Font fDate = CreateFormFont(fsDate))
                {
                    Color ink = Color.FromArgb(20, 15, 8);

                    // RTL blanks between labels: Day | Month | Year (slightly up + larger)
                    DrawOnLine(g, txtDay.Text, 0.76f, 0.340f, 0.08f, w, h, fDate, ink);
                    DrawOnLine(g, txtMonth.Text, 0.52f, 0.340f, 0.10f, w, h, fDate, ink);
                    DrawOnLine(g, txtYear.Text, 0.24f, 0.340f, 0.12f, w, h, fDate, ink);

                    DrawOnLine(g, txtAcceptDate.Text, 0.68f, 0.508f, 0.16f, w, h, fDate, ink);

                    DrawOnLine(g, txtBooker.Text, 0.18f, 0.680f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtPlotPrice.Text, 0.18f, 0.722f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtCorner.Text, 0.18f, 0.764f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtRoadside.Text, 0.18f, 0.805f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtOtherCharges.Text, 0.18f, 0.847f, 0.52f, w, h, f, ink);
                    DrawOnLine(g, txtGrandTotal.Text, 0.18f, 0.889f, 0.52f, w, h, f, ink);
                }

                // Shrink slightly + leave bottom gap so A4 printer hard-margin does not clip mobile footer
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

        /// <summary>
        /// Draw text so glyph baseline sits just above the printed underline (lineYPct).
        /// Right-aligned; long text shrinks to fit (no "..." cut-off).
        /// </summary>
        private static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();
            float lineY = lineYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.018f);
            float top = lineY - boxH - 2f;
            if (top < 0f) top = 0f;
            var rect = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.Alignment = StringAlignment.Far;
                sf.LineAlignment = StringAlignment.Far;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                Font drawFont = font;
                Font shrunk = null;
                try
                {
                    float size = font.Size;
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW && size > 7.5f)
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

        /// <summary>
        /// Scale page content up slightly so bottom footer clears typical A4 printer hard margins.
        /// </summary>
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
