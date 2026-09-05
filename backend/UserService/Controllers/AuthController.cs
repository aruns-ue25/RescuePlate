using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, data) = await _authService.RegisterAsync(dto);
        if (!success)
        {
            return Conflict(new { success = false, message });
        }

        return StatusCode(201, new { success = true, message, data });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message, data) = await _authService.LoginAsync(dto);
        if (!success)
        {
            return Unauthorized(new { success = false, message });
        }

        return Ok(new { success = true, message, data });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Session termination / client clears JWT token
        return Ok(new { success = true, message = "Successfully signed out of RescuePlate session." });
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid user session." });
        }

        var (success, message) = await _authService.DeleteAccountAsync(userId, dto.Password);
        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, message });
    }
}
