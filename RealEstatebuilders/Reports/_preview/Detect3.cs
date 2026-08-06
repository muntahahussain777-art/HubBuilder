using System;
using System.Collections.Generic;
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
      int w = bmp.Width, h = bmp.Height;
      var rowCounts = new int[h];
      for (int y = 0; y < h; y++) {
        int cnt = 0;
        // scan left-center area where long underlines are (avoid right labels)
        for (int x = (int)(w*0.05); x < (int)(w*0.70); x++) {
          if (IsBlue(bmp.GetPixel(x,y))) cnt++;
        }
        rowCounts[y] = cnt;
      }
      Console.WriteLine("Top rowCounts peaks (>80):");
      for (int y=(int)(h*0.25); y<(int)(h*0.85); y++) {
        if (rowCounts[y] > 80) {
          bool localMax = true;
          for (int dy=-2;dy<=2;dy++) if (dy!=0 && y+dy>=0 && y+dy<h && rowCounts[y+dy]>rowCounts[y]) localMax=false;
          if (localMax)
            Console.WriteLine(string.Format("Y={0} pct={1:0.000} cnt={2}", y, (float)y/h, rowCounts[y]));
        }
      }
      // Also dump every 5px average in body
      Console.WriteLine("--- every 5px body ---");
      for (int y=(int)(h*0.28); y<(int)(h*0.82); y+=5) {
        int max=0, my=y;
        for (int k=0;k<5;k++) if (rowCounts[y+k]>max){max=rowCounts[y+k];my=y+k;}
        if (max>40) Console.WriteLine(string.Format("around {0} bestY={1} pct={2:0.000} cnt={3}", y, my, (float)my/h, max));
      }

      // Segment analysis for candidate line Ys
      int[] cand = {211, 260, 310, 360, 373, 400, 431, 460, 482, 520, 551};
      // find better candidates from local maxima with cnt>60
      var lines = new List<int>();
      for (int y=(int)(h*0.28); y<(int)(h*0.85); y++) {
        if (rowCounts[y] < 70) continue;
        bool max=true;
        for (int dy=-4;dy<=4;dy++) if (dy!=0 && rowCounts[y+dy]>rowCounts[y]) max=false;
        if (max) {
          if (lines.Count==0 || y-lines[lines.Count-1]>12) lines.Add(y);
          else if (rowCounts[y]>rowCounts[lines[lines.Count-1]]) lines[lines.Count-1]=y;
        }
      }
      Console.WriteLine("CANDIDATE LINES: " + lines.Count);
      using (var g = Graphics.FromImage(bmp))
      using (var pen = new Pen(Color.Red,1))
      using (var font = new Font("Consolas", 8, FontStyle.Bold))
      using (var br = new SolidBrush(Color.Red)) {
        int i=0;
        foreach (var y in lines) {
          Console.WriteLine(string.Format("L{0} Y={1} pct={2:0.000} cnt={3}", ++i, y, (float)y/h, rowCounts[y]));
          // segments full width
          bool inRun=false; int start=0;
          for (int x=0;x<w;x++) {
            bool ink = IsBlue(bmp.GetPixel(x,y)) || (y>0 && IsBlue(bmp.GetPixel(x,y-1)));
            if (ink && !inRun){inRun=true;start=x;}
            if ((!ink||x==w-1)&&inRun){
              int end = ink&&x==w-1?x:x-1; int len=end-start+1;
              if (len>40) Console.WriteLine(string.Format("   seg [{0:0.000}-{1:0.000}] len={2}", (float)start/w,(float)end/w,len));
              inRun=false;
            }
          }
          g.DrawLine(pen,0,y,w,y);
          g.DrawString("L"+i+" "+((float)y/h).ToString("0.000"), font, br, 2, y-11);
        }
      }
      bmp.Save(@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview\receipt_lines2.png", ImageFormat.Png);
    }
  }
}
