using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class TrackForwardCompletedNotificationHandler(
    LongRunningJobsHub hub,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    : IMessageHandler<TrackForwardCompleted>
{
    public async Task HandleAsync(TrackForwardCompleted message, CancellationToken cancellationToken = default)
    {
        List<DetectedObjectDto> createdObjects = [];
        if (message.CreatedObjectIds.Count > 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            createdObjects = await db.DetectedObjects
                .Where(o => message.CreatedObjectIds.Contains(o.Id))
                .Select(o => o.ToDto())
                .ToListAsync(cancellationToken);
        }

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
                    : null,
                CreatedObjects = createdObjects
            },
            cancellationToken);
    }
}
