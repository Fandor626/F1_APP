using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriversController(DriverProfileService driverProfileService, HeadToHeadService headToHeadService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverOption>>> GetAllDrivers(CancellationToken cancellationToken)
    {
        var drivers = await headToHeadService.GetAllDriversAsync(cancellationToken);
        return Ok(drivers);
    }

    [HttpGet("compare")]
    public async Task<ActionResult<HeadToHeadComparison>> Compare(
        [FromQuery] string driverA, [FromQuery] string driverB,
        [FromQuery] int? season, [FromQuery] string? circuitId,
        CancellationToken cancellationToken)
    {
        var comparison = await headToHeadService.CompareAsync(driverA, driverB, season, circuitId, cancellationToken);
        return comparison is null ? NotFound() : Ok(comparison);
    }

    [HttpGet("{driverId}")]
    public async Task<ActionResult<DriverProfile>> GetProfile(string driverId, CancellationToken cancellationToken)
    {
        var profile = await driverProfileService.GetDriverProfileAsync(driverId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
