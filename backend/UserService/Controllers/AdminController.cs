using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly RescuePlateDbContext _db;
    private readonly IAdminService _adminService;

    public AdminController(RescuePlateDbContext db, IAdminService adminService)
    {
        _db = db;
        _adminService = adminService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var (success, message, data) = await _adminService.AdminLoginAsync(dto, clientIp);

        if (!success)
        {
            if (message.Contains("inactive"))
            {
                return Unauthorized(new { success = false, message });
            }
            if (message.Contains("Access denied"))
            {
                return StatusCode(403, new { success = false, message });
            }
            return Unauthorized(new { success = false, message });
        }

        return Ok(new { success = true, message, data });
    }

    [HttpPost("verify-access-key")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyAccessKey([FromBody] VerifyAccessKeyDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var (success, message, data) = await _adminService.VerifyAccessKeyAsync(dto, clientIp);

        if (!success)
        {
            if (message.Contains("locked out"))
            {
                return StatusCode(429, new { success = false, message });
            }
            if (message.Contains("denied"))
            {
                return StatusCode(403, new { success = false, message });
            }
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, data });
    }

    [HttpGet("users")]
    [Authorize(Policy = "VerifiedAdminOnly")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.OrganizationProfile)
            .Select(u => new
            {
                u.Id,
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                BusinessName = u.DonorProfile != null ? u.DonorProfile.BusinessName :
                               u.OrganizationProfile != null ? u.OrganizationProfile.OrganizationName : "System Admin",
                Location = u.DonorProfile != null ? u.DonorProfile.Address :
                           u.OrganizationProfile != null ? u.OrganizationProfile.Address : "N/A"
            })
            .ToListAsync();

        return Ok(new { success = true, count = users.Count, data = users });
    }

    [HttpPatch("users/{userId:guid}/status")]
    [Authorize(Policy = "VerifiedAdminOnly")]
    public async Task<IActionResult> ToggleUserStatus(Guid userId, [FromBody] UserStatusUpdateDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { success = false, message = "User not found." });
        }

        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"User account has been {(user.IsActive ? "activated" : "deactivated")}."
        });
    }

    [HttpGet("monitoring/activity")]
    [Authorize(Policy = "VerifiedAdminOnly")]
    public async Task<IActionResult> GetActivityLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (success, message, data) = await _adminService.GetActivityLogsAsync(page, pageSize);
        return Ok(new { success = true, message, data });
    }
}
