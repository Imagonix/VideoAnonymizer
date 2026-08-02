namespace VideoAnonymizer.Web.Shared.DTO;

public class TrackForwardResponseDto
{
    public int TrackId { get; set; }
    public int CreatedDetections { get; set; }
    public int SkippedConflicts { get; set; }
    public int ReacquiredCount { get; set; }
    public string StoppedReason { get; set; } = "";
    public List<TrackForwardGapDto> Gaps { get; set; } = [];
    public List<DetectedObjectDto> UpdatedObjects { get; set; } = [];
    public List<DetectedObjectDto> CreatedObjects { get; set; } = [];
    public List<AnalyzedFrameDto> UpdatedFrames { get; set; } = [];
}
