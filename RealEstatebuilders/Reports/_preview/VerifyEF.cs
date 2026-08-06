using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class V {
  static void Main() {
    string reports = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports";
    string outDir = reports + @"\_preview";
    string front = Path.Combine(reports, "TemporaryAllotmentLetterFront.png");
    string back = Path.Combine(reports, "TemporaryAllotmentLetterBack.png");

    // Candidate E: lower onto underscores, tighter blanks, smaller font
    var seats = new[] {
      S("DATE","30-07-2026", 0.56f, 0.282f, 0.22f),
      S("NAME","irtaza hussain", 0.42f, 0.345f, 0.30f),
      S("FATH","Naik mohammad", 0.12f, 0.345f, 0.22f),
      S("MEM","Memership no", 0.58f, 0.378f, 0.18f),
      S("CAT","category", 0.30f, 0.378f, 0.16f),
      S("PLOT","plot no", 0.62f, 0.418f, 0.13f),
      S("AREA","area", 0.40f, 0.418f, 0.12f),
      S("LOC","location detail", 0.34f, 0.458f, 0.34f),
    };
    Compose(front, Path.Combine(outDir,"verify_front_E.png"), seats, 0.20f, 0.30f, true);
    Compose(front, Path.Combine(outDir,"verify_front_E_full.png"), seats, 0, 1, false);

    // Candidate F: even lower
    var seatsF = new[] {
      S("DATE","30-07-2026", 0.56f, 0.288f, 0.22f),
      S("NAME","irtaza hussain", 0.43f, 0.352f, 0.28f),
      S("FATH","Naik mohammad", 0.12f, 0.352f, 0.22f),
      S("MEM","Memership no", 0.58f, 0.385f, 0.18f),
      S("CAT","category", 0.30f, 0.385f, 0.16f),
      S("PLOT","plot no", 0.62f, 0.425f, 0.13f),
      S("AREA","area", 0.40f, 0.425f, 0.12f),
      S("LOC","location detail", 0.34f, 0.465f, 0.34f),
    };
    Compose(front, Path.Combine(outDir,"verify_front_F.png"), seatsF, 0.20f, 0.30f, true);

    var nicE = new[] { S("NIC","42201-1234567-1", 0.20f, 0.515f, 0.50f) };
    var nicF = new[] { S("NIC","42201-1234567-1", 0.20f, 0.520f, 0.50f) };
    Compose(back, Path.Combine(outDir,"verify_back_E.png"), nicE, 0.40f, 0.25f, true);
    Compose(back, Path.Combine(outDir,"verify_back_F.png"), nicF, 0.40f, 0.25f, true);
    Console.WriteLine("ok");
  }
  class Seat { public string Tag,Text; public float X,Y,W; }
  static Seat S(string t,string te,float x,float y,float w){ return new Seat{Tag=t,Text=te,X=x,Y=y,W=w}; }

  static void Compose(string template, string outPath, Seat[] seats, float y0p, float hp, bool crop) {
    using (var src = Image.FromFile(template))
    using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      g.DrawImage(src, 0, 0, src.Width, src.Height);
      g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      float w=bmp.Width,h=bmp.Height;
      float fs = Math.Max(16f, h * 0.0078f);
      using (var font = new Font("Segoe UI Semibold", fs, FontStyle.Bold, GraphicsUnit.Pixel))
      using (var red = new Pen(Color.FromArgb(200,220,0,0), 3))
      using (var tagFont = new Font("Arial", Math.Max(11f,h*0.006f), FontStyle.Bold, GraphicsUnit.Pixel)) {
        foreach (var s in seats) {
          Draw(g, s.Text, s.X, s.Y, s.W, w, h, font);
          float rx=s.X*w, ry=s.Y*h - h*0.012f, rw=s.W*w, rh=h*0.012f;
          g.DrawRectangle(red, rx, ry, rw, rh);
          g.DrawString(s.Tag, tagFont, Brushes.Red, rx, ry - h*0.01f);
        }
      }
      if (!crop) {
        int tw=750; int th=(int)(h*(tw/w));
        using (var thumb=new Bitmap(tw,th))
        using (var gg=Graphics.FromImage(thumb)) { gg.DrawImage(bmp,0,0,tw,th); thumb.Save(outPath, ImageFormat.Png); }
      } else {
        int y0=(int)(h*y0p), ch=(int)(h*hp), x0=(int)(w*0.05), cw=(int)(w*0.90);
        using (var c=bmp.Clone(new Rectangle(x0,y0,cw,ch), bmp.PixelFormat)) {
          int tw=1400; int th=(int)(c.Height*(tw/(double)c.Width));
          using (var thumb=new Bitmap(tw,th))
          using (var gg=Graphics.FromImage(thumb)) { gg.DrawImage(c,0,0,tw,th); thumb.Save(outPath, ImageFormat.Png); }
        }
      }
    }
    Console.WriteLine(outPath);
  }
  static void Draw(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font) {
    if (string.IsNullOrWhiteSpace(text)) return;
    float lineY=lineYPct*imgH, maxW=wPct*imgW;
    float boxH=Math.Max(font.GetHeight(g)*1.05f, imgH*0.012f);
    // sit ON the underscore (baseline near line)
    float top = lineY - boxH + imgH*0.002f;
    var rect=new RectangleF(xPct*imgW, top, maxW, boxH);
    using (var br=new SolidBrush(Color.FromArgb(20,15,10)))
    using (var sf=new StringFormat(StringFormat.GenericTypographic)) {
      sf.Alignment=StringAlignment.Center; sf.LineAlignment=StringAlignment.Far;
      sf.FormatFlags |= StringFormatFlags.NoWrap;
      Font df=font; Font sh=null;
      try {
        float size=font.Size; var m=g.MeasureString(text,df,int.MaxValue,sf);
        while(m.Width>maxW && size>9f){ size-=0.4f; if(sh!=null)sh.Dispose(); sh=new Font(font.FontFamily,size,font.Style,font.Unit); df=sh; m=g.MeasureString(text,df,int.MaxValue,sf);} 
        g.DrawString(text, df, br, rect, sf);
      } finally { if(sh!=null) sh.Dispose(); }
    }
  }
}