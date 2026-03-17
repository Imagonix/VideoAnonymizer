namespace VideoAnonymizer.ApiService
{
    public class LongRunningJobFinishedMessage
    {
        public string JobId { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
