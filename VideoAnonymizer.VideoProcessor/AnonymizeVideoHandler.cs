using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.VideoProcessor;

public sealed class AnonymizeVideoHandler(VideoAnonymizer worker) : IMessageHandler<AnonymizeVideo>
{
    public Task HandleAsync(AnonymizeVideo message, CancellationToken cancellationToken = default)
    {
        return worker.EnqueueAsync(message, cancellationToken);
    }
}
