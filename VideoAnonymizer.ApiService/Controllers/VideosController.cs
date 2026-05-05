using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class VideosController(
    IMessagePublisher messagePublisher,
    IWebHostEnvironment environment,
    VideoDataService videoDataService) : ControllerBase
{
    private static readonly string[] AllowedVideoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".webm"];

    [HttpPost($"{SharedConstants.Paths.Analyze}")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Analyze(
        IFormFile video,
        CancellationToken cancellationToken,
        [FromQuery]
        [Range(50, 5000, ErrorMessage = "detectionIntervalMs must be between 50 and 5000 ms")]
        int detectionIntervalMs = 100)
    {
        if (video is null || video.Length == 0)
            return BadRequest("No video uploaded.");

        var extension = Path.GetExtension(video.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedVideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported file type.");
        }

        (Guid videoId, string fullPath) = await videoDataService.SaveVideoFileAndCreateDbEntry(
            video,
            video.FileName,
            extension,
            environment.ContentRootPath,
            cancellationToken);

        await messagePublisher.PublishAsync(
            RabbitMQConstants.RoutingKeys.Analyze,
            new AnalyzeVideo(videoId, fullPath, DateTime.UtcNow, detectionIntervalMs),
            cancellationToken);

        return Ok(new ApiResponse<Guid>
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
            return Ok(new ApiResponse<List<AnalyzedFrameDto>>
            {
                IsSuccess = true,
                Payload = video,
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet($"{SharedConstants.Paths.Videos}")]
    public async Task<IActionResult> GetVideos()
    {
        var videos = await videoDataService.GetVideos();
        return Ok(new ApiResponse<List<VideoDto>> { IsSuccess = true, Payload = videos });
    }

    [HttpPut($"{SharedConstants.Paths.Video}/{{videoId:guid}}/{SharedConstants.Paths.VideoSettings}")]
    public async Task<IActionResult> UpdateVideoSettings([FromRoute] Guid videoId, [FromBody] AnonymizationSettingsDto settings)
    {
        try
        {
            await videoDataService.UpdateVideoSettings(videoId, settings.BlurSizePercent, settings.TimeBufferMs);
            return Ok(new ApiResponse<object> { IsSuccess = true });
        }
        catch (NotFoundException)
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
                return NotFound();

            return PhysicalFile(
                videoPath,
                VideoFileContentTypes.FromPath(videoPath),
                Path.GetFileName(videoPath),
                enableRangeProcessing: true);
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
                cancellationToken);

            return Ok(new ApiResponse<Guid>
            {
                Payload = jobId,
                Message = "video creation started"
            });
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
                return NotFound();

            return PhysicalFile(
                videoPath,
                VideoFileContentTypes.FromPath(videoPath),
                Path.GetFileName(videoPath),
                enableRangeProcessing: true);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
