using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Actions;

public sealed record ObjectUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required string AnalyzedFrameId { get; init; }
    public required DetectedObjectDto Object { get; init; }
    public string OperationType { get; init; } = "";
    public required IReadOnlyList<DetectedObjectDto> BeforeState { get; init; }
}
