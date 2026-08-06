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
    var seats = new[] {
      S("DATE","30-07-2026", 0.55f, 0.273f, 0.20f),
      S("NAME","raza hussain", 0.36f, 0.325f, 0.28f),
      S("FATH","naik mohammad", 0.11f, 0.325f, 0.20f),
      S("MEM","membership no", 0.53f, 0.360f, 0.20f),
      S("CAT","category", 0.20f, 0.360f, 0.18f),
      S("PLOT","plot no", 0.68f, 0.398f, 0.14f),
      S("AREA","area", 0.50f, 0.398f, 0.14f),
      S("LOC","location detail", 0.40f, 0.435f, 0.30f),
    };
    Compose(front, Path.Combine(outDir,"fix_J.png"), seats, 0.20f, 0.28f, 0.0122f);
    Compose(back, Path.Combine(outDir,"fix_J_back.png"), new[]{ S("NIC","nic no", 0.15f, 0.515f, 0.42f) }, 0.42f, 0.20f, 0.0122f);
  }
  class Seat { public string Tag,Text; public float X,Y,W; }
  static Seat S(string t,string te,float x,float y,float w){return new Seat{Tag=t,Text=te,X=x,Y=y,W=w};}
  static void Compose(string template, string outPath, Seat[] seats, float y0p, float hp, float fontPct) {
    using (var src = Image.FromFile(template))
    using (var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      g.DrawImage(src,0,0,src.Width,src.Height);
      g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
      float w=bmp.Width,h=bmp.Height; float fs=Math.Max(26f,h*fontPct);
      using (var font=new Font("Segoe UI Semibold", fs, FontStyle.Bold, GraphicsUnit.Pixel))
      using (var red=new Pen(Color.FromArgb(200,220,0,0),3))
      using (var tagFont=new Font("Arial", Math.Max(13f,h*0.007f), FontStyle.Bold, GraphicsUnit.Pixel)) {
        foreach (var s in seats) {
          Draw(g,s.Text,s.X,s.Y,s.W,w,h,font);
          float rx=s.X*w, ry=s.Y*h-h*0.015f, rw=s.W*w, rh=h*0.015f;
          g.DrawRectangle(red,rx,ry,rw,rh);
          g.DrawString(s.Tag, tagFont, Brushes.Red, rx, ry-h*0.012f);
        }
      }
      int y0=(int)(h*y0p), ch=(int)(h*hp), x0=(int)(w*0.05), cw=(int)(w*0.90);
      using (var c=bmp.Clone(new Rectangle(x0,y0,cw,ch), bmp.PixelFormat)) {
        int tw=1500; int th=(int)(c.Height*(tw/(double)c.Width));
        using (var thumb=new Bitmap(tw,th))
        using (var gg=Graphics.FromImage(thumb)) { gg.DrawImage(c,0,0,tw,th); thumb.Save(outPath, ImageFormat.Png); }
      }
    }
    Console.WriteLine(outPath);
  }
  static void Draw(Graphics g, string text, float xPct, float lineYPct, float wPct, float imgW, float imgH, Font font) {
    float lineY=lineYPct*imgH, maxW=wPct*imgW;
    float boxH=Math.Max(font.GetHeight(g)*1.08f, imgH*0.015f);
    float top=lineY-boxH-1f; if(top<0)top=0;
    var rect=new RectangleF(xPct*imgW, top, maxW, boxH);
    using (var br=new SolidBrush(Color.FromArgb(20,15,10)))
    using (var sf=new StringFormat(StringFormat.GenericTypographic)) {
      sf.Alignment=StringAlignment.Center; sf.LineAlignment=StringAlignment.Far;
      sf.FormatFlags|=StringFormatFlags.NoWrap;
      Font df=font; Font sh=null;
      try {
        float size=font.Size; var m=g.MeasureString(text,df,int.MaxValue,sf);
        while(m.Width>maxW && size>12f){ size-=0.5f; if(sh!=null)sh.Dispose(); sh=new Font(font.FontFamily,size,font.Style,font.Unit); df=sh; m=g.MeasureString(text,df,int.MaxValue,sf);} 
        g.DrawString(text,df,br,rect,sf);
      } finally { if(sh!=null) sh.Dispose(); }
    }
  }
}