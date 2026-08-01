namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

public sealed class TrackForwardFailedException(
    string message,
    Exception innerException,
    int trackId,
    IReadOnlyList<Guid> createdObjectIds)
    : Exception(message, innerException)
{
    public int TrackId { get; } = trackId;
    public IReadOnlyList<Guid> CreatedObjectIds { get; } = createdObjectIds;
}
