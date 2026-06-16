using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/races")]
public class RacesController(RaceScheduleService raceScheduleService) : ControllerBase
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
}
