using MassTransit;
using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    //[Route("api/anonymizer")]
    public class VideoAnonymizerApi(IPublishEndpoint publishEndpoint) : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }

        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] string videoPath)
        {
            var jobId = Guid.NewGuid();
            publishEndpoint.Publish(new AnalyzeVideo(jobId, videoPath, DateTime.Now));
            return Ok(new
            {
                jobId = jobId,
                message = "analysis started"
            });
        }

        [HttpGet("analyzed/{jobId:guid}")]
        public IActionResult GetAnalyzedVideo([FromRoute] Guid jobId)
        {
            throw new NotImplementedException();
        }

        [HttpPost("anonymizedVideo/{jobId:guid}")]
        public IActionResult AnalyzedResult([FromRoute] Guid jobId)
        {
            throw new NotImplementedException();
        }
    }
}
