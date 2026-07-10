using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/standings")]
public class StandingsController(
    StandingsService standingsService,
    RaceScheduleService raceScheduleService,
    SeasonWrappedService seasonWrappedService) : ControllerBase
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

    [HttpGet("season-wrapped")]
    public async Task<ActionResult<SeasonWrapped?>> GetSeasonWrapped(CancellationToken cancellationToken)
    {
        var schedule = await raceScheduleService.GetCurrentSeasonScheduleAsync(cancellationToken);
        var wrapped = await seasonWrappedService.GetSeasonWrappedAsync(schedule, cancellationToken);
        return Ok(wrapped); // 200 + null body when season in progress — a real "there is nothing yet" state, not a 404
    }
}
