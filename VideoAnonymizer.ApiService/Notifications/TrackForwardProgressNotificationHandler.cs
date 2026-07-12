using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Notifications;

public sealed class TrackForwardProgressNotificationHandler(
    LongRunningJobsHub hub,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    : IMessageHandler<TrackForwardProgress>
{
    public async Task HandleAsync(TrackForwardProgress message, CancellationToken cancellationToken = default)
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
            SharedConstants.SignalR.Messages.TrackForwardProgress,
            new TrackForwardProgressMessage
            {
                JobId = message.JobId,
                VideoId = message.VideoId,
                Status = message.Status,
                Error = message.Error,
                TrackId = message.TrackId,
                GapStartTimeMs = message.GapStartTimeMs,
                GapEndTimeMs = message.GapEndTimeMs,
                IsFinal = message.IsFinal,
                CreatedObjects = createdObjects
            },
            cancellationToken);
    }
}
