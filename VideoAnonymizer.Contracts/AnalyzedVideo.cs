namespace VideoAnonymizer.Contracts;

public class AnalyzedVideo
{
    public Guid JobId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    // for rabbitMq
    public AnalyzedVideo() { }

    public AnalyzedVideo(Guid jobId, DateTimeOffset addedAt)
    {
        JobId = jobId;
        AddedAt = addedAt;
    }
}