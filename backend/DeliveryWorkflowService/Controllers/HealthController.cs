using Microsoft.AspNetCore.Mvc;

namespace DeliveryWorkflowService.Controllers;

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
            service = "RescuePlate.DeliveryWorkflowService",
            version = "1.0.0",
            status = "Online",
            architecture = "Microservice",
            port = 5003,
            timestamp = DateTime.UtcNow
        });
    }
}
