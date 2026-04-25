using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications
{
    public class VideoAnonymizedConsumer : MessageConsumer<AnonymizedVideo>
    {
        private readonly IMessageHandler<AnonymizedVideo> _handler;

        public VideoAnonymizedConsumer(IMessageHandler<AnonymizedVideo> handler, IRabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<MessageConsumer<AnonymizedVideo>> logger) : base(connectionFactory, options, logger)
        {
            _handler = handler;
        }

        protected override string Queue => RabbitMQConstants.Queues.Anonymized;

        protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Anonymized;

        public override async Task Consume(AnonymizedVideo message, CancellationToken cancellationToken)
        {
            await _handler.HandleAsync(message, cancellationToken);
        }
    }
}
