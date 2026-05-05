using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class DetectedObjectsController(DetectedObjectDataService detectedObjectDataService) : ControllerBase
{
    [HttpPost($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.AnalyzedFrame}/{{analyzedFrameId:guid}}/{SharedConstants.Paths.DetectedObject}")]
    public async Task<IActionResult> AddDetectedObject([FromRoute] Guid videoId, [FromRoute] Guid analyzedFrameId, [FromBody] DetectedObjectDto dto)
    {
        try
        {
            if (analyzedFrameId != dto.AnalyzedFrameId)
                return BadRequest("analyzedFrameId in URL does not match request body");

            var created = await detectedObjectDataService.AddDetectedObject(videoId, dto);
            return CreatedAtAction(nameof(AddDetectedObject), new { videoId, analyzedFrameId },
                new ApiResponse<DetectedObjectDto> { IsSuccess = true, Payload = created });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.AnalyzedFrame}/{{analyzedFrameId:guid}}/{SharedConstants.Paths.DetectedObject}/{{objectId:guid}}")]
    public async Task<IActionResult> UpdateDetectedObject([FromRoute] Guid videoId, [FromRoute] Guid analyzedFrameId, [FromRoute] Guid objectId, [FromBody] DetectedObjectDto dto)
    {
        try
        {
            if (analyzedFrameId != dto.AnalyzedFrameId || objectId != dto.Id)
                return BadRequest("URL parameters do not match request body");

            var updated = await detectedObjectDataService.UpdateDetectedObject(videoId, dto);
            return Ok(new ApiResponse<DetectedObjectDto> { IsSuccess = true, Payload = updated });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.AnalyzedFrame}/{{analyzedFrameId:guid}}/{SharedConstants.Paths.DetectedObject}/{{objectId:guid}}")]
    public async Task<IActionResult> DeleteDetectedObject([FromRoute] Guid videoId, [FromRoute] Guid analyzedFrameId, [FromRoute] Guid objectId)
    {
        try
        {
            await detectedObjectDataService.DeleteDetectedObject(videoId, analyzedFrameId, objectId);
            return Ok(new ApiResponse<object> { IsSuccess = true });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.DetectedObjects}")]
    public async Task<IActionResult> BulkUpdateDetectedObjects([FromRoute] Guid videoId, [FromBody] List<DetectedObjectDto> dtos)
    {
        try
        {
            await detectedObjectDataService.BulkUpdateDetectedObjects(videoId, dtos);
            return Ok(new ApiResponse<object> { IsSuccess = true });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
