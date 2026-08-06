using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

class P {
  static void Main() {
    string back = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterBack.png";
    string outDir = Path.GetDirectoryName(back) + @"\_preview";
    using (var src = new Bitmap(back)) {
      int sw=900, sh=(int)(900.0*src.Height/src.Width);
      using (var bmp = new Bitmap(sw,sh,PixelFormat.Format24bppRgb))
      using (var g = Graphics.FromImage(bmp)) {
        g.DrawImage(src,0,0,sw,sh);
        var data = bmp.LockBits(new Rectangle(0,0,sw,sh), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        int stride=data.Stride; byte[] px=new byte[stride*sh];
        Marshal.Copy(data.Scan0, px, 0, px.Length); bmp.UnlockBits(data);
        Console.WriteLine("Back size full "+src.Width+"x"+src.Height);
        // Find rows with "long" dark content (text) vs empty
        for (int y=0; y<sh; y+=4) {
          int dark=0,n=0;
          for (int x=(int)(sw*0.15); x<(int)(sw*0.85); x+=3) {
            int i=y*stride+x*3; int lum=(px[i]+px[i+1]+px[i+2])/3;
            if (lum<100) dark++; n++;
          }
          double r=(double)dark/n;
          if (r>0.04) Console.WriteLine("content Ypct="+((float)y/sh).ToString("F3")+" dens="+r.ToString("F3"));
        }
      }
      // Also save annotated full back with horizontal guides at 0.05 intervals from 0.4 to 0.95
      using (var full = new Bitmap(700, (int)(700.0*src.Height/src.Width)))
      using (var g = Graphics.FromImage(full))
      using (var pen = new Pen(Color.FromArgb(150,255,0,0),1))
      using (var font = new Font("Arial",8)) {
        g.DrawImage(src,0,0,full.Width,full.Height);
        for (float p=0.40f; p<=0.95f; p+=0.05f) {
          float yy=p*full.Height;
          g.DrawLine(pen,0,yy,full.Width,yy);
          g.DrawString(p.ToString("0.00"), font, Brushes.Red, 2, yy-10);
        }
        // mark current NIC seats
        Mark(g, full.Width, full.Height, 0.18f, 0.745f, 0.55f, "OLD");
        Mark(g, full.Width, full.Height, 0.18f, 0.78f, 0.55f, "A");
        Mark(g, full.Width, full.Height, 0.20f, 0.82f, 0.52f, "B");
        Mark(g, full.Width, full.Height, 0.22f, 0.72f, 0.50f, "C");
        full.Save(Path.Combine(outDir,"back_guides.png"), ImageFormat.Png);
      }
    }
  }
  static void Mark(Graphics g, int w, int h, float x, float y, float ww, string t) {
    using (var pen = new Pen(Color.Blue,2))
    using (var font = new Font("Arial",9,FontStyle.Bold)) {
      g.DrawRectangle(pen, x*w, y*h-16, ww*w, 16);
      g.DrawString(t, font, Brushes.Blue, x*w, y*h-30);
    }
  }
}