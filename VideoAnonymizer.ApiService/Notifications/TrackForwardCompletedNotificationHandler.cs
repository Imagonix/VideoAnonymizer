using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class TrackForwardCompletedNotificationHandler(LongRunningJobsHub hub)
    : IMessageHandler<TrackForwardCompleted>
{
    public async Task HandleAsync(TrackForwardCompleted message, CancellationToken cancellationToken = default)
    {
        await hub.Clients.All.SendAsync(
            SharedConstants.SignalR.Messages.TrackForwardCompleted,
            new TrackForwardCompletedMessage
            {
                JobId = message.JobId,
                VideoId = message.VideoId,
                Status = message.Status,
                Error = message.Error,
                Result = message.TrackId.HasValue
                    ? new TrackForwardResponseDto { TrackId = message.TrackId.Value }
                    : null
            },
            cancellationToken);
    }
}
