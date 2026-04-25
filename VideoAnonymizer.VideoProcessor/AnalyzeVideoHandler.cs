using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.VideoProcessor;

public sealed class AnalyzeVideoHandler(VideoAnalyzer worker) : IMessageHandler<AnalyzeVideo>
{
    public Task HandleAsync(AnalyzeVideo message, CancellationToken cancellationToken = default)
    {
        return worker.EnqueueAsync(message, cancellationToken);
    }
}
