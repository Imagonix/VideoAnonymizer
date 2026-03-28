namespace VideoAnonymizer.ApiService.DTO
{
    public class DetectedObjectDto
    {
        public Guid Id { get; set; }
        public double Confidence { get; set; }
        public string? ClassName { get; set; }
        public bool Selected { get; set; }
        public int? TrackId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Guid AnalyzedFrameId { get; set; }
    }
}
