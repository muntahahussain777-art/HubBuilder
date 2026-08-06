using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class DetectLines
{
    static void Main(string[] args)
    {
        string front = args.Length > 0 ? args[0] : @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterFront.png";
        string back = args.Length > 1 ? args[1] : @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\TemporaryAllotmentLetterBack.png";
        string outDir = Path.GetDirectoryName(front) + @"\_preview";
        Directory.CreateDirectory(outDir);

        Analyze(front, outDir, "front", 0.19f, 0.46f);
        Analyze(back, outDir, "back", 0.55f, 0.92f);
    }

    static void Analyze(string path, string outDir, string tag, float y0p, float y1p)
    {
        using (var src = new Bitmap(path))
        {
            int sw = 1240, sh = (int)(1240.0 * src.Height / src.Width);
            using (var bmp = new Bitmap(sw, sh, PixelFormat.Format24bppRgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(src, 0, 0, sw, sh);
                var data = bmp.LockBits(new Rectangle(0, 0, sw, sh), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                int stride = data.Stride;
                byte[] px = new byte[stride * sh];
                Marshal.Copy(data.Scan0, px, 0, px.Length);
                bmp.UnlockBits(data);

                Func<int, int, bool> ink = (x, y) =>
                {
                    int i = y * stride + x * 3;
                    int lum = (px[i] + px[i + 1] + px[i + 2]) / 3; // BGR
                    return lum < 115;
                };

                int y0 = (int)(sh * y0p), y1 = (int)(sh * y1p);
                int x0 = (int)(sw * 0.10), x1 = (int)(sw * 0.90);

                var rowHits = new List<(int y, List<(int a, int b)> runs, int total)>();
                for (int y = y0; y <= y1; y++)
                {
                    var runs = new List<(int a, int b)>();
                    int rs = -1, rl = 0;
                    for (int x = x0; x <= x1; x++)
                    {
                        bool thin = false;
                        if (ink(x, y))
                        {
                            bool a2 = y >= 2 && ink(x, y - 2);
                            bool b2 = y < sh - 2 && ink(x, y + 2);
                            thin = !a2 && !b2;
                        }
                        if (thin)
                        {
                            if (rs < 0) { rs = x; rl = 1; } else rl++;
                        }
                        else
                        {
                            if (rs >= 0 && rl >= 35) runs.Add((rs, rs + rl - 1));
                            rs = -1; rl = 0;
                        }
                    }
                    if (rs >= 0 && rl >= 35) runs.Add((rs, rs + rl - 1));
                    int total = runs.Sum(r => r.b - r.a + 1);
                    if (total >= 70) rowHits.Add((y, runs, total));
                }

                // pick local maxima every ~10px
                var picked = new List<(int y, List<(int a, int b)> runs, int total)>();
                foreach (var hit in rowHits.OrderByDescending(h => h.total))
                {
                    if (picked.Any(p => Math.Abs(p.y - hit.y) < 10)) continue;
                    picked.Add(hit);
                }
                picked = picked.OrderBy(p => p.y).ToList();

                Console.WriteLine("=== " + tag + " ===");
                using (var mark = new Bitmap(bmp))
                using (var gg = Graphics.FromImage(mark))
                using (var pen = new Pen(Color.Red, 2))
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                {
                    int idx = 0;
                    foreach (var p in picked)
                    {
                        float yp = (float)p.y / sh;
                        Console.WriteLine($"Y={p.y} Ypct={yp:F4} total={p.total}");
                        var merged = Merge(p.runs);
                        foreach (var m in merged)
                        {
                            float xa = (float)m.a / sw, xb = (float)m.b / sw, ww = (float)(m.b - m.a + 1) / sw;
                            Console.WriteLine($"  X={xa:F3}-{xb:F3} w={ww:F3}");
                            gg.DrawRectangle(pen, m.a, p.y - 12, m.b - m.a, 14);
                        }
                        gg.DrawString("#" + idx, font, Brushes.Red, 5, p.y - 14);
                        idx++;
                    }
                    mark.Save(Path.Combine(outDir, tag + "_detected.png"), ImageFormat.Png);
                }
            }
        }
    }

    static List<(int a, int b)> Merge(List<(int a, int b)> runs)
    {
        var sorted = runs.OrderBy(r => r.a).ToList();
        var m = new List<(int a, int b)>();
        foreach (var r in sorted)
        {
            if (m.Count == 0) m.Add(r);
            else
            {
                var last = m[m.Count - 1];
                if (r.a <= last.b + 12) m[m.Count - 1] = (last.a, Math.Max(last.b, r.b));
                else m.Add(r);
            }
        }
        return m;
    }
}
