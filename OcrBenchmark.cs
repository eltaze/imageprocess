using Emgu.CV;

public class DocumentOcrPipeline
{
    private readonly ImageCleaner _cleaner;
    private readonly OcrEngine _ocr;

    public DocumentOcrPipeline(string tessDataPath)
    {
        _cleaner = new ImageCleaner();
        _ocr = new OcrEngine(tessDataPath);
    }

    public string Run(string imagePath)
    {
        var processed = _cleaner.Process(imagePath);

        // Optional: save debug image
        CvInvoke.Imwrite("debug_cleaned.png", processed);

        string text = _ocr.ExtractText(processed);

        return text;
    }
}