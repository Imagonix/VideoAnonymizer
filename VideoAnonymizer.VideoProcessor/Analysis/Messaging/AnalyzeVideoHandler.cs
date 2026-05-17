using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.VideoProcessor.Analysis;

namespace VideoAnonymizer.VideoProcessor.Analysis.Messaging;

internal sealed class AnalyzeVideoHandler(VideoAnalyzer worker) : IMessageHandler<AnalyzeVideo>
{
    public Task HandleAsync(AnalyzeVideo message, CancellationToken cancellationToken = default)
    {
        return worker.EnqueueAsync(message, cancellationToken);
    }
}
