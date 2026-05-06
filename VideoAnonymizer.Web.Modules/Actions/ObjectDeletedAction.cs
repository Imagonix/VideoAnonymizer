using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Actions;

public sealed record ObjectDeletedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required string AnalyzedFrameId { get; init; }
    public required DetectedObjectDto Object { get; init; }
}
