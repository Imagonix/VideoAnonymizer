using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Actions;

public abstract record VideoEditorAction
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ObjectAddedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required string AnalyzedFrameId { get; init; }
    public required DetectedObjectDto Object { get; init; }
    public required IReadOnlyList<DetectedObjectDto> BeforeState { get; init; }
    public required IReadOnlyList<DetectedObjectDto> AfterState { get; init; }
}

public sealed record ObjectUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required string AnalyzedFrameId { get; init; }
    public required DetectedObjectDto Object { get; init; }
    public string OperationType { get; init; } = "";
    public required IReadOnlyList<DetectedObjectDto> BeforeState { get; init; }
    public required IReadOnlyList<DetectedObjectDto> AfterState { get; init; }
}

public sealed record ObjectsBulkUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required IReadOnlyList<DetectedObjectDto> Objects { get; init; }
    public string OperationType { get; init; } = "";
    public required IReadOnlyList<DetectedObjectDto> BeforeState { get; init; }
    public required IReadOnlyList<DetectedObjectDto> AfterState { get; init; }
}

public sealed record SettingsUpdatedAction : VideoEditorAction
{
    public required Guid VideoId { get; init; }
    public required AnonymizationSettingsDto BeforeState { get; init; }
    public required AnonymizationSettingsDto AfterState { get; init; }
}

public sealed record UndoAction : VideoEditorAction
{
}

public sealed record RedoAction : VideoEditorAction
{
}
