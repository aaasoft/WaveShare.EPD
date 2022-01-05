//EPD Device Wiki: https://www.waveshare.net/wiki/2.9inch_e-Paper_Module
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using WaveShare.EPD;


//init font
FontCollection collection = new FontCollection();
FontFamily family = collection.Install("simhei.ttf");
Font font24 = family.CreateFont(24);

Console.WriteLine("epd2in9 Demo");
using (var epd = new EPD_2in9(
    System.Device.Gpio.PinNumberingScheme.Logical,
    new System.Device.Gpio.Drivers.RaspberryPi3Driver()))
{
    Console.WriteLine("init and Clear");
    epd.Init();
    epd.Clear(0xFF);

    Console.WriteLine("1.Drawing on the Horizontal image...");
    var Himage = new Image<Rgba32>(epd.Height, epd.Width);
    Himage.Mutate(x =>
    {
        x.DrawText("hello world", font24, Color.Black, new PointF(10, 0));
        x.DrawText("2.9inch e-Paper", font24, Color.Black, new PointF(10, 20));
        x.DrawText("微雪电子", font24, Color.Black, new PointF(150, 0));
        x.DrawLines(Color.Black, 1, new PointF(20, 50), new PointF(70, 100));
        x.DrawLines(Color.Black, 1, new PointF(70, 50), new PointF(20, 100));
        x.DrawPolygon(Color.Black, 1, new PointF(80, 50), new PointF(130, 50), new PointF(130, 100), new PointF(80, 100));
        x.DrawLines(Color.Black, 1, new PointF(165, 50), new PointF(165, 100));
        x.DrawLines(Color.Black, 1, new PointF(140, 75), new PointF(190, 75));
        x.FillPolygon(Color.Black, new PointF(200, 50), new PointF(250, 50), new PointF(250, 100), new PointF(200, 100));
    });
    epd.Display(Himage);
    Thread.Sleep(2);

    Console.WriteLine("Done...");
}