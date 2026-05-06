namespace VideoAnonymizer.Web.Shared.DTO
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; }
        public int BlurSizePercent { get; set; } = 120;
        public int TimeBufferMs { get; set; } = 300;
    }
}
