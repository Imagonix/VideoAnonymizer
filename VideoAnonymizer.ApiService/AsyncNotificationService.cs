using MassTransit;
using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.ApiService
{
    public class AsyncNotificationService(LongRunningJobsHub hub) : IConsumer<AnalyzedVideo>, IConsumer<AnonomyzedVideo>
    {
        public async Task Consume(ConsumeContext<AnalyzedVideo> context)
        {
            await hub.Clients.All.SendAsync("videoAnalyzed", new LongRunningJobFinishedMessage() { JobId = context.Message.jobId});
        }

        public async Task Consume(ConsumeContext<AnonomyzedVideo> context)
        {
            await hub.Clients.All.SendAsync("videoAnonymized", new LongRunningJobFinishedMessage() { JobId = context.Message.jobId });
        }
    }
}
