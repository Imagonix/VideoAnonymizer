using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Actions;

public sealed record ObjectsBulkUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required IReadOnlyList<DetectedObjectDto> Objects { get; init; }
    public string OperationType { get; init; } = "";
    public required IReadOnlyList<DetectedObjectDto> BeforeState { get; init; }
}
