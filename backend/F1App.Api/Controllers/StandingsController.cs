using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/standings")]
public class StandingsController(StandingsService standingsService) : ControllerBase
{
    [HttpGet("drivers")]
    public async Task<ActionResult<IReadOnlyList<DriverStanding>>> GetDrivers(CancellationToken cancellationToken)
    {
        var standings = await standingsService.GetCurrentDriverStandingsAsync(cancellationToken);
        return Ok(standings);
    }

    [HttpGet("constructors")]
    public async Task<ActionResult<IReadOnlyList<ConstructorStanding>>> GetConstructors(CancellationToken cancellationToken)
    {
        var standings = await standingsService.GetCurrentConstructorStandingsAsync(cancellationToken);
        return Ok(standings);
    }
}
