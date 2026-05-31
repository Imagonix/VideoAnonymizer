namespace VideoAnonymizer.Contracts;

public class AnonymizeVideo
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public bool InterpolateTrackedObjects { get; set; } = true;

    // for rabbitMq
    public AnonymizeVideo() { }

    public AnonymizeVideo(Guid jobId, Guid videoId, DateTimeOffset addedAt, bool interpolateTrackedObjects = true)
    {
        JobId = jobId;
        VideoId = videoId;
        AddedAt = addedAt;
        InterpolateTrackedObjects = interpolateTrackedObjects;
    }
}