using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    public class VideoAnonymizerApi(IPublishEndpoint publishEndpoint, IWebHostEnvironment environment, IDbContextFactory<VideoAnonymizerDbContext> dbFactory) : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromForm] IFormFile video, CancellationToken cancellationToken)
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

            var videoEntity = new Video();

            var uploadsRoot = Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{videoEntity.Id}{extension}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);
            videoEntity.Path = fullPath;

            await using (var fileStream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 64,
                useAsync: true))
            {
                await video.CopyToAsync(fileStream, cancellationToken);
            }

            using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            await db.Videos.AddAsync(videoEntity, cancellationToken);
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(
                new AnalyzeVideo(videoEntity.Id, fullPath, DateTime.UtcNow),
                cancellationToken);

            return Ok(new ApiResponse<Guid>()
            {
                Payload = videoEntity.Id,
                Message = "analysis started"
            });
        }

        [HttpGet("analyzed/{videoId:guid}")]
        public async Task<IActionResult> GetAnalyzedVideo([FromRoute] Guid videoId)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var video = db.Videos.Where(x => x.Id.Equals(videoId)).Include(x => x.AnalyzedFrames).ThenInclude(x => x.DetectedObjects).SingleOrDefault();
            return Ok(
                new ApiResponse<Video>() { 
                    IsSuccess = true, 
                    Payload = video,
                });
        }


        [HttpPost("anonymize")]
        public IActionResult Anonymize([FromBody] string videoPath)
        {
            var jobId = Guid.NewGuid();
            publishEndpoint.Publish(new AnalyzeVideo(jobId, videoPath, DateTime.Now));
            return Ok(new ApiResponse<Guid>()
            {
                Payload = jobId,
                Message = "analysis started"
            });
        }

        [HttpPost("anonymized/{jobId:guid}")]
        public IActionResult GetAnalyzedResult([FromRoute] Guid jobId)
        {
            throw new NotImplementedException();
        }
    }
}
