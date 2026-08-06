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
    /// Blank overlay for pre-printed Temporary Allotment Letter stock.
    /// Same field seats/scale as filled Temporary Allotment Letter — white page, text only (no design reprint).
    /// Does not change the filled Temporary Allotment Letter report.
    /// </summary>
    public partial class frmTemporaryAllotmentLetterBlank : Sample
    {
        public frmTemporaryAllotmentLetterBlank()
        {
            InitializeComponent();
            BuildFields();
        }

        private void frmTemporaryAllotmentLetterBlank_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDate.Text))
                txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
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

            AddLabel("--- Page 1 ---");
            AddLabel("Date / تاریخ");
            txtDate = AddBox("txtDate");
            AddLabel("Name / محترم محترمہ مسماۃ");
            txtName = AddBox("txtName");
            AddLabel("Father-Husband / ولدیت شوہر (نیچے والی لائن)");
            txtFather = AddBox("txtFather");
            AddLabel("Membership No / ممبر شپ نمبر");
            txtMembershipNo = AddBox("txtMembershipNo");
            AddLabel("Plot No / پلاٹ نمبر");
            txtPlotNo = AddBox("txtPlotNo");
            AddLabel("Area / رقبہ");
            txtArea = AddBox("txtArea");
            AddLabel("Location / الاٹ والی لائن");
            txtLocationDetail = AddBox("txtLocationDetail");

            AddLabel("--- Page 2 ---");
            AddLabel("CNIC / این آئی سی نمبر");
            txtNic = AddBox("txtNic");

            panelFields.Height = y + 20;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                string reportsDir = Path.Combine(Application.StartupPath, "Reports");
                string frontPath = Path.Combine(reportsDir, "TemporaryAllotmentLetterFront.png");
                string backPath = Path.Combine(reportsDir, "TemporaryAllotmentLetterBack.png");
                string reportPath = Path.Combine(reportsDir, "TemporaryAllotmentLetterBlank.rdlc");

                // PNGs only for exact page size — design image is NOT printed
                if (!File.Exists(frontPath) || !File.Exists(backPath))
                {
                    MessageBox.Show("Temporary Allotment Letter images not found in Reports folder.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Blank overlay report file not found!", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Error: " + ex.Message, "Temporary Allotment Letter (Blank)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            foreach (Control c in panelFields.Controls)
            {
                if (c is TextBox tb) tb.Clear();
            }
            txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
            reportViewer1.Clear();
        }

        /// <summary>White page + field text only (same seats as filled Temporary Allotment Letter).</summary>
        private byte[] ComposeFrontOverlay(int width, int height)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                float fs = Math.Max(28f, h * 0.0145f);
                using (Font f = CreateFormFont(fs))
                {
                    Color ink = Color.FromArgb(25, 18, 10);

                    DrawOnLine(g, txtDate.Text, 0.10f, 0.270f, 0.18f, w, h, f, ink);
                    DrawOnLine(g, txtName.Text, 0.24f, 0.325f, 0.26f, w, h, f, ink);
                    DrawOnLine(g, txtFather.Text, 0.62f, 0.360f, 0.18f, w, h, f, ink);
                    DrawOnLine(g, txtMembershipNo.Text, 0.18f, 0.360f, 0.30f, w, h, f, ink);
                    DrawOnLine(g, txtPlotNo.Text, 0.70f, 0.395f, 0.11f, w, h, f, ink);
                    using (Font fArea = CreateFormFont(Math.Max(34f, h * 0.0175f)))
                        DrawOnLine(g, txtArea.Text, 0.50f, 0.395f, 0.14f, w, h, fArea, ink);
                    DrawOnLine(g, txtLocationDetail.Text, 0.52f, 0.438f, 0.22f, w, h, f, ink);
                }

                return ToPngBytes(bmp);
            }
        }

        private byte[] ComposeBackOverlay(int width, int height)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float w = bmp.Width;
                float h = bmp.Height;
                float fs = Math.Max(28f, h * 0.0145f);
                using (Font f = CreateFormFont(fs))
                {
                    Color ink = Color.FromArgb(25, 18, 10);
                    DrawOnLine(g, txtNic.Text, 0.18f, 0.515f, 0.48f, w, h, f, ink);
                }

                return ToPngBytes(bmp);
            }
        }

        private static Font CreateFormFont(float size)
        {
            string[] preferred =
            {
                "Segoe UI Semibold",
                "Segoe UI",
                "Jameel Noori Nastaleeq",
                "Urdu Typesetting",
                "Microsoft Uighur",
                "Arabic Typesetting"
            };
            foreach (string name in preferred)
            {
                try { return new Font(name, size, FontStyle.Bold, GraphicsUnit.Pixel); }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string t = text.Trim();
            float lineY = lineYPct * imgH;
            float maxW = wPct * imgW;
            float boxH = Math.Max(font.GetHeight(g) * 1.08f, imgH * 0.016f);
            float top = lineY - boxH - 1f;
            if (top < 0f) top = 0f;
            var rect = new RectangleF(xPct * imgW, top, maxW, boxH);

            using (var brush = new SolidBrush(color))
            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Far;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                Font drawFont = font;
                Font shrunk = null;
                try
                {
                    float size = font.Size;
                    SizeF measured = g.MeasureString(t, drawFont, int.MaxValue, sf);
                    while (measured.Width > maxW && size > 14f)
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
