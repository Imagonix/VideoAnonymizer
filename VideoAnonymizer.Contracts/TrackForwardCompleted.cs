namespace VideoAnonymizer.Contracts;

public class TrackForwardCompleted
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int? TrackId { get; set; }

    public TrackForwardCompleted() { }

    public TrackForwardCompleted(Guid jobId, Guid videoId, DateTimeOffset addedAt, string status, string error, int? trackId)
    {
        JobId = jobId;
        VideoId = videoId;
        AddedAt = addedAt;
        Status = status;
        Error = error;
        TrackId = trackId;
    }
}
