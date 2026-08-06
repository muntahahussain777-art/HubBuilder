using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class V {
  struct S { public string Tag,Text; public float X,Y,W; public bool Rtl; public S(string t,string v,float x,float y,float w,bool rtl=false){Tag=t;Text=v;X=x;Y=y;W=w;Rtl=rtl;} }
  static Font F(float s){
    string[] n={"Jameel Noori Nastaleeq","Urdu Typesetting","Segoe UI Semibold","Segoe UI"};
    foreach(var x in n){ try{return new Font(x,s,FontStyle.Bold,GraphicsUnit.Pixel);}catch{} }
    return new Font(FontFamily.GenericSansSerif,s,FontStyle.Bold,GraphicsUnit.Pixel);
  }
  static void Draw(Graphics g,string text,float xPct,float lineYPct,float wPct,float imgW,float imgH,Font font,Color ink,bool rtl,StringAlignment align){
    if(string.IsNullOrWhiteSpace(text)) return;
    string t=text.Trim();
    float lineY=lineYPct*imgH, maxW=wPct*imgW;
    float boxH=Math.Max(font.GetHeight(g)*1.15f, imgH*0.032f);
    float top=lineY-boxH-1f; if(top<0) top=0;
    var rect=new RectangleF(xPct*imgW, top, maxW, boxH);
    using(var br=new SolidBrush(ink))
    using(var sf=new StringFormat(StringFormat.GenericTypographic)){
      if(rtl) sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
      sf.Alignment=align; sf.LineAlignment=StringAlignment.Far; sf.Trimming=StringTrimming.None; sf.FormatFlags|=StringFormatFlags.NoWrap;
      Font df=font; Font sh=null;
      try{
        float size=font.Size; var m=g.MeasureString(t,df,int.MaxValue,sf);
        while(m.Width>maxW && size>10f){ size-=0.5f; if(sh!=null)sh.Dispose(); sh=new Font(font.FontFamily,size,font.Style,font.Unit); df=sh; m=g.MeasureString(t,df,int.MaxValue,sf); }
        g.DrawString(t,df,br,rect,sf);
      } finally { if(sh!=null) sh.Dispose(); }
    }
  }
  static void Main(){
    string tmpl=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png";
    string outp=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview\receipt_filled.png";
    using(var src=Image.FromFile(tmpl))
    using(var bmp=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))
    using(var g=Graphics.FromImage(bmp)){
      g.DrawImage(src,0,0,src.Width,src.Height);
      g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
      float w=bmp.Width,h=bmp.Height;
      float fs=Math.Max(18f,h*0.028f);
      using(var f=F(fs)){
        Color ink=Color.FromArgb(15,20,90);
        // seats tuned from line detection
        Draw(g,"31-07-2026", 0.04f, 0.318f, 0.18f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"125",         0.72f, 0.306f, 0.17f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"احمد خان",    0.10f, 0.395f, 0.42f, w,h,f,ink,true,StringAlignment.Far);
        Draw(g,"محمد خان",    0.28f, 0.466f, 0.35f, w,h,f,ink,true,StringAlignment.Far);
        Draw(g,"ایک لاکھ",    0.20f, 0.541f, 0.32f, w,h,f,ink,true,StringAlignment.Far);
        Draw(g,"100000",      0.68f, 0.541f, 0.18f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"31-07-2026",  0.05f, 0.627f, 0.20f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"کیش",         0.34f, 0.627f, 0.28f, w,h,f,ink,true,StringAlignment.Center);
        Draw(g,"A",           0.12f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"B-2",         0.30f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"120",         0.50f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"45",          0.70f, 0.708f, 0.14f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"50000",       0.40f, 0.800f, 0.18f, w,h,f,ink,false,StringAlignment.Center);
        Draw(g,"100000",      0.68f, 0.800f, 0.18f, w,h,f,ink,false,StringAlignment.Center);
        // signature intentionally blank
      }
      bmp.Save(outp, ImageFormat.Png);
      Console.WriteLine("OK "+outp);
    }
  }
}
