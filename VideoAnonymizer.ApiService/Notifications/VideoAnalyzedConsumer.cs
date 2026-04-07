using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications
{
    public class VideoAnalyzedConsumer : MessageConsumer<AnalyzedVideo>
    {
        private LongRunningJobsHub _hub;

        public VideoAnalyzedConsumer(LongRunningJobsHub hub, IRabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<MessageConsumer<AnalyzedVideo>> logger) : base(connectionFactory, options, logger)
        {
            _hub = hub;
        }

        protected override string Queue => RabbitMQConstants.Queues.VideoNotifications;

        protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Analyzed;

        public override async Task Consume(AnalyzedVideo message, CancellationToken cancellationToken)
        {
            await _hub.Clients.All.SendAsync(SharedConstants.SignalR.Messages.VideoAnalyzed, new LongRunningJobFinishedMessage() { JobId = message.JobId, Status = SharedConstants.SignalR.Status.Completed}, cancellationToken);
        }
    }
}
