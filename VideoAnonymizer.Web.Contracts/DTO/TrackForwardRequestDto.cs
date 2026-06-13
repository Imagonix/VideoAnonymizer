namespace VideoAnonymizer.Web.Shared.DTO;

public class TrackForwardRequestDto
{
    public Guid? SeedDetectionId { get; set; }
    public int SeedTimeMs { get; set; }
    public int? SeedFrameIndex { get; set; }
    public TrackForwardBoundingBoxDto? BoundingBox { get; set; }
    public string? ObjectClass { get; set; }
    public int? TrackId { get; set; }
    public int? PersistEveryMs { get; set; }
    public int MaxLostDurationMs { get; set; } = 5000;
    public int RecoveryDetectorIntervalMs { get; set; } = 250;
    public string TrackerType { get; set; } = "CSRT";
    public string ConflictMode { get; set; } = "skip";
    public double SearchAreaExpansion { get; set; } = 3.0;
    public int MaxTrackDurationMs { get; set; } = 30000;
}
