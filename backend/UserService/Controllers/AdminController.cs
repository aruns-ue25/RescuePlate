using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly RescuePlateDbContext _db;

    public AdminController(RescuePlateDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
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
    public async Task<IActionResult> ToggleUserStatus(Guid userId, [FromBody] StatusUpdateDto dto)
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
}

public class StatusUpdateDto
{
    public bool IsActive { get; set; }
}
