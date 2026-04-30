using System;
using System.IO;
using OpenCvSharp;
using Tesseract;

namespace TradingBot.OcrModule
{
    public static class OrderFlowAnalyzer
    {
        public static void ExecuteFullOcrProcess(string imagePath, string dataPath)
        {
            string cleanedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "optimized_channel_output.png");

            try
            {
                // 1. معالجة الصورة وفصل القنوات (كما في فكرة Emgu.CV)
                using (Mat src = Cv2.ImRead(imagePath))
                {
                    if (src.Empty()) return;

                    // فصل الصورة إلى B, G, R
                    Mat[] channels = Cv2.Split(src);

                    // اختيار القناة الخضراء (Index 1) أو الزرقاء (Index 0)
                    // في حالتك، القناة الخضراء تجعل العلامة المائية "بيضاء" تقريباً
                    using (Mat targetChannel = channels[1])
                    using (Mat binary = new Mat())
                    {
                        // تطبيق Threshold ثنائي (العتبة 150 كما اقترحت)
                        Cv2.Threshold(targetChannel, binary, 150, 255, ThresholdTypes.Binary);

                        // تحسين اتصال الحروف العربية (Morphology)
                        using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 1)))
                        {
                            Cv2.MorphologyEx(binary, binary, MorphTypes.Open, kernel);
                        }

                        binary.SaveImage(cleanedPath);
                    }

                    // تنظيف القنوات الأخرى من الذاكرة
                    foreach (var ch in channels) ch.Dispose();
                }

                // 2. مرحلة الـ OCR باستخدام Tesseract
                using (var engine = new TesseractEngine(dataPath, "ara+eng", EngineMode.LstmOnly))
                {
                    engine.SetVariable("user_defined_dpi", "300");

                    using (var img = Pix.LoadFromFile(cleanedPath))
                    {
                        using (var page = engine.Process(img, PageSegMode.Auto))
                        {
                            float confidence = page.GetMeanConfidence();
                            string result = page.GetText();

                            Console.WriteLine("\n========================================");
                            Console.WriteLine($"نسبة الثقة (بعد فلترة القنوات): {confidence:P2}");
                            Console.WriteLine("========================================\n");

                            if (string.IsNullOrWhiteSpace(result))
                            {
                                Console.WriteLine("⚠️ لم يتم استخراج نص. تأكد من تحميل ملفات ara.traineddata (نسخة Best).");
                            }
                            else
                            {
                                Console.WriteLine("--- النص المستخرج ---");
                                Console.WriteLine(result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analyzer Error]: {ex.Message}");
            }
            finally
            {
                // مسح الملف المؤقت
                if (File.Exists(cleanedPath)) File.Delete(cleanedPath);
            }
        }
    }
}
//using System;
//using System.IO;
//using OpenCvSharp;
//using Tesseract;

//namespace TradingBot.OcrModule
//{
//    public static class OrderFlowAnalyzer
//    {
//        public static void ExecuteFullOcrProcess(string imagePath, string dataPath)
//        {
//            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
//            string cleanedPath = Path.Combine(baseDir, "final_rebuilt_text.png");

//            // ── DEBUG PATHS (delete these lines once tuning is done) ──────────────
//            string debugMask = Path.Combine(baseDir, "debug_mask.png");
//            string debugInpainted = Path.Combine(baseDir, "debug_inpainted.png");
//            string debugGray = Path.Combine(baseDir, "debug_gray.png");
//            string debugBinary = Path.Combine(baseDir, "debug_binary.png");
//            // ─────────────────────────────────────────────────────────────────────

//            try
//            {
//                using (Mat src = Cv2.ImRead(imagePath))
//                {
//                    if (src.Empty()) return;

//                    // ── DIAGNOSTIC: print the exact HSV of the centre pixel ───────
//                    // This tells us the real hue of the watermark
//                    using (Mat hsvCheck = new Mat())
//                    {
//                        Cv2.CvtColor(src, hsvCheck, ColorConversionCodes.BGR2HSV);

//                        // Sample a few points likely to be ON the watermark
//                        // (top-right area where we can see diagonal text in the image)
//                        int[] sampleX = { src.Width / 4, src.Width / 2, src.Width * 3 / 4 };
//                        int[] sampleY = { src.Height / 4, src.Height / 2, src.Height * 3 / 4 };

//                        Console.WriteLine("=== HSV SAMPLES (find watermark pixel values) ===");
//                        foreach (int y in sampleY)
//                            foreach (int x in sampleX)
//                            {
//                                var px = hsvCheck.At<Vec3b>(y, x);
//                                Console.WriteLine(
//                                    $"  Pixel ({x,4},{y,4})  " +
//                                    $"H={px.Item0,3}  S={px.Item1,3}  V={px.Item2,3}");
//                            }
//                        Console.WriteLine("=================================================\n");
//                    }

