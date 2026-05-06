using Microsoft.AspNetCore.Mvc;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.Controllers;

[ApiController]
public sealed class AppStateController(StateDataService stateDataService) : ControllerBase
{
    [HttpGet($"{SharedConstants.Paths.AppState}")]
    public async Task<IActionResult> GetAppState(CancellationToken cancellationToken)
    {
        var appState = await stateDataService.LoadState(cancellationToken);
        return Ok(appState);
    }
}
