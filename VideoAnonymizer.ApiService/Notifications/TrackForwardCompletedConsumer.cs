using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.ApiService.Notifications;

public class TrackForwardCompletedConsumer : MessageConsumer<TrackForwardCompleted>
{
    private readonly IMessageHandler<TrackForwardCompleted> _handler;

    public TrackForwardCompletedConsumer(
        IMessageHandler<TrackForwardCompleted> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<MessageConsumer<TrackForwardCompleted>> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    protected override string Queue => RabbitMQConstants.Queues.TrackForwardCompleted;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.TrackForwardCompleted;

    public override async Task Consume(TrackForwardCompleted message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
