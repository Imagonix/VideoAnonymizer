using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public sealed record DetectedObjectChangeSet
{
    public required IReadOnlyList<DetectedObjectDto> ObjectsToUpdate { get; init; }
    public required IReadOnlyList<string> ObjectsToRemove { get; init; }
    public required IReadOnlyList<DetectedObjectDto> ObjectsToAdd { get; init; }
}
