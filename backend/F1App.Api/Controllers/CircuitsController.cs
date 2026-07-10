using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/circuits")]
public class CircuitsController(CircuitProfileService circuitProfileService) : ControllerBase
{
    [HttpGet("{circuitId}")]
    public async Task<ActionResult<CircuitProfile>> GetProfile(string circuitId, CancellationToken cancellationToken)
    {
        var profile = await circuitProfileService.GetCircuitProfileAsync(circuitId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
