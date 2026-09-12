using Microsoft.AspNetCore.Mvc;

namespace DonationService.Controllers;

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
            service = "RescuePlate.DonationService",
            version = "1.0.0",
            status = "Online",
            database = "PostgreSQL",
            architecture = "Microservice",
            port = 5001,
            timestamp = DateTime.UtcNow
        });
    }
}
