using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class Diff2 {
  static void Main() {
    string reports = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports";
    string outDir = reports + @"\_preview";
    string tmpl = Path.Combine(reports, "TemporaryAllotmentLetterFront.png");
    string pdf = Path.Combine(outDir, "user_pdf_page_0.png");
    int W=1000, H=(int)(1000.0*3508/2480);
    using (var t = Load(tmpl,W,H))
    using (var p = Load(pdf,W,H))
    using (var mask = new Bitmap(W,H,PixelFormat.Format24bppRgb)) {
      var td=Lock(t); var pd=Lock(p); var md=Lock(mask);
      var pts = new List<Point>();
      for (int y=(int)(H*0.22); y<(int)(H*0.48); y++) {
        for (int x=(int)(W*0.08); x<(int)(W*0.92); x++) {
          int ti=y*td.s+x*3, pi=y*pd.s+x*3;
          int tL=(td.b[ti]+td.b[ti+1]+td.b[ti+2])/3;
          int pL=(pd.b[pi]+pd.b[pi+1]+pd.b[pi+2])/3;
          // Strong: PDF much darker, and template fairly light (blank area)
          bool hit = (tL - pL) > 55 && pL < 90 && tL > 150;
          int mi=y*md.s+x*3;
          md.b[mi]=md.b[mi+1]=md.b[mi+2]=(byte)(hit?0:255);
          if (hit) pts.Add(new Point(x,y));
        }
      }
      Unlock(mask, md); Unlock(t, td); Unlock(p, pd);

      // Connected components via simple Y-band + X clustering
      var bands = pts.GroupBy(pt => pt.Y / 6).OrderBy(g => g.Key).ToList();
      Console.WriteLine("overlay ink bands:");
      using (var mark = new Bitmap(p))
      using (var g = Graphics.FromImage(mark))
      using (var pen = new Pen(Color.Lime, 2))
      using (var font = new Font("Arial", 10, FontStyle.Bold)) {
        var fields = new List<RectangleF>();
        foreach (var band in bands) {
          if (band.Count() < 25) continue;
          int minX=band.Min(z=>z.X), maxX=band.Max(z=>z.X);
          int minY=band.Min(z=>z.Y), maxY=band.Max(z=>z.Y);
          float wp=(maxX-minX)/(float)W;
          if (wp < 0.04f) continue;
          // split band into x-clusters if gap
          var xs = band.Select(z=>z.X).OrderBy(z=>z).ToList();
          var clusters = new List<List<int>>();
          var cur = new List<int>{xs[0]};
          for (int i=1;i<xs.Count;i++) {
            if (xs[i]-xs[i-1] > 18) { clusters.Add(cur); cur=new List<int>(); }
            cur.Add(xs[i]);
          }
          clusters.Add(cur);
          foreach (var c in clusters) {
            if (c.Count < 20) continue;
            int x0=c.Min(), x1=c.Max();
            float xpct=x0/(float)W, wpct=(x1-x0)/(float)W, ymid=(minY+maxY)/2f/H, ybase=maxY/(float)H;
            if (wpct < 0.04f) continue;
            Console.WriteLine(string.Format("FIELD yMid={0:F3} yBase={1:F3} x={2:F3} w={3:F3}", ymid, ybase, xpct, wpct));
            g.DrawRectangle(pen, x0, minY, x1-x0, Math.Max(2,maxY-minY));
            g.DrawString(ymid.ToString("0.000"), font, Brushes.Red, x0, minY-14);
          }
        }
        // crop
        int y0=(int)(H*0.22), ch=(int)(H*0.28);
        using (var crop = mark.Clone(new Rectangle(0,y0,W,ch), mark.PixelFormat)) {
          crop.Save(Path.Combine(outDir, "overlay_locate.png"), ImageFormat.Png);
        }
        mask.Save(Path.Combine(outDir, "overlay_mask.png"), ImageFormat.Png);
      }
    }
  }
  static Bitmap Load(string path,int w,int h){ using(var s=new Bitmap(path)){ var b=new Bitmap(w,h,PixelFormat.Format24bppRgb); using(var g=Graphics.FromImage(b)) g.DrawImage(s,0,0,w,h); return b; } }
  class D { public byte[] b; public int s; public BitmapData d; public Bitmap bmp; }
  static D Lock(Bitmap bmp){ var d=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); var b=new byte[d.Stride*bmp.Height]; Marshal.Copy(d.Scan0,b,0,b.Length); return new D{b=b,s=d.Stride,d=d,bmp=bmp}; }
  static void Unlock(Bitmap bmp, D p){ Marshal.Copy(p.b,0,p.d.Scan0,p.b.Length); bmp.UnlockBits(p.d); }
}