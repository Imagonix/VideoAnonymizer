using OpenCvSharp;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed record VideoAnalysisMetadata(
    double Fps,
    int FrameStep,
    int LastFrameIndex,
    int TotalFramesToAnalyze)
{
    public static VideoAnalysisMetadata Read(string videoPath, int captureIntervalMs)
    {
        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open video: {videoPath}");

        var fps = capture.Fps;
        if (fps <= 0 || double.IsNaN(fps))
            fps = 25;

        var frameStep = Math.Max(1, (int)Math.Round(fps * captureIntervalMs * 0.001));
        var totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        var lastFrameIndex = Math.Max(0, totalFrames - 1);
        var totalFramesToAnalyze = CountFramesToAnalyze(totalFrames, frameStep);

        return new VideoAnalysisMetadata(fps, frameStep, lastFrameIndex, totalFramesToAnalyze);
    }

    private static int CountFramesToAnalyze(int totalFrames, int frameStep)
    {
        if (totalFrames <= 0)
            return 0;

        var lastFrameIndex = totalFrames - 1;
        var safeFrameStep = Math.Max(1, frameStep);
        var count = lastFrameIndex / safeFrameStep + 1;

        if (lastFrameIndex > 0 && lastFrameIndex % safeFrameStep != 0)
            count++;

        return count;
    }
}
