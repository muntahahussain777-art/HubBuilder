using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class P {
  static bool IsBlue(Color c) {
    int avg = (c.R + c.G + c.B) / 3;
    return avg < 200 && c.B > c.R + 15 && c.B > 60;
  }
  static void Main() {
    string path = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png";
    using (var bmp = new Bitmap(path)) {
      int w=bmp.Width,h=bmp.Height;
      int[] ys = {210,219,272,321,373,432,488,551};
      foreach (int y0 in ys) {
        // pick best y near
        int best=y0,bestCnt=0;
        for (int y=y0-3;y<=y0+3;y++) {
          int cnt=0;
          for (int x=0;x<w;x++) if (IsBlue(bmp.GetPixel(x,y))) cnt++;
          if (cnt>bestCnt){bestCnt=cnt;best=y;}
        }
        Console.WriteLine(string.Format("=== Y={0} pct={1:0.000} cnt={2} ===", best, (float)best/h, bestCnt));
        bool inRun=false; int start=0;
        for (int x=0;x<w;x++) {
          bool ink=false;
          for (int dy=-1;dy<=1;dy++) {
            int yy=best+dy; if (yy<0||yy>=h) continue;
            if (IsBlue(bmp.GetPixel(x,yy))) { ink=true; break; }
          }
          if (ink && !inRun){inRun=true;start=x;}
          if ((!ink||x==w-1)&&inRun){
            int end=ink&&x==w-1?x:x-1; int len=end-start+1;
            if (len>25) Console.WriteLine(string.Format("  [{0:0.000}-{1:0.000}] L={2} midX={3:0.000}", (float)start/w,(float)end/w,len, ((float)start+end)/(2f*w)));
            inRun=false;
          }
        }
      }
      // Draw seat boxes with proposed normalized rects
      // lineY is bottom of underline; text sits above
      float[] lineYs = {0.318f, 0.395f, 0.466f, 0.541f, 0.627f, 0.708f, 0.800f};
      using (var g = Graphics.FromImage(bmp))
      using (var pen = new Pen(Color.Lime, 2))
      using (var font = new Font("Arial", 9, FontStyle.Bold))
      using (var br = new SolidBrush(Color.Magenta)) {
        // ReceiptNo right, Date left on line 1
        void Box(string tag, float x, float yBottom, float ww, float hh) {
          float px=x*w, py=(yBottom-hh)*h, pw=ww*w, ph=hh*h;
          g.DrawRectangle(pen, px, py, pw, ph);
          g.DrawString(tag, font, br, px, py-12);
        }
        Box("Date", 0.05f, 0.318f, 0.18f, 0.035f);
        Box("RNo", 0.58f, 0.318f, 0.18f, 0.035f);
        Box("Name", 0.05f, 0.395f, 0.72f, 0.035f);
        Box("Father", 0.20f, 0.466f, 0.55f, 0.035f);
        Box("AmtW", 0.05f, 0.541f, 0.48f, 0.035f);
        Box("Amt", 0.62f, 0.541f, 0.20f, 0.035f);
        Box("PayDt", 0.05f, 0.627f, 0.22f, 0.035f);
        Box("PayMode", 0.32f, 0.627f, 0.35f, 0.035f);
        Box("Cat", 0.05f, 0.708f, 0.16f, 0.035f);
        Box("Blk", 0.26f, 0.708f, 0.16f, 0.035f);
        Box("Size", 0.48f, 0.708f, 0.16f, 0.035f);
        Box("Plot", 0.70f, 0.708f, 0.16f, 0.035f);
        Box("Bal", 0.38f, 0.800f, 0.20f, 0.035f);
        Box("Amt2", 0.70f, 0.800f, 0.18f, 0.035f);
        // signature intentionally not boxed
      }
      bmp.Save(@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview\receipt_seats.png", ImageFormat.Png);
      Console.WriteLine("wrote seats");
    }
  }
}
