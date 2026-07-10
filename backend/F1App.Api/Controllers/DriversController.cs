using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriversController(DriverProfileService driverProfileService) : ControllerBase
{
    [HttpGet("{driverId}")]
    public async Task<ActionResult<DriverProfile>> GetProfile(string driverId, CancellationToken cancellationToken)
    {
        var profile = await driverProfileService.GetDriverProfileAsync(driverId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
