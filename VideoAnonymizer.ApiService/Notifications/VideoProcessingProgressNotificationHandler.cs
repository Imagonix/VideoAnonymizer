using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class VideoProcessingProgressNotificationHandler(LongRunningJobsHub hub)
    : IMessageHandler<VideoProcessingProgress>
{
    public Task HandleAsync(VideoProcessingProgress message, CancellationToken cancellationToken = default)
    {
        return hub.Clients.All.SendAsync(
            SharedConstants.SignalR.Messages.JobProgress,
            new LongRunningJobProgressMessage
            {
                JobId = message.JobId,
                VideoId = message.VideoId,
                Operation = message.Operation,
                ProgressPercent = message.ProgressPercent,
                Status = message.Status
            },
            cancellationToken);
    }
}
