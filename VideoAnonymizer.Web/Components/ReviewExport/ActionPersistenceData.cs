using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

internal sealed record ActionDataAdd(string FrameId, DetectedObjectDto Object);

internal sealed record ActionDataUpdate(string FrameId, DetectedObjectDto Before, DetectedObjectDto After, string OperationType);

internal sealed record ActionDataBulkUpdate(List<DetectedObjectDto> Before, List<DetectedObjectDto> After, string OperationType);

internal sealed record ActionDataDelete(string FrameId, DetectedObjectDto Object);

internal sealed record ActionDataSettings(AnonymizationSettingsDto Before, AnonymizationSettingsDto After);

internal sealed record ActionDataTrackForward(
    string SeedFrameId,
    DetectedObjectDto Seed,
    List<DetectedObjectDto> CreatedObjects,
    int? TrackId,
    bool IsPartial = false);
