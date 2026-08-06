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

    // Tuned from preview A feedback + RTL blanks left of labels
    var seats = new[] {
      new Seat("DATE", "30-07-2026", 0.58f, 0.270f, 0.20f),
      new Seat("NAME", "محمد احمد خان", 0.40f, 0.325f, 0.36f),
      new Seat("FATH", "غلام حسین", 0.13f, 0.325f, 0.22f),
      new Seat("MEM", "MB-1024", 0.56f, 0.362f, 0.20f),
      new Seat("CAT", "کمرشل", 0.30f, 0.362f, 0.16f),
      new Seat("PLOT", "A-15", 0.60f, 0.402f, 0.14f),
      new Seat("AREA", "120", 0.38f, 0.402f, 0.12f),
      new Seat("LOC", "بلاک بی فیز 1", 0.30f, 0.440f, 0.38f),
    };
    Compose(front, Path.Combine(outDir, "preview_front_D.png"), seats, 0.17f, 0.32f);
    ComposeFull(front, Path.Combine(outDir, "preview_front_D_full.png"), seats, 0f);

    var nic = new[] {
      new Seat("NIC", "42201-1234567-1", 0.22f, 0.545f, 0.50f),
    };
    Compose(back, Path.Combine(outDir, "preview_back_D.png"), nic, 0.35f, 0.55f);
    ComposeFull(back, Path.Combine(outDir, "preview_back_D_full.png"), nic, 0f);

    // also try NIC slightly higher/lower
    Compose(back, Path.Combine(outDir, "preview_back_D2.png"), new[]{ new Seat("NIC","42201-1234567-1",0.22f,0.525f,0.50f)}, 0.35f, 0.55f);
    Compose(back, Path.Combine(outDir, "preview_back_D3.png"), new[]{ new Seat("NIC","42201-1234567-1",0.22f,0.560f,0.50f)}, 0.35f, 0.55f);
    Console.WriteLine("done");
  }
  class Seat {
    public string Tag, Text; public float X,Y,W;
    public Seat(string t,string te,float x,float y,float w){Tag=t;Text=te;X=x;Y=y;W=w;}
  }
  static Font MakeFont(float size) {
    string[] preferred = { "Jameel Noori Nastaleeq", "Urdu Typesetting", "Segoe UI Semibold", "Segoe UI" };
    foreach (var n in preferred) { try { return new Font(n, size, FontStyle.Regular, GraphicsUnit.Pixel); } catch {} }
    return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold, GraphicsUnit.Pixel);
  }
  static void Compose(string template, string outPath, Seat[] seats, float y0p, float hp) {
    using (var src = Image.FromFile(template))
    using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      DrawAll(g, src, bmp.Width, bmp.Height, seats);
      int y0=(int)(bmp.Height*y0p), ch=(int)(bmp.Height*hp), x0=(int)(bmp.Width*0.06), cw=(int)(bmp.Width*0.88);
      using (var crop = bmp.Clone(new Rectangle(x0,y0,cw,Math.Min(ch,bmp.Height-y0)), bmp.PixelFormat)) {
        int tw=1300; int th=(int)(crop.Height*(tw/(double)crop.Width));
        using (var thumb=new Bitmap(tw,th))
        using (var gg=Graphics.FromImage(thumb)) {
          gg.InterpolationMode=InterpolationMode.HighQualityBicubic;
          gg.DrawImage(crop,0,0,tw,th);
          thumb.Save(outPath, ImageFormat.Png);
        }
      }
    }
    Console.WriteLine("wrote "+outPath);
  }
  static void ComposeFull(string template, string outPath, Seat[] seats, float pad) {
    using (var src = Image.FromFile(template))
    using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      DrawAll(g, src, bmp.Width, bmp.Height, seats);
      int tw=700; int th=(int)(bmp.Height*(tw/(double)bmp.Width));
      using (var thumb=new Bitmap(tw,th))
      using (var gg=Graphics.FromImage(thumb)) {
        gg.InterpolationMode=InterpolationMode.HighQualityBicubic;
        gg.DrawImage(bmp,0,0,tw,th);
        thumb.Save(outPath, ImageFormat.Png);
      }
    }
  }
  static void DrawAll(Graphics g, Image src, float w, float h, Seat[] seats) {
    g.DrawImage(src,0,0,(int)w,(int)h);
    g.SmoothingMode=SmoothingMode.AntiAlias;
    g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
    float fs = Math.Max(18f, h * 0.0095f);
    using (var font = MakeFont(fs))
    using (var brush = new SolidBrush(Color.FromArgb(25,18,10)))
    using (var red = new Pen(Color.FromArgb(180,220,0,0),2))
    using (var tagFont = new Font("Arial", Math.Max(12f,h*0.007f), FontStyle.Bold, GraphicsUnit.Pixel)) {
      foreach (var s in seats) {
        DrawOnLine(g, s.Text, s.X, s.Y, s.W, w, h, font, brush.Color);
        float rx=s.X*w, ry=s.Y*h - h*0.014f, rw=s.W*w, rh=h*0.014f;
        g.DrawRectangle(red, rx, ry, rw, rh);
        g.DrawString(s.Tag, tagFont, Brushes.Red, rx, ry - h*0.011f);
      }
    }
  }
  static void DrawOnLine(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font, Color color) {
    if (string.IsNullOrWhiteSpace(text)) return;
    string t=text.Trim();
    float lineY=lineYPct*imgH, maxW=wPct*imgW;
    float boxH=Math.Max(font.GetHeight(g)*1.1f, imgH*0.015f);
    float top=lineY-boxH-1f; if(top<0)top=0;
    var rect=new RectangleF(xPct*imgW, top, maxW, boxH);
    using (var brush=new SolidBrush(color))
    using (var sf=new StringFormat(StringFormat.GenericTypographic)) {
      sf.Alignment=StringAlignment.Center;
      sf.LineAlignment=StringAlignment.Far;
      sf.FormatFlags |= StringFormatFlags.NoWrap;
      Font draw=font; Font shrunk=null;
      try {
        float size=font.Size; var m=g.MeasureString(t,draw,int.MaxValue,sf);
        while(m.Width>maxW && size>10f){ size-=0.5f; if(shrunk!=null)shrunk.Dispose(); shrunk=new Font(font.FontFamily,size,font.Style,font.Unit); draw=shrunk; m=g.MeasureString(t,draw,int.MaxValue,sf);} 
        g.DrawString(t, draw, brush, rect, sf);
      } finally { if(shrunk!=null) shrunk.Dispose(); }
    }
  }
}