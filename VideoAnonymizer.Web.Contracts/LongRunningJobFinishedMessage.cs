namespace VideoAnonymizer.Web.Contracts
{
    public class LongRunningJobFinishedMessage
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = "";
    }
}
