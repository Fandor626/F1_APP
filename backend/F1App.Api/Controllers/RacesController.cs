using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/races")]
public class RacesController(RaceScheduleService raceScheduleService, RaceReplayService raceReplayService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RaceWeekendSummary>>> Get(CancellationToken cancellationToken)
    {
        var schedule = await raceScheduleService.GetCurrentSeasonScheduleAsync(cancellationToken);
        return Ok(schedule);
    }

    [HttpGet("{round:int}")]
    public async Task<ActionResult<RaceWeekendDetail>> GetDetail(int round, CancellationToken cancellationToken)
    {
        var detail = await raceScheduleService.GetRaceDetailAsync(round, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("last-result")]
    public async Task<ActionResult<LastRaceResult>> GetLastResult(CancellationToken cancellationToken)
    {
        var result = await raceScheduleService.GetLastRaceResultAsync(cancellationToken);
        return result is null ? NoContent() : Ok(result);
    }

    // {season} is part of the route for REST/AD-2 shape consistency but isn't
    // used in the lookup — Ergast round-result queries are already scoped to
    // the current season implicitly, matching every other round-based lookup
    // in this controller (GetDetail above takes no season param either).
    [HttpGet("{season:int}/{round:int}/replay")]
    public async Task<ActionResult<IReadOnlyList<RaceStateSnapshot>>> GetReplay(
        int season, int round, CancellationToken cancellationToken)
    {
        var frames = await raceReplayService.GetReplayAsync(round, cancellationToken);
        return frames is null ? NotFound() : Ok(frames);
    }
}
