using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

class P {
  static void Main() {
    string srcPath = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaMarketBhawaniReceipt.png";
    string outPath = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview\market_fill_preview.png";
    using (var src = Image.FromFile(srcPath))
    using (var bmp = new Bitmap(src.Width, src.Height))
    using (var g = Graphics.FromImage(bmp)) {
      g.DrawImage(src, 0, 0, src.Width, src.Height);
      g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
      float w = bmp.Width, h = bmp.Height;
      float fs = Math.Max(18f, h * 0.028f) + 2f;
      using (var f = new Font("Segoe UI Semibold", fs, FontStyle.Bold, GraphicsUnit.Pixel)) {
        Color ink = Color.FromArgb(15, 20, 90);
        Draw(g, "02-08-2026", 0.04f, 0.293f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "MB-1001", 0.72f, 0.293f, 0.17f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "احمد خان", 0.34f, 0.368f, 0.24f, w, h, f, ink, true, StringAlignment.Center);
        Draw(g, "محمد خان", 0.44f, 0.438f, 0.22f, w, h, f, ink, true, StringAlignment.Center);
        Draw(g, "ایک لاکھ روپے", 0.20f, 0.506f, 0.32f, w, h, f, ink, true, StringAlignment.Far);
        Draw(g, "100,000", 0.68f, 0.506f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "02-08-2026", 0.05f, 0.584f, 0.20f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "کیش", 0.34f, 0.584f, 0.28f, w, h, f, ink, true, StringAlignment.Center);
        Draw(g, "A", 0.06f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "1", 0.30f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "5 Marla", 0.50f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "12", 0.70f, 0.663f, 0.14f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "50,000", 0.40f, 0.750f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
        Draw(g, "100,000", 0.68f, 0.750f, 0.18f, w, h, f, ink, false, StringAlignment.Center);
      }
      Directory.CreateDirectory(Path.GetDirectoryName(outPath));
      bmp.Save(outPath);
      Console.WriteLine("OK " + outPath);
    }
  }
  static void Draw(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color, bool rtl, StringAlignment align) {
    float lineY = lineYPct * imgH;
    float maxW = wPct * imgW;
    float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.032f);
    float top = lineY - boxH - 1f;
    if (top < 0f) top = 0f;
    var rect = new RectangleF(xPct * imgW, top, maxW, boxH);
    using (var brush = new SolidBrush(color))
    using (var sf = new StringFormat(StringFormat.GenericTypographic)) {
      if (rtl) sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
      sf.Alignment = align;
      sf.LineAlignment = StringAlignment.Far;
      sf.Trimming = StringTrimming.None;
      sf.FormatFlags |= StringFormatFlags.NoWrap;
      // shrink fit
      float size = font.Size;
      Font use = font;
      SizeF measured;
      while (true) {
        measured = g.MeasureString(text, use, int.MaxValue, sf);
        if (measured.Width <= maxW || size <= 10f) break;
        size -= 1f;
        if (use != font) use.Dispose();
        use = new Font(font.FontFamily, size, font.Style, GraphicsUnit.Pixel);
      }
      g.DrawString(text, use, brush, rect, sf);
      // debug seat line
      using (var pen = new Pen(Color.FromArgb(120, 255, 0, 0), 1f)) g.DrawLine(pen, rect.Left, lineY, rect.Right, lineY);
      if (use != font) use.Dispose();
    }
  }
}
