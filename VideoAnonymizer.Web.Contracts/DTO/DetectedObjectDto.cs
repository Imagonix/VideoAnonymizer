namespace VideoAnonymizer.Web.Shared.DTO
{
    public class DetectedObjectDto
    {
        public Guid Id { get; set; }
        public double Confidence { get; set; }
        public string? ClassName { get; set; }
        public string? BlurShape { get; set; }
        public int? BlurSizePercentOverride { get; set; }
        public int? OccurrenceBlurSizePercentOverride { get; set; }
        public int? PreBufferMsOverride { get; set; }
        public int? PostBufferMsOverride { get; set; }
        /// <summary>
        /// Handling for the real same-track gap after this occurrence when it is the last
        /// of its consecutive segment. Values: "Interpolate", "UseBuffers". Null defaults
        /// to Interpolate at a valid gap boundary.
        /// </summary>
        public string? NextGapHandlingMode { get; set; }
        public bool Selected { get; set; }
        public int? TrackId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Guid AnalyzedFrameId { get; set; }
    }
}
