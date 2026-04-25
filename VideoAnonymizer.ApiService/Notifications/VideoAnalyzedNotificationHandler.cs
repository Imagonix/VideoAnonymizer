using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class VideoAnalyzedNotificationHandler(LongRunningJobsHub hub) : IMessageHandler<AnalyzedVideo>
{
    public Task HandleAsync(AnalyzedVideo message, CancellationToken cancellationToken = default)
    {
        return hub.Clients.All.SendAsync(
            SharedConstants.SignalR.Messages.VideoAnalyzed,
            new LongRunningJobFinishedMessage
            {
                JobId = message.JobId,
                Status = SharedConstants.SignalR.Status.Completed
            },
            cancellationToken);
    }
}
