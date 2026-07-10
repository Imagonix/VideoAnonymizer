using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class TracksController(IMessagePublisher messagePublisher) : ControllerBase
{
    [HttpPost($"/{SharedConstants.Paths.Analyzed}/{{videoId:guid}}/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}")]
    public async Task<IActionResult> TrackForward(
        [FromRoute] Guid videoId,
        [FromBody] TrackForwardRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobId = request.JobId ?? Guid.NewGuid();

            var job = new TrackForwardJob
            {
                JobId = jobId,
                VideoId = videoId,
                AddedAt = DateTimeOffset.UtcNow,
                SeedDetectionId = request.SeedDetectionId,
                SeedFrameIndex = request.SeedFrameIndex,
                SeedTimeMs = request.SeedTimeMs,
                SeedBoundingBoxX = request.BoundingBox?.X,
                SeedBoundingBoxY = request.BoundingBox?.Y,
                SeedBoundingBoxWidth = request.BoundingBox?.Width,
                SeedBoundingBoxHeight = request.BoundingBox?.Height,
                ObjectClass = request.ObjectClass,
                InitialTrackId = request.TrackId,
                PersistEveryMs = request.PersistEveryMs,
                MaxLostDurationMs = request.MaxLostDurationMs,
                RecoveryDetectorIntervalMs = request.RecoveryDetectorIntervalMs,
                TrackerType = request.TrackerType,
                ConflictMode = request.ConflictMode,
                SearchAreaExpansion = request.SearchAreaExpansion,
                MaxTrackDurationMs = request.MaxTrackDurationMs
            };

            await messagePublisher.PublishAsync(
                RabbitMQConstants.RoutingKeys.TrackForward,
                job,
                cancellationToken);

            return Accepted(new ApiResponse<TrackForwardJobDto>
            {
                IsSuccess = true,
                Payload = new TrackForwardJobDto { JobId = jobId }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }
}
