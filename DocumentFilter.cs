using System;
using System.IO;
using Emgu.CV;
using Emgu.CV.Util;

namespace ImageProcessing
{
    /// <summary>
    /// Filters a source image using the Green channel + Binary Threshold
    /// to suppress watermarks and improve OCR readability.
    /// </summary>
    public class DocumentFilter
    {
        /// <summary>
        /// Applies green-channel extraction and binary thresholding to the input image
        /// and saves the result to <paramref name="outputImagePath"/>.
        /// </summary>
        public bool Execute(string inputImagePath, string outputImagePath, int threshold = 150)
        {
            using Mat original = CvInvoke.Imread(inputImagePath, Emgu.CV.CvEnum.ImreadModes.ColorBgr);

            if (original.IsEmpty)
            {
                Console.WriteLine("[DocumentFilter] Failed to load image: " + inputImagePath);
                return false;
            }

            using VectorOfMat channels = new VectorOfMat();
            CvInvoke.Split(original, channels);   // splits into B, G, R

            // Green channel = index 1 in BGR
            using Mat greenChannel = channels[1].Clone();
            using Mat result = new Mat();

            CvInvoke.Threshold(greenChannel, result, threshold, 255,
                Emgu.CV.CvEnum.ThresholdType.Binary);

            CvInvoke.Imwrite(outputImagePath, result);

            Console.WriteLine($"[DocumentFilter] Filtered image saved → {outputImagePath}");
            return true;
        }
    }
}
