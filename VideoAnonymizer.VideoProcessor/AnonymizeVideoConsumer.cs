using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class AnonymizeVideoConsumer : MessageConsumer<AnonymizeVideo>
{
    private readonly IMessageHandler<AnonymizeVideo> _handler;

    protected override string Queue => RabbitMQConstants.Queues.Anonymize;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Anonymize;

    public AnonymizeVideoConsumer(
        IMessageHandler<AnonymizeVideo> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<AnonymizeVideoConsumer> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    public override async Task Consume(AnonymizeVideo message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
