using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class Align {
  static void Main() {
    string reports = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports";
    string outDir = reports + @"\_preview";
    string frontT = Path.Combine(reports, "TemporaryAllotmentLetterFront.png");
    string backT = Path.Combine(reports, "TemporaryAllotmentLetterBack.png");
    string pdf0 = Path.Combine(outDir, "user_pdf_page_0.png");
    string pdf1 = Path.Combine(outDir, "user_pdf_page_1.png");

    // Scale PDF page to template size and diff to find overlay ink
    FindOverlayDiff(frontT, pdf0, outDir, "front");
    FindOverlayDiff(backT, pdf1, outDir, "back");

    // Detect blank underlines on template more carefully in field band
    DetectUnderlines(frontT, outDir, "frontUL", 0.20f, 0.48f);
    DetectUnderlines(backT, outDir, "backUL", 0.40f, 0.60f);
  }

  static Bitmap LoadScaled(string path, int w, int h) {
    using (var src = new Bitmap(path)) {
      var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
      using (var g = Graphics.FromImage(bmp)) {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(src, 0, 0, w, h);
      }
      return bmp;
    }
  }

  static void FindOverlayDiff(string templatePath, string pdfPagePath, string outDir, string tag) {
    using (var tmplFull = new Bitmap(templatePath)) {
      int W = 1240, H = (int)(1240.0 * tmplFull.Height / tmplFull.Width);
      using (var tmpl = LoadScaled(templatePath, W, H))
      using (var pdf = LoadScaled(pdfPagePath, W, H))
      using (var diff = new Bitmap(W, H, PixelFormat.Format24bppRgb)) {
        var td = Lock(tmpl); var pd = Lock(pdf); var dd = Lock(diff);
        var clusters = new List<Point>();
        for (int y = 0; y < H; y++) {
          for (int x = 0; x < W; x++) {
            int ti = y * td.stride + x * 3;
            int pi = y * pd.stride + x * 3;
            int tLum = (td.px[ti] + td.px[ti+1] + td.px[ti+2]) / 3;
            int pLum = (pd.px[pi] + pd.px[pi+1] + pd.px[pi+2]) / 3;
            // overlay text: pdf darker than template significantly
            bool added = (tLum - pLum) > 45 && pLum < 110;
            int v = added ? 0 : 255;
            int di = y * dd.stride + x * 3;
            dd.px[di] = dd.px[di+1] = dd.px[di+2] = (byte)v;
            if (added) clusters.Add(new Point(x, y));
          }
        }
        Unlock(diff, dd); Unlock(tmpl, td); Unlock(pdf, pd);

        // Cluster into connected components roughly by Y bands
        var byY = clusters.GroupBy(p => p.Y / 8).OrderBy(g => g.Key).ToList();
        Console.WriteLine("=== DIFF " + tag + " ===");
        using (var mark = new Bitmap(tmpl))
        using (var g = Graphics.FromImage(mark))
        using (var pen = new Pen(Color.Lime, 2))
        using (var font = new Font("Arial", 10, FontStyle.Bold)) {
          foreach (var band in byY) {
            if (band.Count() < 30) continue;
            int minX = band.Min(p => p.X), maxX = band.Max(p => p.X);
            int minY = band.Min(p => p.Y), maxY = band.Max(p => p.Y);
            int cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            float x0 = (float)minX / W, x1 = (float)maxX / W, yp = (float)maxY / H, ymid = (float)cy / H;
            if ((x1 - x0) < 0.03f) continue;
            Console.WriteLine(string.Format("textBand Ymid={0:F4} Ybase~={1:F4} X={2:F3}-{3:F3} w={4:F3} pixels={5}",
              ymid, yp, x0, x1, x1 - x0, band.Count()));
            g.DrawRectangle(pen, minX, minY, maxX - minX, Math.Max(1, maxY - minY));
          }
          mark.Save(Path.Combine(outDir, tag + "_diff_mark.png"), ImageFormat.Png);
          diff.Save(Path.Combine(outDir, tag + "_diff.png"), ImageFormat.Png);

          // Crop field area
          int y0 = (int)(H * (tag == "front" ? 0.18f : 0.40f));
          int ch = (int)(H * (tag == "front" ? 0.30f : 0.25f));
          using (var crop = mark.Clone(new Rectangle(0, y0, W, ch), mark.PixelFormat)) {
            crop.Save(Path.Combine(outDir, tag + "_diff_crop.png"), ImageFormat.Png);
          }
        }
      }
    }
  }

  static void DetectUnderlines(string path, string outDir, string tag, float y0p, float y1p) {
    using (var src = new Bitmap(path)) {
      int W = 1600, H = (int)(1600.0 * src.Height / src.Width);
      using (var bmp = LoadScaled(path, W, H)) {
        var d = Lock(bmp);
        Console.WriteLine("=== UNDERLINES " + tag + " ===");
        var hits = new List<Tuple<int,int,int,int>>(); // y,x0,x1,score
        for (int y = (int)(H * y0p); y <= (int)(H * y1p); y++) {
          int rs = -1, rl = 0, best = 0, bx0 = 0, bx1 = 0;
          var runs = new List<Tuple<int,int>>();
          for (int x = (int)(W * 0.10); x < (int)(W * 0.90); x++) {
            bool ink = Lum(d, x, y) < 120;
            bool thin = false;
            if (ink) {
              bool up = Lum(d, x, y - 2) >= 130;
              bool dn = Lum(d, x, Math.Min(H - 1, y + 2)) >= 130;
              thin = up && dn;
            }
            if (thin) { if (rs < 0) { rs = x; rl = 1; } else rl++; }
            else {
              if (rs >= 0 && rl >= 40) runs.Add(Tuple.Create(rs, rs + rl - 1));
              rs = -1; rl = 0;
            }
          }
          if (rs >= 0 && rl >= 40) runs.Add(Tuple.Create(rs, rs + rl - 1));
          // merge
          if (runs.Count == 0) continue;
          var merged = new List<Tuple<int,int>>();
          foreach (var r in runs.OrderBy(t => t.Item1)) {
            if (merged.Count == 0) merged.Add(r);
            else {
              var last = merged[merged.Count - 1];
              if (r.Item1 <= last.Item2 + 10) merged[merged.Count - 1] = Tuple.Create(last.Item1, Math.Max(last.Item2, r.Item2));
              else merged.Add(r);
            }
          }
          int total = merged.Sum(m => m.Item2 - m.Item1 + 1);
          if (total < 60) continue;
          foreach (var m in merged) {
            if (m.Item2 - m.Item1 < 40) continue;
            hits.Add(Tuple.Create(y, m.Item1, m.Item2, m.Item2 - m.Item1));
          }
        }
        // pick local maxima
        var picked = new List<Tuple<int,int,int,int>>();
        foreach (var h in hits.OrderByDescending(t => t.Item4)) {
          if (picked.Any(p => Math.Abs(p.Item1 - h.Item1) < 8 && !(h.Item2 > p.Item3 + 20 || h.Item3 < p.Item2 - 20))) {
            // same-ish line segment - keep longer
            continue;
          }
          if (picked.Count(p => Math.Abs(p.Item1 - h.Item1) < 8) >= 4) continue;
          // allow multiple segments on same y
          bool nearY = picked.Any(p => Math.Abs(p.Item1 - h.Item1) < 6 && Math.Abs(((p.Item2+p.Item3)/2) - ((h.Item2+h.Item3)/2)) < 30);
          if (nearY) continue;
          picked.Add(h);
        }
        using (var mark = new Bitmap(bmp))
        using (var g = Graphics.FromImage(mark))
        using (var pen = new Pen(Color.Red, 2))
        using (var font = new Font("Arial", 9, FontStyle.Bold)) {
          foreach (var p in picked.OrderBy(t => t.Item1).ThenBy(t => t.Item2)) {
            float yp = (float)p.Item1 / H;
            float x0 = (float)p.Item2 / W, x1 = (float)p.Item3 / W;
            Console.WriteLine(string.Format("UL Y={0:F4} X={1:F3}-{2:F3} w={3:F3}", yp, x0, x1, x1-x0));
            g.DrawLine(pen, p.Item2, p.Item1, p.Item3, p.Item1);
            g.DrawString(yp.ToString("0.000"), font, Brushes.Blue, p.Item2, p.Item1 - 14);
          }
          mark.Save(Path.Combine(outDir, tag + ".png"), ImageFormat.Png);
        }
        Unlock(bmp, d);
      }
    }
  }

  class Pix { public byte[] px; public int stride; public BitmapData data; public Bitmap bmp; }
  static Pix Lock(Bitmap bmp) {
    var data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
    var px = new byte[data.Stride * bmp.Height];
    Marshal.Copy(data.Scan0, px, 0, px.Length);
    return new Pix { px = px, stride = data.Stride, data = data, bmp = bmp };
  }
  static void Unlock(Bitmap bmp, Pix p) {
    Marshal.Copy(p.px, 0, p.data.Scan0, p.px.Length);
    bmp.UnlockBits(p.data);
  }
  static int Lum(Pix p, int x, int y) {
    if (x < 0 || y < 0) return 255;
    int i = y * p.stride + x * 3;
    return (p.px[i] + p.px[i+1] + p.px[i+2]) / 3;
  }
}