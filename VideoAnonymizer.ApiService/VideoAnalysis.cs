namespace VideoAnonymizer.ApiService
{
    public class VideoAnalysis
    {
        public FrameAnalysis[] AnalyzedFrames { get; set; }
    }

    public class FrameAnalysis
    {
        public double TimeSeconds { get; set; }
        public DetectedObject[] DetectedObjects { get; set; }
    }

    public class DetectedObject
    {
        public string TrackId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
