using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.VideoProcessor.VideoAnalyzer
{
    internal sealed record FrameWorkItem(
        int FrameIndex,
        double TimestampSeconds,
        Guid VideoId,
        Guid AnalyzedFrameId,
        string ImageBase64);
}
