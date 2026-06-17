using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/races/{round:int}/win-probability")]
public class WinProbabilityController(WinProbabilityService winProbabilityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WinProbabilityEntry>>> Get(int round, CancellationToken cancellationToken)
    {
        var results = await winProbabilityService.GetWinProbabilitiesAsync(round, cancellationToken);
        return Ok(results);
    }
}
