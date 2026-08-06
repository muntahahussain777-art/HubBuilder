using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class Preview {
  static void Main() {
    string front = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterFront.png";
    string back = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterBack.png";
    string outDir = Path.GetDirectoryName(front) + @"\_preview";

    // Candidate seat maps to try - version A (shifted to detected underlines)
    var seatsA = new[] {
      new Seat("DATE", "30-07-2026", 0.58f, 0.268f, 0.22f),
      new Seat("NAME", "محمد احمد خان", 0.38f, 0.323f, 0.38f),
      new Seat("FATH", "غلام حسین", 0.12f, 0.323f, 0.22f),
      new Seat("MEM", "MB-1024", 0.55f, 0.360f, 0.22f),
      new Seat("CAT", "کمرشل", 0.28f, 0.360f, 0.18f),
      new Seat("PLOT", "A-15", 0.58f, 0.400f, 0.16f),
      new Seat("AREA", "120", 0.35f, 0.400f, 0.14f),
      new Seat("LOC", "بلاک بی فیز 1", 0.22f, 0.438f, 0.38f),
    };
    Compose(front, Path.Combine(outDir, "preview_front_A.png"), seatsA, true);

    // Version B - slightly lower / adjusted X for RTL blanks
    var seatsB = new[] {
      new Seat("DATE", "30-07-2026", 0.62f, 0.272f, 0.18f),
      new Seat("NAME", "محمد احمد خان", 0.42f, 0.328f, 0.36f),
      new Seat("FATH", "غلام حسین", 0.14f, 0.328f, 0.24f),
      new Seat("MEM", "MB-1024", 0.58f, 0.365f, 0.20f),
      new Seat("CAT", "کمرشل", 0.32f, 0.365f, 0.16f),
      new Seat("PLOT", "A-15", 0.62f, 0.405f, 0.14f),
      new Seat("AREA", "120", 0.38f, 0.405f, 0.14f),
      new Seat("LOC", "بلاک بی فیز 1", 0.28f, 0.442f, 0.42f),
    };
    Compose(front, Path.Combine(outDir, "preview_front_B.png"), seatsB, true);

    // Version C - from visual reading of grid (body lower)
    var seatsC = new[] {
      new Seat("DATE", "30-07-2026", 0.55f, 0.265f, 0.25f),
      new Seat("NAME", "محمد احمد خان", 0.35f, 0.318f, 0.40f),
      new Seat("FATH", "غلام حسین", 0.12f, 0.318f, 0.20f),
      new Seat("MEM", "MB-1024", 0.52f, 0.355f, 0.24f),
      new Seat("CAT", "کمرشل", 0.25f, 0.355f, 0.18f),
      new Seat("PLOT", "A-15", 0.55f, 0.395f, 0.18f),
      new Seat("AREA", "120", 0.30f, 0.395f, 0.16f),
      new Seat("LOC", "بلاک بی فیز 1", 0.20f, 0.435f, 0.45f),
    };
    Compose(front, Path.Combine(outDir, "preview_front_C.png"), seatsC, true);

    // Back NIC candidates
    var nicA = new[] { new Seat("NIC", "42201-1234567-1", 0.18f, 0.78f, 0.55f) };
    var nicB = new[] { new Seat("NIC", "42201-1234567-1", 0.20f, 0.82f, 0.52f) };
    var nicC = new[] { new Seat("NIC", "42201-1234567-1", 0.22f, 0.72f, 0.50f) };
    Compose(back, Path.Combine(outDir, "preview_back_A.png"), nicA, true);
    Compose(back, Path.Combine(outDir, "preview_back_B.png"), nicB, true);
    Compose(back, Path.Combine(outDir, "preview_back_C.png"), nicC, true);
    Console.WriteLine("done");
  }

  class Seat {
    public string Tag, Text; public float X, Y, W;
    public Seat(string t, string te, float x, float y, float w) { Tag=t; Text=te; X=x; Y=y; W=w; }
  }

  static void Compose(string template, string outPath, Seat[] seats, bool cropFields) {
    using (var src = Image.FromFile(template))
    using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      g.DrawImage(src, 0, 0, src.Width, src.Height);
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
      float w = bmp.Width, h = bmp.Height;
      float fs = Math.Max(20f, h * 0.011f);
      using (var font = new Font("Segoe UI Semibold", fs, FontStyle.Bold, GraphicsUnit.Pixel))
      using (var brush = new SolidBrush(Color.FromArgb(30, 20, 10)))
      using (var red = new Pen(Color.FromArgb(160, 220, 0, 0), 2))
      using (var tagFont = new Font("Arial", Math.Max(14f, h*0.008f), FontStyle.Bold, GraphicsUnit.Pixel)) {
        foreach (var s in seats) {
          DrawOnLine(g, s.Text, s.X, s.Y, s.W, w, h, font, brush.Color);
          float rx=s.X*w, ry=s.Y*h - h*0.016f, rw=s.W*w, rh=h*0.016f;
          g.DrawRectangle(red, rx, ry, rw, rh);
          g.DrawString(s.Tag, tagFont, Brushes.Red, rx, ry - h*0.012f);
        }
      }
      if (cropFields) {
        int y0=(int)(h*0.17), ch=(int)(h*0.32), x0=(int)(w*0.06), cw=(int)(w*0.88);
        if (outPath.Contains("back")) { y0=(int)(h*0.55); ch=(int)(h*0.38); }
        using (var crop = bmp.Clone(new Rectangle(x0,y0,cw,ch), bmp.PixelFormat)) {
          int tw = 1200; int th = (int)(crop.Height * (tw/(double)crop.Width));
          using (var thumb = new Bitmap(tw, th))
          using (var gg = Graphics.FromImage(thumb)) {
            gg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gg.DrawImage(crop, 0, 0, tw, th);
            thumb.Save(outPath, ImageFormat.Png);
          }
        }
      } else bmp.Save(outPath, ImageFormat.Png);
    }
    Console.WriteLine("wrote " + outPath);
  }

  static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color) {
    if (string.IsNullOrWhiteSpace(text)) return;
    string t = text.Trim();
    float lineY = lineYPct * imgH;
    float maxW = wPct * imgW;
    float boxH = Math.Max(font.GetHeight(g) * 1.15f, imgH * 0.018f);
    float top = lineY - boxH - 2f;
    if (top < 0f) top = 0f;
    var rect = new RectangleF(xPct * imgW, top, maxW, boxH);
    using (var brush = new SolidBrush(color))
    using (var sf = new StringFormat(StringFormat.GenericTypographic)) {
      sf.Alignment = StringAlignment.Far; // RTL-ish
      sf.LineAlignment = StringAlignment.Far;
      sf.FormatFlags |= StringFormatFlags.NoWrap;
      g.DrawString(t, font, brush, rect, sf);
    }
  }
}