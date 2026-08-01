namespace VideoAnonymizer.Contracts;

public class TrackForwardCompleted
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int? TrackId { get; set; }
    public List<Guid> CreatedObjectIds { get; set; } = [];
    public int CreatedDetections { get; set; }
    public int SkippedConflicts { get; set; }
    public int ReacquiredCount { get; set; }
    public string StoppedReason { get; set; } = string.Empty;
    public List<TrackForwardGapSummary> Gaps { get; set; } = [];

    public TrackForwardCompleted() { }

    public TrackForwardCompleted(Guid jobId, Guid videoId, DateTimeOffset addedAt, string status, string error, int? trackId, List<Guid>? createdObjectIds = null)
    {
        JobId = jobId;
        VideoId = videoId;
        AddedAt = addedAt;
        Status = status;
        Error = error;
        TrackId = trackId;
        CreatedObjectIds = createdObjectIds ?? [];
    }
}

public class TrackForwardGapSummary
{
    public int StartTimeMs { get; set; }
    public int EndTimeMs { get; set; }

    public TrackForwardGapSummary() { }

    public TrackForwardGapSummary(int startTimeMs, int endTimeMs)
    {
        StartTimeMs = startTimeMs;
        EndTimeMs = endTimeMs;
    }
}
