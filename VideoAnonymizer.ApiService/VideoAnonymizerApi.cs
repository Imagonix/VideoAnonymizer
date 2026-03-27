using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using VideoAnonymizer.ApiService.DTO;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    public class VideoAnonymizerApi(IPublishEndpoint publishEndpoint, IWebHostEnvironment environment, VideoDataService videoDataService) : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }

        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Analyze(IFormFile video, CancellationToken cancellationToken)
        {
            if (video is null || video.Length == 0)
            {
                return BadRequest("No video uploaded.");
            }

            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var extension = Path.GetExtension(video.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Unsupported file type.");
            }

            (Guid videoId, string fullPath) = await videoDataService.SaveVideoFileAndCreateDbEntry(video, extension, environment.ContentRootPath, cancellationToken);

            await publishEndpoint.Publish(
                new AnalyzeVideo(videoId, fullPath, DateTime.UtcNow),
                cancellationToken);

            return Ok(new ApiResponse<Guid>()
            {
                Payload = videoId,
                Message = "analysis started"
            });
        }

        [HttpGet("analyzed/{videoId:guid}")]
        public async Task<IActionResult> GetAnalyzedVideo([FromRoute] Guid videoId)
        {
            try
            {
                var video = await videoDataService.GetAnalyzedVideo(videoId);
                return Ok(
                    new ApiResponse<List<AnalyzedFrameDto>>()
                    {
                        IsSuccess = true,
                        Payload = video,
                    });
            }
            catch (NotFoundException e)
            {
                return NotFound();
            }
        }


        [HttpPost("anonymize/{videoId:guid}")]
        public async Task<IActionResult> Anonymize([FromRoute] Guid videoId, [FromBody] List<AnalyzedFrameDto> frames)
        {
            var jobId = Guid.NewGuid();
            try
            {
                var video = await videoDataService.UpdateFramesAndObjects(videoId, frames);
                await publishEndpoint.Publish(new AnonomyzeVideo(jobId, video.Id, DateTime.Now));
                return Ok(new ApiResponse<Guid>()
                {
                    Payload = jobId,
                    Message = "video creation started"
                });
            } catch (NotFoundException e) { 
                return NotFound();
            }
        }

        [HttpGet("anonymized/{videoId:guid}")]
        public async Task<IActionResult> GetAnonymizedVideo([FromRoute] Guid videoId)
        {
            try
            {
                var video = await videoDataService.LoadAnonomyzedVideoPath(videoId);
                var fileName = Path.GetFileName(video);
                var stream = new FileStream(
                    video,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                return File(stream, "video/mp4", fileName);
            }
            catch (NotFoundException e)
            {
                return NotFound();
            }
        }
    }
}
