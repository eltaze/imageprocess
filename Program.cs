

using TradingBot.OcrModule;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string inputImage = @"C:\test.png";
        string tessDataPath = @"C:\data";

        if (!Directory.Exists(tessDataPath))
        {
            Console.WriteLine($"[Error]: Missing tessdata folder.");
            return;
        }

        OrderFlowAnalyzer.ExecuteFullOcrProcess(inputImage, tessDataPath);
    }
}

////using System;
////using Emgu.CV;
////using Emgu.CV.Util;

////namespace ImageProcessing
////{
////    public class DocumentFilter
////    {
////        public void Execute(string imagePath, string restoredImagePath)
////        {
////            // تحميل الصورة بالألوان القياسية
////            using (Mat originalImage = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.ColorBgr))
////            {
////                if (originalImage.IsEmpty)
////                {
////                    Console.WriteLine("فشل في تحميل الصورة.");
////                    return;
////                }

////                using (VectorOfMat channels = new VectorOfMat())
////                using (Mat resultImage = new Mat())
////                {
////                    // 1. فصل الصورة إلى قنواتها الثلاث (B, G, R)
////                    CvInvoke.Split(originalImage, channels);

////                    // 2. استدعاء القناة الخضراء فقط (الفهرس 1 لأن الترتيب هو BGR)
////                    using (Mat greenChannel = channels[1])
////                    {
////                        // 3. تطبيق Threshold ثنائي
////                        // أي بكسل قيمته أعلى من 150 (فاتح/أخضر) سيتحول إلى 255 (أبيض)
////                        // أي بكسل قيمته أقل (مثل النص الأسود) سيتحول إلى 0 (أسود)
////                        CvInvoke.Threshold(greenChannel, resultImage, 150, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

////                        // 4. حفظ الصورة النهائية
////                        CvInvoke.Imwrite(restoredImagePath, resultImage);

////                        Console.WriteLine("تمت تصفية المستند بنجاح.");
////                    }
////                }
////            }
////        }
////    }

////    public class Program
////    {
////        public static void Main(string[] args)
////        {
////            DocumentFilter filter = new DocumentFilter();

////            string input = @"C:\test.png";
////            string cleanOutput = "clean_document_output.jpg";

////            filter.Execute(input, cleanOutput);
////        }
////    }
////}

//using System;
//using Emgu.CV;
//using Emgu.CV.Structure;

//namespace ImageProcessing
//{
//    public class DocumentWatermarkProcessor
//    {
//        public void ExtractAndRestore(string imagePath, string watermarkLayerPath, string restoredImagePath)
//        {
//            using (Mat originalImage = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.AnyColor))
//            {
//                if (originalImage.IsEmpty)
//                {
//                    Console.WriteLine("فشل في تحميل الصورة. تأكد من صحة المسار.");
//                    return;
//                }

//                using (Mat hsvImage = new Mat())
//                using (Mat watermarkMask = new Mat())
//                using (Mat restoredImage = new Mat())
//                {
//                    CvInvoke.CvtColor(originalImage, hsvImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);

//                    // النطاق اللوني الخاص بالعلامة المائية الخضراء
//                    ScalarArray lowerBound = new ScalarArray(new MCvScalar(35, 50, 50));
//                    ScalarArray upperBound = new ScalarArray(new MCvScalar(85, 255, 255));

//                    CvInvoke.InRange(hsvImage, lowerBound, upperBound, watermarkMask);

//                    // تم استبدال ElementShape بـ MorphShapes ليتوافق مع الإصدارات الحديثة
//                    // تم إضافة using هنا لتحرير الـ Mat Kernel من الذاكرة بمجرد انتهاء التمدد
//                    using (Mat kernel = CvInvoke.GetStructuringElement(
//                        Emgu.CV.CvEnum.MorphShapes.Rectangle,
//                        new System.Drawing.Size(3, 3),
//                        new System.Drawing.Point(-1, -1)))
//                    {
//                        CvInvoke.Dilate(watermarkMask, watermarkMask, kernel, new System.Drawing.Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
//                    }

//                    CvInvoke.Imwrite(watermarkLayerPath, watermarkMask);

//                    CvInvoke.Inpaint(originalImage, watermarkMask, restoredImage, 3, Emgu.CV.CvEnum.InpaintType.Telea);

//                    CvInvoke.Imwrite(restoredImagePath, restoredImage);

//                    Console.WriteLine("تمت المعالجة اللونية بنجاح.");
//                }
//            }
//        }
//    }

//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            DocumentWatermarkProcessor processor = new DocumentWatermarkProcessor();

//            string input = @"C:\test.png";
//            string extractedWatermark = "extracted_watermark_layer_hsv.png";
//            string restoredOutput = "restored_original_hsv.jpg";

//            processor.ExtractAndRestore(input, extractedWatermark, restoredOutput);
//        }
//    }
//}