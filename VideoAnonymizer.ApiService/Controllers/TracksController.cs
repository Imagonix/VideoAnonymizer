using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class TracksController(ForwardTrackingService forwardTrackingService) : ControllerBase
{
    [HttpPost($"/{SharedConstants.Paths.Analyzed}/{{videoId:guid}}/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}")]
    public async Task<IActionResult> TrackForward(
        [FromRoute] Guid videoId,
        [FromBody] TrackForwardRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await forwardTrackingService.TrackForwardAsync(videoId, request, cancellationToken);
            return Ok(new ApiResponse<TrackForwardResponseDto>
            {
                IsSuccess = true,
                Payload = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, ex.Response);
        }
    }
}
