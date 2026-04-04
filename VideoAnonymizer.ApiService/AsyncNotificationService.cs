using MassTransit;
using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.ApiService
{
    public class AsyncNotificationService(LongRunningJobsHub hub) : IConsumer<AnalyzedVideo>, IConsumer<AnonymizedVideo>
    {
        public async Task Consume(ConsumeContext<AnalyzedVideo> context)
        {
            await hub.Clients.All.SendAsync("videoAnalyzed", new LongRunningJobFinishedMessage() { JobId = context.Message.JobId, Status = "completed"});
        }

        public async Task Consume(ConsumeContext<AnonymizedVideo> context)
        {
            await hub.Clients.All.SendAsync("videoAnonymized", new LongRunningJobFinishedMessage() { JobId = context.Message.JobId, Status = "completed" });
        }
    }
}
