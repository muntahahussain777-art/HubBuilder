using System; using System.Drawing; using System.Drawing.Imaging; using System.Drawing.Text; using System.IO;
class V {
  static void Main() {
    string reports=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports";
    string outDir=reports+@"\_preview";
    // User mapping: Date LEFT-top; Name after muhtram; Father; Mem; Plot; Area; side/loc underline; NIC
    var seats = new[] {
      S("DATE","30-07-2026", 0.22f, 0.272f, 0.28f),
      S("NAME","asad", 0.40f, 0.328f, 0.28f),
      S("FATH","father", 0.12f, 0.328f, 0.20f),
      S("MEM","member", 0.42f, 0.362f, 0.32f),
      S("CAT","cat", 0.18f, 0.362f, 0.16f),
      S("PLOT","plot", 0.68f, 0.398f, 0.12f),
      S("AREA","area", 0.48f, 0.398f, 0.14f),
      S("LOC","location", 0.35f, 0.438f, 0.32f),
    };
    Compose(Path.Combine(reports,"TemporaryAllotmentLetterFront.png"), Path.Combine(outDir,"map_front.png"), seats, 0.20f, 0.28f);
    Compose(Path.Combine(reports,"TemporaryAllotmentLetterBack.png"), Path.Combine(outDir,"map_back.png"), new[]{ S("NIC","nic no main", 0.18f, 0.515f, 0.48f)}, 0.42f, 0.20f);
  }
  class Seat{public string Tag,Text;public float X,Y,W;}
  static Seat S(string t,string te,float x,float y,float w){return new Seat{Tag=t,Text=te,X=x,Y=y,W=w};}
  static void Compose(string template,string outPath,Seat[] seats,float y0p,float hp){
    using(var src=Image.FromFile(template)) using(var bmp=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(bmp)){
      g.DrawImage(src,0,0,src.Width,src.Height); g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
      float w=bmp.Width,h=bmp.Height; float fs=Math.Max(28f,h*0.0145f);
      using(var font=new Font("Segoe UI Semibold",fs,FontStyle.Bold,GraphicsUnit.Pixel))
      using(var red=new Pen(Color.FromArgb(200,220,0,0),3))
      using(var tagFont=new Font("Arial",Math.Max(14f,h*0.007f),FontStyle.Bold,GraphicsUnit.Pixel)){
        foreach(var s in seats){
          float lineY=s.Y*h,maxW=s.W*w,boxH=Math.Max(font.GetHeight(g)*1.08f,h*0.016f),top=lineY-boxH-1f;
          var rect=new RectangleF(s.X*w,top,maxW,boxH);
          using(var br=new SolidBrush(Color.FromArgb(20,15,10))) using(var sf=new StringFormat(StringFormat.GenericTypographic)){ sf.Alignment=StringAlignment.Center; sf.LineAlignment=StringAlignment.Far; sf.FormatFlags|=StringFormatFlags.NoWrap; g.DrawString(s.Text,font,br,rect,sf); }
          g.DrawRectangle(red,s.X*w,lineY-h*0.016f,s.W*w,h*0.016f); g.DrawString(s.Tag,tagFont,Brushes.Red,s.X*w,lineY-h*0.028f);
        }
      }
      int y0=(int)(h*y0p),ch=(int)(h*hp),x0=(int)(w*0.05),cw=(int)(w*0.90);
      using(var c=bmp.Clone(new Rectangle(x0,y0,cw,ch),bmp.PixelFormat)){ int tw=1600; int th=(int)(c.Height*(tw/(double)c.Width)); using(var thumb=new Bitmap(tw,th)) using(var gg=Graphics.FromImage(thumb)){ gg.DrawImage(c,0,0,tw,th); thumb.Save(outPath,ImageFormat.Png);} }
    }
    Console.WriteLine(outPath);
  }
}