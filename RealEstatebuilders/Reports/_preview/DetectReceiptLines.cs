using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

class P {
  static bool IsInk(Color c) {
    // dark blue-ish on white
    return c.R < 80 && c.G < 100 && c.B > 80 && c.B < 200 && (c.B - c.R) > 20;
  }
  static void Main() {
    string path = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png";
    using (var bmp = new Bitmap(path)) {
      int w = bmp.Width, h = bmp.Height;
      Console.WriteLine("SIZE " + w + "x" + h);
      // For each y, count ink pixels in middle band (exclude header/footer extremes)
      var rowCounts = new int[h];
      for (int y = 0; y < h; y++) {
        int cnt = 0;
        for (int x = (int)(w*0.05); x < (int)(w*0.95); x++) {
          if (IsInk(bmp.GetPixel(x,y))) cnt++;
        }
        rowCounts[y] = cnt;
      }
      // Find horizontal line peaks: rows with many ink pixels that are thin bands
      var peaks = new List<int>();
      for (int y = (int)(h*0.22); y < (int)(h*0.88); y++) {
        if (rowCounts[y] > w * 0.25) {
          // local max in +/- 2
          bool max = true;
          for (int dy=-2; dy<=2; dy++) if (dy!=0 && y+dy>=0 && y+dy<h && rowCounts[y+dy] > rowCounts[y]) max = false;
          if (max) peaks.Add(y);
        }
      }
      // Merge nearby peaks
      var lines = new List<int>();
      foreach (var y in peaks) {
        if (lines.Count == 0 || y - lines[lines.Count-1] > 8) lines.Add(y);
        else if (rowCounts[y] > rowCounts[lines[lines.Count-1]]) lines[lines.Count-1] = y;
      }
      Console.WriteLine("LINES " + lines.Count);
      foreach (var y in lines) {
        Console.WriteLine(string.Format("Y={0} pct={1:0.000} ink={2}", y, (float)y/h, rowCounts[y]));
        // find underline segments: contiguous ink runs
        bool inRun=false; int start=0;
        var segs = new List<string>();
        for (int x=0; x<w; x++) {
          bool ink = IsInk(bmp.GetPixel(x,y)) || (y>0 && IsInk(bmp.GetPixel(x,y-1))) || (y+1<h && IsInk(bmp.GetPixel(x,y+1)));
          if (ink && !inRun) { inRun=true; start=x; }
          if ((!ink || x==w-1) && inRun) {
            int end = ink && x==w-1 ? x : x-1;
            int len = end-start+1;
            if (len > 25) segs.Add(string.Format("[{0:0.000}-{1:0.000}]", (float)start/w, (float)end/w));
            inRun=false;
          }
        }
        Console.WriteLine("  SEGS " + string.Join(" ", segs));
      }

      // Draw debug overlay
      using (var g = Graphics.FromImage(bmp))
      using (var pen = new Pen(Color.Red, 1))
      using (var font = new Font("Arial", 10, FontStyle.Bold))
      using (var br = new SolidBrush(Color.Red)) {
        int i=0;
        foreach (var y in lines) {
          g.DrawLine(pen, 0, y, w, y);
          g.DrawString("L"+(++i)+" y="+((float)y/h).ToString("0.000"), font, br, 5, y-14);
        }
      }
      bmp.Save(Path.Combine(Path.GetDirectoryName(path), @"_preview\receipt_lines.png"), ImageFormat.Png);
      Console.WriteLine("WROTE receipt_lines.png");
    }
  }
}
