using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

class P {
  static int Darkness(Color c) { return 255 - (c.R + c.G + c.B) / 3; }
  static bool IsLine(Color c) {
    // any non-near-white darkish pixel (blue lines on white)
    int avg = (c.R + c.G + c.B) / 3;
    return avg < 170 && (c.B >= c.R - 10);
  }
  static void Main() {
    string path = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png";
    using (var bmp = new Bitmap(path)) {
      int w = bmp.Width, h = bmp.Height;
      // sample a few known areas
      for (int y = 180; y < 520; y += 20) {
        var c = bmp.GetPixel(w/2, y);
        Console.WriteLine(string.Format("y={0} pct={1:0.000} RGB={2},{3},{4} dark={5}", y, (float)y/h, c.R,c.G,c.B, Darkness(c)));
      }
      var rowCounts = new int[h];
      for (int y = 0; y < h; y++) {
        int cnt = 0;
        for (int x = (int)(w*0.04); x < (int)(w*0.96); x++) {
          if (IsLine(bmp.GetPixel(x,y))) cnt++;
        }
        rowCounts[y] = cnt;
      }
      var peaks = new List<int>();
      for (int y = (int)(h*0.24); y < (int)(h*0.86); y++) {
        if (rowCounts[y] > w * 0.18) {
          bool max = true;
          for (int dy=-3; dy<=3; dy++) if (dy!=0 && rowCounts[y+dy] > rowCounts[y]) max = false;
          if (max) peaks.Add(y);
        }
      }
      var lines = new List<int>();
      foreach (var y in peaks) {
        if (lines.Count == 0 || y - lines[lines.Count-1] > 10) lines.Add(y);
        else if (rowCounts[y] > rowCounts[lines[lines.Count-1]]) lines[lines.Count-1] = y;
      }
      Console.WriteLine("LINES " + lines.Count);
      foreach (var y in lines) {
        Console.WriteLine(string.Format("Y={0} pct={1:0.000} ink={2}", y, (float)y/h, rowCounts[y]));
        bool inRun=false; int start=0;
        var segs = new List<string>();
        for (int x=0; x<w; x++) {
          bool ink = IsLine(bmp.GetPixel(x,y)) || (y>0 && IsLine(bmp.GetPixel(x,y-1))) || (y+1<h && IsLine(bmp.GetPixel(x,y+1)));
          if (ink && !inRun) { inRun=true; start=x; }
          if ((!ink || x==w-1) && inRun) {
            int end = ink && x==w-1 ? x : x-1;
            int len = end-start+1;
            if (len > 30) segs.Add(string.Format("[{0:0.000}-{1:0.000} L={2}]", (float)start/w, (float)end/w, len));
            inRun=false;
          }
        }
        Console.WriteLine("  " + string.Join(" ", segs));
      }
      using (var g = Graphics.FromImage(bmp))
      using (var pen = new Pen(Color.FromArgb(200,255,0,0), 1))
      using (var font = new Font("Consolas", 9, FontStyle.Bold))
      using (var br = new SolidBrush(Color.Red)) {
        int i=0;
        foreach (var y in lines) {
          g.DrawLine(pen, 0, y, w, y);
          g.DrawString("L"+(++i)+" "+((float)y/h).ToString("0.000"), font, br, 2, y-12);
        }
      }
      bmp.Save(Path.Combine(Path.GetDirectoryName(path), @"_preview\receipt_lines.png"), ImageFormat.Png);
    }
  }
}
