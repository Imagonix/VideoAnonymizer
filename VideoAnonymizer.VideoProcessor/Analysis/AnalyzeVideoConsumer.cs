using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class AnalyzeVideoConsumer : MessageConsumer<AnalyzeVideo>
{
    private readonly IMessageHandler<AnalyzeVideo> _handler;

    protected override string Queue => RabbitMQConstants.Queues.Analyze;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Analyze;

    public AnalyzeVideoConsumer(
        IMessageHandler<AnalyzeVideo> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<AnalyzeVideoConsumer> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    public override async Task Consume(AnalyzeVideo message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
