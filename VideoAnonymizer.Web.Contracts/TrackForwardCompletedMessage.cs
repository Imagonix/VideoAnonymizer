using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Shared;

public class TrackForwardCompletedMessage
{
    public Guid JobId { get; set; }
    public Guid VideoId { get; set; }
    public string Status { get; set; } = "";
    public string Error { get; set; } = "";
    public TrackForwardResponseDto? Result { get; set; }
}
