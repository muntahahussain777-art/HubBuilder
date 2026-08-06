using System;
using System.Drawing;
class P {
  static bool IsBlue(Color c) {
    int avg = (c.R + c.G + c.B) / 3;
    return avg < 210 && c.B > c.R + 10 && c.B > 50;
  }
  static void Main() {
    using (var bmp = new Bitmap(@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\LasbelaVillageReceipt.png")) {
      int w=bmp.Width,h=bmp.Height;
      // For each target y, dump blue density across x in bins of 20px
      int[] ys={211,219,272,321,373,432,485,551};
      foreach(int y0 in ys){
        Console.WriteLine("Y="+y0+" pct="+((float)y0/h).ToString("0.000"));
        for(int bx=0;bx<w;bx+=32){
          int cnt=0;
          for(int x=bx;x<Math.Min(bx+32,w);x++)
            for(int dy=-1;dy<=1;dy++)
              if(IsBlue(bmp.GetPixel(x,y0+dy))) cnt++;
          if(cnt>8) Console.Write(string.Format(" {0:0.00}:{1}", (float)bx/w, cnt));
        }
        Console.WriteLine();
      }
    }
  }
}
