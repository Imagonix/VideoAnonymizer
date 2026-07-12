namespace VideoAnonymizer.Contracts;

public class TrackForwardProgress
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int? TrackId { get; set; }
    public int GapStartTimeMs { get; set; }
    public int GapEndTimeMs { get; set; }
    public List<Guid> CreatedObjectIds { get; set; } = [];
    public bool IsFinal { get; set; }

    public TrackForwardProgress() { }

    public TrackForwardProgress(Guid jobId, Guid videoId, DateTimeOffset addedAt, string status, string error, int? trackId, int gapStartTimeMs, int gapEndTimeMs, List<Guid>? createdObjectIds = null, bool isFinal = false)
    {
        JobId = jobId;
        VideoId = videoId;
        AddedAt = addedAt;
        Status = status;
        Error = error;
        TrackId = trackId;
        GapStartTimeMs = gapStartTimeMs;
        GapEndTimeMs = gapEndTimeMs;
        CreatedObjectIds = createdObjectIds ?? [];
        IsFinal = isFinal;
    }
}
