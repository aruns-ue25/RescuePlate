using Microsoft.AspNetCore.Mvc;

namespace RequestWorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            success = true,
            service = "RescuePlate.RequestWorkflowService",
            version = "1.0.0",
            status = "Online",
            database = "PostgreSQL",
            architecture = "Microservice",
            port = 5002,
            timestamp = DateTime.UtcNow
        });
    }
}
