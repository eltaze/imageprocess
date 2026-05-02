using Emgu.CV;
using Tesseract;
using Emgu.CV.Bitmap;
public class OcrEngine
{
    private readonly TesseractEngine _engine;

    public OcrEngine(string tessDataPath)
    {
        _engine = new TesseractEngine(tessDataPath, "ara+eng", EngineMode.LstmOnly);

        // 🔥 Critical tuning
        _engine.SetVariable("tessedit_do_invert", "1");
        _engine.SetVariable("textord_heavy_nr", "1");
        _engine.SetVariable("preserve_interword_spaces", "1");
    }

    public string ExtractText(Mat image)
    {
        using var bmp = image.ToBitmap();
        using var pix = PixConverter.ToPix(bmp);
        using var page = _engine.Process(pix, PageSegMode.Auto);

        return page.GetText();
    }
}