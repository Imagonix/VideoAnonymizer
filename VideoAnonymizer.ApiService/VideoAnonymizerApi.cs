using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.ComponentModel.DataAnnotations;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    public class VideoAnonymizerApi(IMessagePublisher messagePublisher, IWebHostEnvironment environment, VideoDataService videoDataService, StateDataService stateDataService) : ControllerBase
    {
        [HttpGet($"{SharedConstants.Paths.Health}")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }

        [HttpPost($"{SharedConstants.Paths.Analyze}")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> Analyze(IFormFile video, CancellationToken cancellationToken,
            [FromQuery]
            [Range(50, 5000, ErrorMessage = "detectionIntervalMs must be between 50 and 5000 ms")]
            int detectionIntervalMs = 100)
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

            await messagePublisher.PublishAsync(
                RabbitMQConstants.RoutingKeys.Analyze,
                new AnalyzeVideo(videoId, fullPath, DateTime.UtcNow, detectionIntervalMs),
                cancellationToken
            );

            return Ok(new ApiResponse<Guid>()
            {
                Payload = videoId,
                Message = "analysis started"
            });
        }

        [HttpGet($"{SharedConstants.Paths.Analyzed}/{{videoId:guid}}")]
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

        [HttpGet($"{SharedConstants.Paths.Video}/{{videoId:guid}}")]
        public async Task<IActionResult> GetOriginalVideo([FromRoute] Guid videoId)
        {
            try
            {
                var videoPath = await videoDataService.LoadOriginalVideoPath(videoId);

                if (!System.IO.File.Exists(videoPath))
                {
                    return NotFound();
                }

                var fileName = Path.GetFileName(videoPath);
                string contentType = GetContentType(videoPath);

                return PhysicalFile(
                    videoPath,
                    contentType,
                    fileName,
                    enableRangeProcessing: true
                );
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }


        [HttpPost($"{SharedConstants.Paths.Anonymize}/{{videoId:guid}}")]
        public async Task<IActionResult> Anonymize([FromRoute] Guid videoId, [FromBody] AnonymizeVideoRequestDto request, CancellationToken cancellationToken)
        {
            var jobId = Guid.NewGuid();
            try
            {
                var video = await videoDataService.UpdateFramesAndObjects(videoId, request);
                await messagePublisher.PublishAsync(
                    RabbitMQConstants.RoutingKeys.Anonymize,
                    new AnonymizeVideo(jobId, video.Id, DateTime.Now),
                    cancellationToken
                );
                return Ok(new ApiResponse<Guid>()
                {
                    Payload = jobId,
                    Message = "video creation started"
                });
            }
            catch (NotFoundException e)
            {
                return NotFound();
            }
        }

        [HttpPost($"/{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.AnalyzedFrame}/{{analyzedFrameId:guid}}/{SharedConstants.Paths.DetectedObject}")]
        public async Task<IActionResult> AddDetectedObject([FromRoute] Guid videoId, [FromRoute] Guid analyzedFrameId, [FromBody] DetectedObjectDto dto)
        {
            try
            {
                if (analyzedFrameId != dto.AnalyzedFrameId)
                    return BadRequest("analyzedFrameId in URL does not match request body");

                var created = await videoDataService.AddDetectedObject(videoId, dto);
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

                var updated = await videoDataService.UpdateDetectedObject(videoId, dto);
                return Ok(new ApiResponse<DetectedObjectDto> { IsSuccess = true, Payload = updated });
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet($"{SharedConstants.Paths.Anonymized}/{{videoId:guid}}")]
        public async Task<IActionResult> GetAnonymizedVideo([FromRoute] Guid videoId)
        {
            try
            {
                var videoPath = await videoDataService.LoadAnonomyzedVideoPath(videoId);

                if (!System.IO.File.Exists(videoPath))
                {
                    return NotFound();
                }
                var fileName = Path.GetFileName(videoPath);
                string contentType = GetContentType(videoPath);
                return PhysicalFile(videoPath, contentType, fileName, enableRangeProcessing: true);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        private static string GetContentType(string videoPath)
        {
            return videoPath.EndsWith(".webm") ? "video/webm" :
                              videoPath.EndsWith(".mov") ? "video/quicktime" :
                              "video/mp4";
        }

        [HttpGet($"{SharedConstants.Paths.AppState}")]
        public async Task<IActionResult> GetAppState(CancellationToken cancellationToken)
        {
            var appState = await stateDataService.LoadState(cancellationToken);
            return Ok(appState);
        }
    }
}
