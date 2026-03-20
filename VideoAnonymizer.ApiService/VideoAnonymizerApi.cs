using MassTransit;
using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    public class VideoAnonymizerApi(IPublishEndpoint publishEndpoint, IWebHostEnvironment environment) : ControllerBase
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

            var jobId = Guid.NewGuid();

            var uploadsRoot = Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{jobId}{extension}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

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

            await publishEndpoint.Publish(
                new AnalyzeVideo(jobId, fullPath, DateTime.UtcNow),
                cancellationToken);

            return Ok(new
            {
                jobId,
                message = "analysis started"
            });
        }

        [HttpGet("analyzed/{jobId:guid}")]
        public IActionResult GetAnalyzedVideo([FromRoute] Guid jobId)
        {
            throw new NotImplementedException();
        }


        [HttpPost("anonymize")]
        public IActionResult Anonymize([FromBody] string videoPath)
        {
            var jobId = Guid.NewGuid();
            publishEndpoint.Publish(new AnalyzeVideo(jobId, videoPath, DateTime.Now));
            return Ok(new
            {
                jobId = jobId,
                message = "analysis started"
            });
        }

        [HttpPost("anonymized/{jobId:guid}")]
        public IActionResult GetAnalyzedResult([FromRoute] Guid jobId)
        {
            throw new NotImplementedException();
        }
    }
}
