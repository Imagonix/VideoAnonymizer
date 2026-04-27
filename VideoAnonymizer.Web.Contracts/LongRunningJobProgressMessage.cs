namespace VideoAnonymizer.Web.Shared
{
    public class LongRunningJobProgressMessage
    {
        public Guid JobId { get; set; }
        public Guid? VideoId { get; set; }
        public string Operation { get; set; } = string.Empty;
        public int? ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
