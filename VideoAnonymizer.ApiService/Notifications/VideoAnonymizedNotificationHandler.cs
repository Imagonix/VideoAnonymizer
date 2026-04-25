using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class VideoAnonymizedNotificationHandler(LongRunningJobsHub hub) : IMessageHandler<AnonymizedVideo>
{
    public Task HandleAsync(AnonymizedVideo message, CancellationToken cancellationToken = default)
    {
        return hub.Clients.All.SendAsync(
            SharedConstants.SignalR.Messages.VideoAnonymized,
            new LongRunningJobFinishedMessage
            {
                JobId = message.JobId,
                Status = SharedConstants.SignalR.Status.Completed
            },
            cancellationToken);
    }
}
