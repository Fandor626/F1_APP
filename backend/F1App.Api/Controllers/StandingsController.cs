using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/standings")]
public class StandingsController(StandingsService standingsService, RaceScheduleService raceScheduleService) : ControllerBase
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

    [HttpGet("trajectory")]
    public async Task<ActionResult<IReadOnlyList<DriverTrajectory>>> GetTrajectory(CancellationToken cancellationToken)
    {
        var schedule = await raceScheduleService.GetCurrentSeasonScheduleAsync(cancellationToken);
        var trajectory = await standingsService.GetChampionshipTrajectoryAsync(schedule, cancellationToken);
        return Ok(trajectory);
    }
}
