using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.ApiService.Notifications;

public class VideoProcessingProgressConsumer : MessageConsumer<VideoProcessingProgress>
{
    private readonly IMessageHandler<VideoProcessingProgress> _handler;

    public VideoProcessingProgressConsumer(
        IMessageHandler<VideoProcessingProgress> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<MessageConsumer<VideoProcessingProgress>> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    protected override string Queue => RabbitMQConstants.Queues.Progress;

    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Progress;

    public override async Task Consume(VideoProcessingProgress message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
