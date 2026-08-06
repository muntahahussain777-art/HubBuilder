using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class P {
  static void Main() {
    string front = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterFront.png";
    string back = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterBack.png";
    string outDir = Path.GetDirectoryName(front) + @"\_preview";
    AnalyzeFront(front, outDir);
    AnalyzeBack(back, outDir);
  }

  static byte[] Lock(Bitmap bmp, out int stride, out int w, out int h) {
    w = bmp.Width; h = bmp.Height;
    var data = bmp.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    stride = data.Stride;
    byte[] px = new byte[stride * h];
    Marshal.Copy(data.Scan0, px, 0, px.Length);
    bmp.UnlockBits(data);
    return px;
  }

  static bool Ink(byte[] px, int stride, int x, int y) {
    int i = y * stride + x * 3;
    return (px[i] + px[i+1] + px[i+2]) / 3 < 125;
  }

  static void AnalyzeFront(string path, string outDir) {
    using (var src = new Bitmap(path)) {
      int sw = 1600, sh = (int)(1600.0 * src.Height / src.Width);
      using (var bmp = new Bitmap(sw, sh, PixelFormat.Format24bppRgb))
      using (var g = Graphics.FromImage(bmp)) {
        g.DrawImage(src, 0, 0, sw, sh);
        int stride, w, h; var px = Lock(bmp, out stride, out w, out h);

        // Candidate baselines: rows with moderate ink density (text lines)
        float[] targets = { 0.268f, 0.323f, 0.360f, 0.400f, 0.438f };
        using (var mark = new Bitmap(bmp))
        using (var gg = Graphics.FromImage(mark))
        using (var pen = new Pen(Color.Lime, 2))
        using (var font = new Font("Arial", 11, FontStyle.Bold)) {
          foreach (float tp in targets) {
            int y = (int)(tp * h);
            // average ink over +/-2 rows
            var colInk = new bool[w];
            for (int x = (int)(w*0.08); x < (int)(w*0.92); x++) {
              int c = 0;
              for (int dy=-2; dy<=2; dy++) if (Ink(px, stride, x, y+dy)) c++;
              colInk[x] = c >= 2;
            }
            // find paper gaps (non-ink runs) that are long = blank lines
            var gaps = new List<Tuple<int,int>>();
            int gs=-1, gl=0;
            for (int x=(int)(w*0.08); x<(int)(w*0.92); x++) {
              if (!colInk[x]) { if (gs<0){gs=x;gl=1;} else gl++; }
              else {
                if (gs>=0 && gl >= (int)(w*0.06)) gaps.Add(Tuple.Create(gs, gs+gl-1));
                gs=-1; gl=0;
              }
            }
            if (gs>=0 && gl >= (int)(w*0.06)) gaps.Add(Tuple.Create(gs, gs+gl-1));

            // Filter: a blank underline also has a thin dark stroke nearby below text
            Console.WriteLine("Ypct="+tp.ToString("F3"));
            int gi=0;
            foreach (var gap in gaps) {
              float xa=(float)gap.Item1/w, xb=(float)gap.Item2/w, ww=xb-xa;
              // check for underline darkness in gap at y+4..y+10
              int dark=0,n=0;
              for (int x=gap.Item1; x<=gap.Item2; x+=2) {
                for (int yy=y+3; yy<=y+12 && yy<h; yy++) {
                  if (Ink(px,stride,x,yy)) dark++;
                  n++;
                }
              }
              double ratio = n==0?0:(double)dark/n;
              if (ww < 0.08 && ratio < 0.02) continue; // skip tiny empty spaces without underline
              if (ratio < 0.015 && ww < 0.12) continue;
              Console.WriteLine("  gap#"+gi+" X="+xa.ToString("F3")+"-"+xb.ToString("F3")+" w="+ww.ToString("F3")+" under="+ratio.ToString("F3"));
              gg.DrawRectangle(pen, gap.Item1, y-16, gap.Item2-gap.Item1, 18);
              gg.DrawString(tp.ToString("F2")+"#"+gi, font, Brushes.Blue, gap.Item1, y-32);
              gi++;
            }
          }
          mark.Save(Path.Combine(outDir, "front_gaps.png"), ImageFormat.Png);
        }
      }
    }
  }

  static void AnalyzeBack(string path, string outDir) {
    using (var src = new Bitmap(path)) {
      int sw = 1600, sh = (int)(1600.0 * src.Height / src.Width);
      using (var bmp = new Bitmap(sw, sh, PixelFormat.Format24bppRgb))
      using (var g = Graphics.FromImage(bmp)) {
        g.DrawImage(src, 0, 0, sw, sh);
        int stride,w,h; var px=Lock(bmp,out stride,out w,out h);
        Console.WriteLine("=== BACK scan for NIC line ===");
        // find long thin underlines
        for (int y=(int)(h*0.60); y<(int)(h*0.90); y++) {
          int run=0, best=0, bx0=0,bx1=0, rs=-1;
          for (int x=(int)(w*0.08); x<(int)(w*0.92); x++) {
            bool thin=false;
            if (Ink(px,stride,x,y)) {
              bool a2=y>=2 && Ink(px,stride,x,y-2);
              bool b2=y<h-2 && Ink(px,stride,x,y+2);
              thin = !a2 && !b2;
            }
            if (thin) { if(rs<0){rs=x;run=1;} else run++; }
            else { if(run>best){best=run;bx0=rs;bx1=rs+run-1;} rs=-1;run=0; }
          }
          if (best > (int)(w*0.25)) {
            Console.WriteLine("Ypct="+((float)y/h).ToString("F4")+" X="+((float)bx0/w).ToString("F3")+"-"+((float)bx1/w).ToString("F3")+" w="+((float)best/w).ToString("F3"));
            y += 8;
          }
        }
      }
    }
  }
}
