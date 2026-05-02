using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

public class ImageCleaner
{
    public Mat Process(string inputPath)
    {
        Mat original = CvInvoke.Imread(inputPath, ImreadModes.Color );

        if (original.IsEmpty)
            throw new Exception("Image not loaded");

        Mat gray = new Mat();
        CvInvoke.CvtColor(original, gray, ColorConversion.Bgr2Gray);

        // 🔹 Normalize lighting
        Mat normalized = new Mat();
        CvInvoke.EqualizeHist(gray, normalized);

        // 🔹 Detect watermark (light regions)
        Mat mask = new Mat();
        CvInvoke.InRange(normalized,
            new ScalarArray(180),
            new ScalarArray(255),
            mask);

        // 🔹 Remove watermark using inpainting
        Mat inpainted = new Mat();
        CvInvoke.Inpaint(original, mask, inpainted,
            3, InpaintType.Telea);

        // 🔹 Convert again after cleaning
        Mat cleanGray = new Mat();
        CvInvoke.CvtColor(inpainted, cleanGray, ColorConversion.Bgr2Gray);

        // 🔹 Adaptive threshold (best for documents)
        Mat binary = new Mat();
        CvInvoke.AdaptiveThreshold(cleanGray, binary, 255,
            AdaptiveThresholdType.GaussianC,
            ThresholdType.Binary,
            31, 10);

        // 🔹 Remove noise
        Mat kernel = CvInvoke.GetStructuringElement(
            ElementShape.Rectangle, new Size(2, 2),
            new Point(-1, -1));

        CvInvoke.MorphologyEx(binary, binary,
            MorphOp.Open, kernel,
            new Point(-1, -1), 1,
            BorderType.Default, default);

        return binary;
    }
}