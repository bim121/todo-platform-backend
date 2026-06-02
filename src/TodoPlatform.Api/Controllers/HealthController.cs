using Microsoft.AspNetCore.Mvc;

namespace TodoPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
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
