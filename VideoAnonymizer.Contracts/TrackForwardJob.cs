namespace VideoAnonymizer.Contracts;

public class TrackForwardJob
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    public Guid? SeedDetectionId { get; set; }
    public int? SeedFrameIndex { get; set; }
    public int SeedTimeMs { get; set; }
    public int? SeedBoundingBoxX { get; set; }
    public int? SeedBoundingBoxY { get; set; }
    public int? SeedBoundingBoxWidth { get; set; }
    public int? SeedBoundingBoxHeight { get; set; }
    public string? ObjectClass { get; set; }
    public int? InitialTrackId { get; set; }

    public int? PersistEveryMs { get; set; }
    public int MaxLostDurationMs { get; set; } = 5000;
    public int RecoveryDetectorIntervalMs { get; set; } = 250;
    public string TrackerType { get; set; } = "CSRT";
    public string ConflictMode { get; set; } = "skip";
    public double SearchAreaExpansion { get; set; } = 3.0;
    public int MaxTrackDurationMs { get; set; } = 30000;
}
