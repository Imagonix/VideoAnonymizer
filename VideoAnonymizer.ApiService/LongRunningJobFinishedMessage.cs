namespace VideoAnonymizer.ApiService
{
    public class LongRunningJobFinishedMessage
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = "";
    }
}
