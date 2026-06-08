using Microsoft.AspNetCore.Mvc;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Application health probe for load balancers and smoke tests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns service name, status, and UTC timestamp.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "TodoPlatform.Api",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
