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
}
