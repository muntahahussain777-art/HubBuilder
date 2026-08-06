using System; using System.Drawing; using System.Drawing.Drawing2D; using System.Drawing.Imaging; using System.Drawing.Text; using System.IO;
class V {
  static Font F(float s){ string[] n={"Jameel Noori Nastaleeq","Urdu Typesetting","Segoe UI Semibold","Segoe UI"}; foreach(var x in n){ try{return new Font(x,s,FontStyle.Bold,GraphicsUnit.Pixel);}catch{} } return new Font(FontFamily.GenericSansSerif,s,FontStyle.Bold,GraphicsUnit.Pixel); }
  static void Draw(Graphics g,string text,float xPct,float lineYPct,float wPct,float imgW,float imgH,Font font,Color ink,bool rtl,StringAlignment align){
    if(string.IsNullOrWhiteSpace(text)) return;
    string t=text.Trim(); float lineY=lineYPct*imgH, maxW=wPct*imgW;
    float boxH=Math.Max(font.GetHeight(g)*1.15f, imgH*0.032f); float top=lineY-boxH-1f; if(top<0) top=0;
    var rect=new RectangleF(xPct*imgW, top, maxW, boxH);
    using(var br=new SolidBrush(ink))
    using(var sf=new StringFormat(StringFormat.GenericTypographic)){
      if(rtl) sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
      sf.Alignment=align; sf.LineAlignment=StringAlignment.Far; sf.Trimming=StringTrimming.None; sf.FormatFlags|=StringFormatFlags.NoWrap;
      g.DrawString(t,font,br,rect,sf);
    }
  }
  static void Main(){
    string tmpl=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png";
    string outp=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview\receipt_name_fix.png";
    using(var src=Image.FromFile(tmpl))
    using(var bmp=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))
    using(var g=Graphics.FromImage(bmp)){
      g.DrawImage(src,0,0,src.Width,src.Height);
      g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
      float w=bmp.Width,h=bmp.Height; float fs=Math.Max(18f,h*0.028f);
      using(var f=F(fs)){
        Color ink=Color.FromArgb(15,20,90);
        Draw(g,"31-07-2026", 0.04f, 0.318f, 0.18f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"125", 0.72f, 0.306f, 0.17f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"احمد خان", 0.28f, 0.395f, 0.28f, w,h,f,ink,true,StringAlignment.Near);
        Draw(g,"محمد خان", 0.40f, 0.466f, 0.26f, w,h,f,ink,true,StringAlignment.Near);
        Draw(g,"A", 0.06f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"B-2", 0.30f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"120", 0.50f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"45", 0.70f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
      }
      bmp.Save(outp, ImageFormat.Png);
      Console.WriteLine("OK");
    }
  }
}
