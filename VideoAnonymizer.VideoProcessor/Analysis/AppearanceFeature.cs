using System.Numerics;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed record AppearanceFeature(
    double[] Histogram,
    ulong PerceptualHash,
    double[] IntensitySignature,
    int HashBitCount = 63)
{
    public static double Compare(AppearanceFeature first, AppearanceFeature second)
    {
        var histogramSimilarity = CompareHistogram(first.Histogram, second.Histogram);
        var hashSimilarity = CompareHash(first, second);
        var intensitySimilarity = CompareIntensitySignature(first.IntensitySignature, second.IntensitySignature);

        return Math.Clamp(
            histogramSimilarity * 0.35 +
            hashSimilarity * 0.25 +
            intensitySimilarity * 0.40,
            0,
            1);
    }

    private static double CompareHistogram(double[] first, double[] second)
    {
        if (first.Length == 0 || first.Length != second.Length)
            return 0;

        var similarity = 0.0;
        for (var i = 0; i < first.Length; i++)
            similarity += Math.Sqrt(first[i] * second[i]);

        return Math.Clamp(similarity, 0, 1);
    }

    private static double CompareHash(AppearanceFeature first, AppearanceFeature second)
    {
        if (first.HashBitCount <= 0 || first.HashBitCount != second.HashBitCount)
            return 0;

        var hashDifference = BitOperations.PopCount(first.PerceptualHash ^ second.PerceptualHash);
        return 1 - Math.Min(first.HashBitCount, hashDifference) / (double)first.HashBitCount;
    }

    private static double CompareIntensitySignature(double[] first, double[] second)
    {
        if (first.Length == 0 || first.Length != second.Length)
            return 0;

        var dotProduct = 0.0;
        var firstMagnitude = 0.0;
        var secondMagnitude = 0.0;
        for (var i = 0; i < first.Length; i++)
        {
            dotProduct += first[i] * second[i];
            firstMagnitude += first[i] * first[i];
            secondMagnitude += second[i] * second[i];
        }

        if (firstMagnitude <= 0.0001 || secondMagnitude <= 0.0001)
            return 0;

        var cosineSimilarity = dotProduct / Math.Sqrt(firstMagnitude * secondMagnitude);
        return Math.Clamp((cosineSimilarity + 1) / 2, 0, 1);
    }
}
