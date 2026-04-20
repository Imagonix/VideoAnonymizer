using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.VideoAnalyzer
{
    internal sealed record DetectionWorkResult(
        int FrameIndex,
        double TimestampSeconds,
        Guid AnalyzedFrameId,
        IReadOnlyList<DetectedObject> DetectedObjects,
        int DetectionCount);
}
