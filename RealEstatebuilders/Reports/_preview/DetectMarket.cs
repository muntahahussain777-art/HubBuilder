using System;
using System.Drawing;
using System.Collections.Generic;
class D {
  static void Main() {
    using (var bmp = new Bitmap(@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaMarketBhawaniReceipt.png")) {
      int w=bmp.Width, h=bmp.Height;
      // Scan horizontal ink lines near known row Y percentages
      float[] ys = {0.293f, 0.368f, 0.438f, 0.506f, 0.584f, 0.663f, 0.750f};
      foreach (float yp in ys) {
        int y = (int)(yp * h);
        // find densest ink row nearby
        int bestY=y; int best=0;
        for (int dy=-8; dy<=8; dy++) {
          int yy=y+dy; if (yy<0||yy>=h) continue;
          int c=0;
          for (int x=0;x<w;x++) {
            Color p=bmp.GetPixel(x,yy);
            if (p.R<80 && p.B>40) c++;
          }
          if (c>best){best=c;bestY=yy;}
        }
        // find segments on bestY
        var segs=new List<string>();
        int start=-1;
        for (int x=0;x<=w;x++) {
          bool ink=false;
          if (x<w) {
            Color p=bmp.GetPixel(x,bestY);
            ink = p.R<90 && p.B>30 && p.G<100;
            // also check nearby rows for thin lines
            if (!ink) {
              for (int k=-1;k<=1 && !ink;k++) {
                int yy=bestY+k; if(yy<0||yy>=h) continue;
                Color q=bmp.GetPixel(x,yy);
                if (q.R<90 && q.B>30 && q.G<100) ink=true;
              }
            }
          }
          if (ink && start<0) start=x;
          if ((!ink || x==w) && start>=0) {
            int len=x-start;
            if (len>=25) segs.Add(string.Format("{0}-{1} ({2:P0}-{3:P0})", start, x-1, start/(float)w, (x-1)/(float)w));
            start=-1;
          }
        }
        Console.WriteLine("yPct~{0:F3} bestY={1} ({2:P1}) segs: {3}", yp, bestY, bestY/(float)h, string.Join(" | ", segs));
      }
    }
  }
}
