using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Shared;

public class TrackForwardProgressMessage
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public string Status { get; set; } = "";
    public string Error { get; set; } = "";
    public int? TrackId { get; set; }
    public int GapStartTimeMs { get; set; }
    public int GapEndTimeMs { get; set; }
    public bool IsFinal { get; set; }
    public List<DetectedObjectDto> CreatedObjects { get; set; } = [];
}
