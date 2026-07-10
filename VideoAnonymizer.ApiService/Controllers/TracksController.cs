using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class TracksController(TrackForwardJobQueue trackForwardJobQueue) : ControllerBase
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
            request.JobId = jobId;

            await trackForwardJobQueue.EnqueueAsync(
                new TrackForwardJob(jobId, videoId, request),
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
