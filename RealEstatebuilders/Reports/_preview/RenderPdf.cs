using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

class RenderPdf {
  static int Main() {
    try { MainAsync().GetAwaiter().GetResult(); return 0; }
    catch (Exception ex) { Console.WriteLine(ex); return 1; }
  }
  static async Task MainAsync() {
    string pdfPath = @"c:\Users\M.A\Desktop\TemporaryAllotmentLetter.pdf";
    string outDir = @"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview";
    var file = await StorageFile.GetFileFromPathAsync(pdfPath);
    var doc = await PdfDocument.LoadFromFileAsync(file);
    Console.WriteLine("pages=" + doc.PageCount);
    for (uint i = 0; i < Math.Min(2u, doc.PageCount); i++) {
      using (var page = doc.GetPage(i))
      using (var stream = new InMemoryRandomAccessStream()) {
        var opts = new PdfPageRenderOptions();
        opts.DestinationWidth = 1240;
        await page.RenderToStreamAsync(stream, opts);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        Console.WriteLine("page "+i+" "+decoder.PixelWidth+"x"+decoder.PixelHeight);
        using (var outStream = new InMemoryRandomAccessStream()) {
          var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
          var soft = await decoder.GetSoftwareBitmapAsync();
          encoder.SetSoftwareBitmap(soft);
          await encoder.FlushAsync();
          string path = Path.Combine(outDir, "user_pdf_page_"+i+".png");
          using (var reader = new DataReader(outStream.GetInputStreamAt(0))) {
            await reader.LoadAsync((uint)outStream.Size);
            byte[] bytes = new byte[outStream.Size];
            reader.ReadBytes(bytes);
            File.WriteAllBytes(path, bytes);
            Console.WriteLine("wrote "+path+" "+bytes.Length);
          }
        }
      }
    }
  }
}