//                    using (Mat hsv = new Mat())
//                    using (Mat mask = new Mat())
//                    using (Mat maskLow = new Mat())
//                    using (Mat maskHigh = new Mat())
//                    using (Mat dilated = new Mat())
//                    using (Mat inpainted = new Mat())
//                    using (Mat gray = new Mat())
//                    using (Mat binary = new Mat())
//                    {
//                        Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

//                        // ── Current mask (red/pink) — may need adjusting ──────────
//                        Cv2.InRange(hsv,
//                            new Scalar(0, 60, 60),
//                            new Scalar(15, 255, 255), maskLow);

//                        Cv2.InRange(hsv,
//                            new Scalar(160, 60, 60),
//                            new Scalar(180, 255, 255), maskHigh);

//                        Cv2.BitwiseOr(maskLow, maskHigh, mask);

//                        // ── DIAGNOSTIC: count how many pixels were masked ─────────
//                        int maskedPixels = Cv2.CountNonZero(mask);
//                        int totalPixels = src.Width * src.Height;
//                        Console.WriteLine(
//                            $"Masked pixels: {maskedPixels} / {totalPixels} " +
//                            $"({100.0 * maskedPixels / totalPixels:F1}%)");
//                        Console.WriteLine(
//                            "  → If 0%: hue range is wrong, check HSV samples above");
//                        Console.WriteLine(
//                            "  → If >10%: mask is too broad, it's eating real text\n");

//                        // Save mask so you can open it and see what was detected
//                        mask.SaveImage(debugMask);
//                        Console.WriteLine($"DEBUG mask saved → {debugMask}");

//                        var kernel = Cv2.GetStructuringElement(
//                            MorphShapes.Rect, new Size(3, 3));
//                        Cv2.Dilate(mask, dilated, kernel, iterations: 1);

//                        Cv2.Inpaint(src, dilated, inpainted, 5, InpaintTypes.Telea);
//                        inpainted.SaveImage(debugInpainted);
//                        Console.WriteLine($"DEBUG inpainted saved → {debugInpainted}");

//                        Cv2.CvtColor(inpainted, gray, ColorConversionCodes.BGR2GRAY);
//                        gray.SaveImage(debugGray);
//                        Console.WriteLine($"DEBUG gray saved → {debugGray}");

//                        Cv2.AdaptiveThreshold(gray, binary, 255,
//                            AdaptiveThresholdTypes.GaussianC,
//                            ThresholdTypes.Binary, 15, 10);

//                        Cv2.Resize(binary, binary, new Size(), 2.0, 2.0,
//                            InterpolationFlags.Cubic);

//                        binary.SaveImage(cleanedPath);
//                        binary.SaveImage(debugBinary);
//                        Console.WriteLine($"DEBUG binary saved → {debugBinary}\n");
//                    }
//                }

//                using (var engine = new TesseractEngine(
//                    dataPath, "ara+eng", EngineMode.LstmOnly))
//                {
//                    engine.SetVariable("user_defined_dpi", "300");

//                    using (var img = Pix.LoadFromFile(cleanedPath))
//                    using (var page = engine.Process(img, PageSegMode.SingleBlock))
//                    {
//                        Console.WriteLine("\n========================================");
//                        Console.WriteLine($"نسبة الثقة: {page.GetMeanConfidence():P2}");
//                        Console.WriteLine("========================================\n");

//                        string result = page.GetText();
//                        if (string.IsNullOrWhiteSpace(result))
//                            Console.WriteLine("⚠️ لم يُستخرج نص.");
//                        else
//                        {
//                            Console.WriteLine("--- النص المستخرج ---");
//                            Console.WriteLine(result);
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[Analyzer Error]: {ex.Message}");
//            }
//            finally
//            {
//                if (File.Exists(cleanedPath)) File.Delete(cleanedPath);
//            }
//        }
//    }
//}


////using System;
////using System.IO;
////using OpenCvSharp;
////using Tesseract;

////namespace TradingBot.OcrModule
////{
////    public static class OrderFlowAnalyzer
////    {
////        public static void ExecuteFullOcrProcess(string imagePath, string dataPath)
////        {
////            string cleanedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "final_rebuilt_text.png");

////            try
////            {
////                using (Mat src = Cv2.ImRead(imagePath))
////                {
////                    if (src.Empty()) return;

////                    using (Mat gray = new Mat())
////                    using (Mat binary = new Mat())
////                    {
////                        // 1. تحويل للرمادي
////                        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

////                        // 2. استخدام العتبة التكيفية لإزالة العلامات المائية المائلة تماماً
////                        Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

