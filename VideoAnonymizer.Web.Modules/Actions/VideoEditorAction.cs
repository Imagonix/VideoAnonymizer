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
}

public sealed record ObjectUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required string AnalyzedFrameId { get; init; }
    public required DetectedObjectDto Object { get; init; }
}

public sealed record ObjectsBulkUpdatedAction : VideoEditorAction
{
    public required string VideoId { get; init; }
    public required IReadOnlyList<DetectedObjectDto> Objects { get; init; }
}
