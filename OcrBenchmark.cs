using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Tesseract;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace ImageProcessing
{
    /// <summary>
    /// Runs OCR on a pre-filtered image using both Windows OCR and Tesseract,
    /// then prints a side-by-side comparison of results and performance.
    /// </summary>
    public class OcrBenchmark
    {
        private readonly string _tessDataPath;
        private readonly string _tessLanguage;

        /// <param name="tessDataPath">Path to the Tesseract tessdata folder (e.g. C:\tessdata).</param>
        /// <param name="tessLanguage">Tesseract language string, e.g. "ara", "eng", "ara+eng".</param>
        public OcrBenchmark(string tessDataPath, string tessLanguage = "ara+eng")
        {
            _tessDataPath = tessDataPath;
            _tessLanguage = tessLanguage;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Windows OCR
        // ─────────────────────────────────────────────────────────────────────
        public async Task<OcrEngineResult> RunWindowsOcrAsync(string imagePath)
        {
            var sw = Stopwatch.StartNew();
            string text = string.Empty;
            string engineInfo = "Windows OCR";
            string error = string.Empty;

            try
            {
                // StorageFile requires an absolute path
                string fullPath = Path.GetFullPath(imagePath);
                StorageFile file = await StorageFile.GetFileFromPathAsync(fullPath);

                using var stream = await file.OpenAsync(FileAccessMode.Read);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync();

                // Try Arabic first, fall back to user-profile languages
                Language arabicLang = new Language("ar");
                OcrEngine engine = OcrEngine.IsLanguageSupported(arabicLang)
                    ? OcrEngine.TryCreateFromLanguage(arabicLang)!
                    : OcrEngine.TryCreateFromUserProfileLanguages()!;

                if (engine == null)
                    throw new InvalidOperationException("Windows OCR engine could not be initialised.");

                engineInfo += $" [{engine.RecognizerLanguage.DisplayName}]";

                OcrResult result = await engine.RecognizeAsync(bitmap);
                text = result.Text;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            sw.Stop();
            return new OcrEngineResult(engineInfo, text, sw.Elapsed, error);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tesseract OCR
        // ─────────────────────────────────────────────────────────────────────
        public OcrEngineResult RunTesseractOcr(string imagePath)
        {
            var sw = Stopwatch.StartNew();
            string text = string.Empty;
            float confidence = 0f;
            string error = string.Empty;
            string engineInfo = $"Tesseract OCR [{_tessLanguage}]";

            try
            {
                using var engine = new TesseractEngine(_tessDataPath, _tessLanguage, EngineMode.LstmOnly);
                engine.SetVariable("user_defined_dpi", "300");

                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img, PageSegMode.Auto);

                text = page.GetText();
                confidence = page.GetMeanConfidence();
                engineInfo += $" | Confidence: {confidence:P1}";
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            sw.Stop();
            return new OcrEngineResult(engineInfo, text, sw.Elapsed, error);
        }
    }

    /// <summary>Holds the result from one OCR engine.</summary>
    public record OcrEngineResult(string EngineName, string Text, TimeSpan Elapsed, string Error);
}
