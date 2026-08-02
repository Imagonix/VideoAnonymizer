using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Messaging;

internal sealed class TrackForwardVideoHandler(SingleObjectTracker worker) : IMessageHandler<TrackForwardJob>
{
    public Task HandleAsync(TrackForwardJob message, CancellationToken cancellationToken = default)
    {
        return worker.EnqueueAsync(message, cancellationToken);
    }
}
