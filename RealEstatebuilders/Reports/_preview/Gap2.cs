using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class Gap {
  static void Main() {
    string front = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterFront.png";
    string back = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterBack.png";
    string outDir = Path.GetDirectoryName(front) + @"\_preview";
    Scan(front, outDir, "front", new float[]{0.270f,0.275f,0.280f, 0.320f,0.325f,0.330f,0.335f, 0.358f,0.363f,0.368f, 0.395f,0.400f,0.405f, 0.430f,0.435f,0.440f,0.445f});
    Scan(back, outDir, "back", new float[]{0.505f,0.510f,0.515f,0.520f,0.525f});
  }
  static void Scan(string path, string outDir, string tag, float[] ys) {
    using (var src = new Bitmap(path)) {
      int W=1800, H=(int)(1800.0*src.Height/src.Width);
      using (var bmp = new Bitmap(W,H,PixelFormat.Format24bppRgb))
      using (var g = Graphics.FromImage(bmp)) {
        g.DrawImage(src,0,0,W,H);
        var data = bmp.LockBits(new Rectangle(0,0,W,H), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        byte[] px = new byte[data.Stride*H];
        Marshal.Copy(data.Scan0, px, 0, px.Length);
        int stride=data.Stride;
        bmp.UnlockBits(data);
        Func<int,int,int> lum = (x,y) => { int i=y*stride+x*3; return (px[i]+px[i+1]+px[i+2])/3; };

        using (var mark = new Bitmap(bmp))
        using (var gg = Graphics.FromImage(mark))
        using (var pen = new Pen(Color.Lime, 3))
        using (var font = new Font("Arial", 11, FontStyle.Bold)) {
          Console.WriteLine("=== "+tag+" ===");
          foreach (float yp in ys) {
            int y = (int)(yp*H);
            // paper gap = high luminance across a band around y (text line height)
            bool[] paper = new bool[W];
            for (int x=(int)(W*0.08); x<(int)(W*0.92); x++) {
              int dark=0,n=0;
              for (int dy=-8; dy<=2; dy++) { // look at glyph body above the underline
                int yy=y+dy; if(yy<0||yy>=H) continue;
                if (lum(x,yy) < 125) dark++;
                n++;
              }
              paper[x] = n>0 && ((double)dark/n) < 0.12; // mostly empty = blank gap
            }
            var gaps = new List<int[]>();
            int gs=-1;
            for (int x=(int)(W*0.08); x<(int)(W*0.92); x++) {
              if (paper[x]) { if (gs<0) gs=x; }
              else {
                if (gs>=0) {
                  int ge=x-1; int len=ge-gs+1;
                  if (len >= (int)(W*0.06)) gaps.Add(new[]{gs,ge,len});
                  gs=-1;
                }
              }
            }
            if (gs>=0) { int ge=(int)(W*0.92)-1; if (ge-gs+1>=(int)(W*0.06)) gaps.Add(new[]{gs,ge,ge-gs+1}); }

            // also require some underline darkness near y
            var good = new List<int[]>();
            foreach (var gap in gaps) {
              int dark=0,n=0;
              for (int x=gap[0]; x<=gap[1]; x+=2)
                for (int dy=0; dy<=6; dy++) {
                  if (lum(x, Math.Min(H-1,y+dy)) < 140) dark++;
                  n++;
                }
              double r = n==0?0:(double)dark/n;
              if (r > 0.02 || gap[2] > W*0.15) good.Add(gap);
            }
            Console.WriteLine("Y="+yp.ToString("F3"));
            int idx=0;
            foreach (var gap in good.OrderByDescending(z=>z[2]).Take(4)) {
              float x0=(float)gap[0]/W, x1=(float)gap[1]/W, ww=x1-x0;
              Console.WriteLine("  blank X="+x0.ToString("F3")+"-"+x1.ToString("F3")+" w="+ww.ToString("F3"));
              gg.DrawRectangle(pen, gap[0], y-18, gap[1]-gap[0], 20);
              gg.DrawString(yp.ToString("0.000")+"#"+idx, font, Brushes.Blue, gap[0], y-34);
              idx++;
            }
          }
          mark.Save(Path.Combine(outDir, tag+"_blanks.png"), ImageFormat.Png);
        }
      }
    }
  }
}