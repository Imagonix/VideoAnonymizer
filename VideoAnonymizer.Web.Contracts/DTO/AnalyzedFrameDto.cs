namespace VideoAnonymizer.Web.Shared.DTO
{
    public class AnalyzedFrameDto
    {
        public Guid Id { get; set; }
        public double TimeSeconds { get; set; }
        public Guid VideoId { get; set; }
        public List<DetectedObjectDto> DetectedObjects { get; set; } = [];
    }
}
