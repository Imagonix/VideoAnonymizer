namespace VideoAnonymizer.Contracts;

public class AnonymizeVideo
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    // for rabbitMq
    public AnonymizeVideo() { }

    public AnonymizeVideo(Guid jobId, Guid videoId, DateTimeOffset addedAt)
    {
        JobId = jobId;
        VideoId = videoId;
        AddedAt = addedAt;
    }
}