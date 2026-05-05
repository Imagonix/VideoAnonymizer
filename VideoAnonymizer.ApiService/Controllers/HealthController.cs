using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet($"{SharedConstants.Paths.Health}")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }
}
