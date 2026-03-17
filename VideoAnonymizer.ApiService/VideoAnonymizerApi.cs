using Microsoft.AspNetCore.Mvc;

namespace VideoAnonymizer.ApiService
{
    [ApiController]
    [Route("api/anonymizer")]
    public class VideoAnonymizerApi : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }

        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] string videoPath)
        {
            return Ok(new
            {
                jobId = Guid.NewGuid(),
                message = "Analysis started"
            });
        }

        [HttpGet("analyze/{jobId:guid}")]
        public IActionResult GetAnalyzedVideo([FromRoute] Guid jobId)
        {
            return Ok(new
            {
                jobId = Guid.NewGuid(),
                message = "Analysis started"
            });
        }

        [HttpPost("anonymizedVideo/{jobId:guid}")]
        public IActionResult AnalyzedResult([FromRoute] Guid jobId)
        {
            return Ok(new
            {
                jobId = Guid.NewGuid(),
                message = "Analysis started"
            });
        }
    }
}
