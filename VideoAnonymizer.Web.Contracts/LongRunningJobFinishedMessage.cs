namespace VideoAnonymizer.Web.Shared
{
    public class LongRunningJobFinishedMessage
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = "";
    }
}