////                        // 3. تكبير الصورة لضمان دقة الحروف (DPI)
////                        Cv2.Resize(binary, binary, new Size(), 1.5, 1.5, InterpolationFlags.Cubic);

////                        binary.SaveImage(cleanedPath);
////                    }
////                }

////                // 4. استخراج النص باستخدام النمط الأحدث LSTM
////                using (var engine = new TesseractEngine(dataPath, "ara+eng", EngineMode.LstmOnly))
////                {
////                    // إجبار المحرك على اعتبار الدقة 300 DPI لحل مشكلة 'Estimating resolution'
////                    engine.SetVariable("user_defined_dpi", "300");

////                    using (var img = Pix.LoadFromFile(cleanedPath))
////                    {
////                        // الحل الجذري: استخدام SingleBlock لقراءة الفقرات بشكل متصل
////                        using (var page = engine.Process(img, PageSegMode.SingleBlock))
////                        {
////                            Console.WriteLine("\n========================================");
////                            Console.WriteLine($"نسبة الثقة النهائية المجمعة: {page.GetMeanConfidence():P2}");
////                            Console.WriteLine("========================================\n");

////                            string result = page.GetText();

////                            if (string.IsNullOrWhiteSpace(result))
////                            {
////                                Console.WriteLine("⚠️ المحرك لم يستخرج نصاً. تأكد من جودة ملفات ara.traineddata.");
////                            }
////                            else
////                            {
////                                Console.WriteLine("--- النص المستخرج ---");
////                                Console.WriteLine(result);
////                            }
////                        }
////                    }
////                }
////            }
////            catch (Exception ex)
////            {
////                Console.WriteLine($"[Analyzer Error]: {ex.Message}");
////            }
////            finally
////            {
////                if (File.Exists(cleanedPath)) File.Delete(cleanedPath);
////            }
////        }
////    }
////}


//////using System;
//////using System.IO;
//////using OpenCvSharp;
//////using Tesseract;

//////namespace TradingBot.OcrModule
//////{
//////    public static class OrderFlowAnalyzer
//////    {
//////        public static void ExecuteFullOcrProcess(string imagePath, string dataPath)
//////        {
//////            string cleanedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rebuilt_final_clean.png");

//////            try
//////            {
//////                using (Mat src = Cv2.ImRead(imagePath))
//////                {
//////                    if (src.Empty()) return;

//////                    using (Mat gray = new Mat())
//////                    using (Mat denoised = new Mat())
//////                    using (Mat binary = new Mat())
//////                    {
//////                        // 1. تحويل للرمادي
//////                        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

//////                        // 2. إزالة الضجيج (Denoising) لتقليل أثر العلامة المائية
//////                        Cv2.FastNlMeansDenoising(gray, denoised, 10);

//////                        // 3. تطبيق التباين التكيفي (Adaptive Thresholding)
//////                        Cv2.AdaptiveThreshold(denoised, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

//////                        // 4. تكبير الصورة مرتين لرفع دقة الحروف (DPI)
//////                        Cv2.Resize(binary, binary, new Size(), 2.0, 2.0, InterpolationFlags.Lanczos4);

//////                        binary.SaveImage(cleanedPath);
//////                    }
//////                }

//////                // 5. استخراج النص باستخدام محرك LSTM الافتراضي
//////                using (var engine = new TesseractEngine(dataPath, "ara+eng", EngineMode.Default))
//////                {
//////                    engine.SetVariable("user_defined_dpi", "300");

//////                    using (var img = Pix.LoadFromFile(cleanedPath))
//////                    {
//////                        // استخدام وضعية AutoOnly لتجنب فشل التحليل الهيكلي
//////                        using (var page = engine.Process(img, PageSegMode.AutoOnly))
//////                        {
//////                            Console.WriteLine("\n========================================");
//////                            Console.WriteLine($"نسبة الثقة النهائية: {page.GetMeanConfidence():P2}");
//////                            Console.WriteLine("========================================\n");

//////                            string result = page.GetText();
//////                            if (!string.IsNullOrWhiteSpace(result))
//////                            {
//////                                Console.WriteLine("النص المستخرج:");
//////                                Console.WriteLine(result);
//////                            }
//////                            else
//////                            {
//////                                Console.WriteLine("⚠️ لم يتم استخراج نص. تأكد من وجود ملفات ara.traineddata بنسخة Best.");
//////                            }
//////                        }
//////                    }
//////                }
//////            }
//////            catch (Exception ex)
//////            {
//////                Console.WriteLine($"[Analyzer Error]: {ex.Message}");
//////            }
//////            finally
//////            {
//////                if (File.Exists(cleanedPath)) File.Delete(cleanedPath);
//////            }
//////        }
//////    }
//////}