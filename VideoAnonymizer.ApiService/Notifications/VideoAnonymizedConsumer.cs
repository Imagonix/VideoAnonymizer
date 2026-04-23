using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications
{
    public class VideoAnonymizedConsumer : MessageConsumer<AnonymizedVideo>
    {
        private LongRunningJobsHub _hub;

        public VideoAnonymizedConsumer(LongRunningJobsHub hub, IRabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<MessageConsumer<AnonymizedVideo>> logger) : base(connectionFactory, options, logger)
        {
            _hub = hub;
        }

        protected override string Queue => RabbitMQConstants.Queues.Anonymized;

        protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Anonymized;

        public override async Task Consume(AnonymizedVideo message, CancellationToken cancellationToken)
        {
            await _hub.Clients.All.SendAsync(SharedConstants.SignalR.Messages.VideoAnonymized, new LongRunningJobFinishedMessage() { JobId = message.JobId, Status = SharedConstants.SignalR.Status.Completed }, cancellationToken);
        }
    }
}
