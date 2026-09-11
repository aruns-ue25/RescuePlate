using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _env;

    public ProfileController(IProfileService profileService, IWebHostEnvironment env)
    {
        _profileService = profileService;
        _env = env;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid session." });
        }

        var profile = await _profileService.GetProfileByUserIdAsync(userId);
        if (profile == null)
        {
            return NotFound(new { success = false, message = "Profile not found." });
        }

        return Ok(new { success = true, data = profile });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid session." });
        }

        var (success, message, data) = await _profileService.UpdateProfileAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message, data });
    }

    [HttpPost("me/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile? file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid session." });
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var (success, message, pictureUrl) = await _profileService.UploadProfilePictureAsync(userId, file, webRoot);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { 
            success = true, 
            message, 
            data = new { profilePictureUrl = pictureUrl }, 
            profilePictureUrl = pictureUrl 
        });
    }

    [HttpDelete("me/picture")]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid session." });
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var (success, message) = await _profileService.RemoveProfilePictureAsync(userId, webRoot);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message });
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(Guid userId)
    {
        var profile = await _profileService.GetProfileByUserIdAsync(userId);
        if (profile == null)
        {
            return NotFound(new { success = false, message = "User profile not found." });
        }

        // Public safe view
        return Ok(new
        {
            success = true,
            data = new
            {
                profile.UserId,
                profile.Role,
                profile.BusinessOrOrgName,
                profile.Address,
                profile.BioOrDescription,
                profile.DonorType,
                profile.AcceptedFoodCategories,
                profile.ProfilePictureUrl
            }
        });
    }
}
