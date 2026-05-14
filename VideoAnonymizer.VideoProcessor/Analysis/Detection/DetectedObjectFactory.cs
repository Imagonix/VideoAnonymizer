using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.Analysis.Detection;

internal static class DetectedObjectFactory
{
    public static AnalyzedFrame CreateAnalyzedFrame(Guid videoId, FrameDetectionResult result)
    {
        var analyzedFrame = new AnalyzedFrame
        {
            FrameIndex = result.FrameIndex,
            TimeSeconds = result.TimeSeconds,
            VideoId = videoId,
            DetectedObjects = []
        };

        analyzedFrame.DetectedObjects = result.Detections
            .Select(detection => CreateDetectedObject(analyzedFrame.Id, detection))
            .ToList();

        return analyzedFrame;
    }

    public static DetectedObject CreateDetectedObject(Guid analyzedFrameId, DetectionResult detection)
    {
        return new DetectedObject
        {
            Selected = true,
            AnalyzedFrameId = analyzedFrameId,
            Height = detection.Height,
            Width = detection.Width,
            X = detection.X,
            Y = detection.Y,
            ClassName = detection.ClassName,
            Confidence = detection.Confidence,
            TrackId = detection.TrackId
        };
    }

}
