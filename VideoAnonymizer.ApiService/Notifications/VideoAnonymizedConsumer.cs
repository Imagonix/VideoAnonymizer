using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications
{
    public class VideoAnonymizedConsumer : MessageConsumer<AnalyzedVideo>
    {
        private LongRunningJobsHub _hub;

        public VideoAnonymizedConsumer(LongRunningJobsHub hub, IRabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<MessageConsumer<AnalyzedVideo>> logger) : base(connectionFactory, options, logger)
        {
            _hub = hub;
        }

        protected override string Queue => RabbitMQConstants.Queues.VideoNotifications;

        protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Anonymized;

        public override async Task Consume(AnalyzedVideo message, CancellationToken cancellationToken)
        {
            await _hub.Clients.All.SendAsync(SharedConstants.SignalR.Messages.VideoAnonymized, new LongRunningJobFinishedMessage() { JobId = message.JobId, Status = "completed" }, cancellationToken);
        }
    }
}
