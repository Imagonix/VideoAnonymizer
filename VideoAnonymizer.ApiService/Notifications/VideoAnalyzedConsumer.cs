using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications
{
    public class VideoAnalyzedConsumer : MessageConsumer<AnalyzedVideo>
    {
        private readonly IMessageHandler<AnalyzedVideo> _handler;

        public VideoAnalyzedConsumer(IMessageHandler<AnalyzedVideo> handler, IRabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<MessageConsumer<AnalyzedVideo>> logger) : base(connectionFactory, options, logger)
        {
            _handler = handler;
        }

        protected override string Queue => RabbitMQConstants.Queues.Analyzed;

        protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Analyzed;

        public override async Task Consume(AnalyzedVideo message, CancellationToken cancellationToken)
        {
            await _handler.HandleAsync(message, cancellationToken);
        }
    }
}
