using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new HealthResponse("ok"));
}

public record HealthResponse(string Status);
