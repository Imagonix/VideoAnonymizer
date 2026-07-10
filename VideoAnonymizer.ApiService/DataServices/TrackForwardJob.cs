using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices;

public sealed record TrackForwardJob(
    Guid JobId,
    Guid VideoId,
    TrackForwardRequestDto Request);
