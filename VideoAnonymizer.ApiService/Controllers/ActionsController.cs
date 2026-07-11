using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class ActionsController(EditorActionDataService actionDataService) : ControllerBase
{
    [HttpPost($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.Actions}")]
    public async Task<IActionResult> RecordAction(
        [FromRoute] Guid videoId,
        [FromBody] RecordActionRequest request)
    {
        try
        {
            var action = await actionDataService.AddActionAsync(videoId, request.ActionType, request.Data);
            return Ok(new ApiResponse<EditorActionDto>
            {
                IsSuccess = true,
                Payload = new EditorActionDto
                {
                    Id = action.Id,
                    VideoId = action.VideoId,
                    ActionType = action.ActionType,
                    SequenceNumber = action.SequenceNumber,
                    CreatedAt = action.CreatedAt,
                    Undone = action.Undone,
                    Data = action.Data
                }
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.Actions}")]
    public async Task<IActionResult> GetActions([FromRoute] Guid videoId)
    {
        try
        {
            var actions = await actionDataService.GetActionsAsync(videoId);
            return Ok(new ApiResponse<List<EditorActionDto>>
            {
                IsSuccess = true,
                Payload = actions.Select(a => new EditorActionDto
                {
                    Id = a.Id,
                    VideoId = a.VideoId,
                    ActionType = a.ActionType,
                    SequenceNumber = a.SequenceNumber,
                    CreatedAt = a.CreatedAt,
                    Undone = a.Undone,
                    Data = a.Data
                }).ToList()
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.Actions}/{{actionId:guid}}/{SharedConstants.Paths.Undone}")]
    public async Task<IActionResult> ToggleUndone(
        [FromRoute] Guid videoId,
        [FromRoute] Guid actionId,
        [FromBody] ToggleUndoneRequest request)
    {
        try
        {
            await actionDataService.ToggleUndoneAsync(videoId, actionId, request.Undone);
            return Ok(new ApiResponse<object> { IsSuccess = true });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
