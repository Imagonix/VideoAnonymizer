namespace VideoAnonymizer.Contracts;

public class AnonymizedVideo
{
    public Guid JobId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    // for rabbitMq
    public AnonymizedVideo() { }

    public AnonymizedVideo(Guid jobId, DateTimeOffset addedAt)
    {
        JobId = jobId;
        AddedAt = addedAt;
    }
}