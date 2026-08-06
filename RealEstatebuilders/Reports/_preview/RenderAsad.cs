using System; using System.IO; using System.Threading.Tasks;
using Windows.Data.Pdf; using Windows.Storage; using Windows.Storage.Streams; using Windows.Graphics.Imaging;
class R {
  static int Main(){ try{ MainAsync().GetAwaiter().GetResult(); return 0;} catch(Exception ex){ Console.WriteLine(ex); return 1;} }
  static async Task MainAsync(){
    string pdf=@"c:\Users\M.A\Desktop\asad.pdf";
    string outDir=@"F:\2026\Ithad Village\Complete iss par kam karna he neche wala par anhi\RealEstatebuilders\RealEstatebuilders\Reports\_preview";
    var file=await StorageFile.GetFileFromPathAsync(pdf);
    var doc=await PdfDocument.LoadFromFileAsync(file);
    Console.WriteLine("pages="+doc.PageCount);
    for(uint i=0;i<Math.Min(2u,doc.PageCount);i++){
      using(var page=doc.GetPage(i))
      using(var stream=new InMemoryRandomAccessStream()){
        var opts=new PdfPageRenderOptions(); opts.DestinationWidth=1400;
        await page.RenderToStreamAsync(stream, opts);
        var decoder=await BitmapDecoder.CreateAsync(stream);
        using(var outStream=new InMemoryRandomAccessStream()){
          var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
          encoder.SetSoftwareBitmap(await decoder.GetSoftwareBitmapAsync());
          await encoder.FlushAsync();
          using(var reader=new DataReader(outStream.GetInputStreamAt(0))){
            await reader.LoadAsync((uint)outStream.Size);
            byte[] bytes=new byte[outStream.Size]; reader.ReadBytes(bytes);
            string path=Path.Combine(outDir,"asad_page_"+i+".png");
            File.WriteAllBytes(path, bytes); Console.WriteLine("wrote "+path+" "+bytes.Length);
          }
        }
      }
    }
  }
}