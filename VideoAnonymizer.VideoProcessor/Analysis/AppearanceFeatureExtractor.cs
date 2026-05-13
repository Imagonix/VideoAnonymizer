using OpenCvSharp;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal static class AppearanceFeatureExtractor
{
    private const int FeatureImageSize = 64;
    private const int SignatureImageSize = 16;
    private const int HashImageSize = 32;
    private const int HashDctSize = 8;
    private const int HashBitCount = HashDctSize * HashDctSize - 1;
    private const int HueBins = 16;
    private const int SaturationBins = 8;
    private const int ValueBins = 4;

    public static AppearanceFeature? Extract(
        Mat frame,
        TrackBox box,
        double cropPaddingPercent)
    {
        if (frame.Empty())
            return null;

        var cropRect = BuildCropRect(box, frame.Width, frame.Height, cropPaddingPercent);
        if (cropRect.Width <= 0 || cropRect.Height <= 0)
            return null;

        using var crop = new Mat(frame, cropRect);
        using var resized = new Mat();
        Cv2.Resize(crop, resized, new Size(FeatureImageSize, FeatureImageSize));

        return new AppearanceFeature(
            BuildHsvHistogram(resized),
            BuildPerceptualHash(resized),
            BuildIntensitySignature(resized),
            HashBitCount);
    }

    private static Rect BuildCropRect(
        TrackBox box,
        int frameWidth,
        int frameHeight,
        double cropPaddingPercent)
    {
        var paddingX = (int)Math.Round(box.Width * cropPaddingPercent);
        var paddingY = (int)Math.Round(box.Height * cropPaddingPercent);

        var left = Math.Max(0, box.X - paddingX);
        var top = Math.Max(0, box.Y - paddingY);
        var right = Math.Min(frameWidth, box.X + box.Width + paddingX);
        var bottom = Math.Min(frameHeight, box.Y + box.Height + paddingY);

        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static double[] BuildHsvHistogram(Mat resizedBgr)
    {
        using var hsv = new Mat();
        Cv2.CvtColor(resizedBgr, hsv, ColorConversionCodes.BGR2HSV);

        var histogram = new double[HueBins * SaturationBins * ValueBins];
        var totalWeight = 0.0;

        for (var y = 0; y < hsv.Height; y++)
        {
            for (var x = 0; x < hsv.Width; x++)
            {
                var pixel = hsv.At<Vec3b>(y, x);
                var hueBin = Math.Min(HueBins - 1, pixel.Item0 * HueBins / 180);
                var saturationBin = Math.Min(SaturationBins - 1, pixel.Item1 * SaturationBins / 256);
                var valueBin = Math.Min(ValueBins - 1, pixel.Item2 * ValueBins / 256);
                var index = (hueBin * SaturationBins + saturationBin) * ValueBins + valueBin;
                var weight = CalculateCenterWeight(x, y);

                histogram[index] += weight;
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0)
            return histogram;

        for (var i = 0; i < histogram.Length; i++)
            histogram[i] /= totalWeight;

        return histogram;
    }

    private static double CalculateCenterWeight(int x, int y)
    {
        var center = (FeatureImageSize - 1) / 2.0;
        var dx = (x - center) / center;
        var dy = (y - center) / center;
        var normalizedDistance = Math.Min(1, Math.Sqrt(dx * dx + dy * dy));

        return 1.0 + 0.5 * (1.0 - normalizedDistance);
    }

    private static ulong BuildPerceptualHash(Mat resizedBgr)
    {
        using var gray = new Mat();
        Cv2.CvtColor(resizedBgr, gray, ColorConversionCodes.BGR2GRAY);

        using var small = new Mat();
        Cv2.Resize(gray, small, new Size(HashImageSize, HashImageSize));

        using var smallFloat = new Mat();
        small.ConvertTo(smallFloat, MatType.CV_32F);

        using var dct = new Mat();
        Cv2.Dct(smallFloat, dct);

        var values = new List<float>(HashBitCount);
        for (var y = 0; y < HashDctSize; y++)
        {
            for (var x = 0; x < HashDctSize; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                values.Add(dct.At<float>(y, x));
            }
        }

        values.Sort();
        var median = values[values.Count / 2];

        var hash = 0UL;
        var bitIndex = 0;
        for (var y = 0; y < HashDctSize; y++)
        {
            for (var x = 0; x < HashDctSize; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (dct.At<float>(y, x) > median)
                    hash |= 1UL << bitIndex;

                bitIndex++;
            }
        }

        return hash;
    }

    private static double[] BuildIntensitySignature(Mat resizedBgr)
    {
        using var gray = new Mat();
        Cv2.CvtColor(resizedBgr, gray, ColorConversionCodes.BGR2GRAY);

        using var small = new Mat();
        Cv2.Resize(gray, small, new Size(SignatureImageSize, SignatureImageSize));

        using var equalized = new Mat();
        Cv2.EqualizeHist(small, equalized);

        var values = new double[SignatureImageSize * SignatureImageSize];
        var index = 0;
        var sum = 0.0;

        for (var y = 0; y < equalized.Height; y++)
        {
            for (var x = 0; x < equalized.Width; x++)
            {
                var value = equalized.At<byte>(y, x) / 255.0;
                values[index++] = value;
                sum += value;
            }
        }

        var mean = sum / values.Length;
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Length;
        var standardDeviation = Math.Sqrt(variance);
        if (standardDeviation <= 0.0001)
            return values.Select(_ => 0.0).ToArray();

        for (var i = 0; i < values.Length; i++)
            values[i] = (values[i] - mean) / standardDeviation;

        return values;
    }
}
