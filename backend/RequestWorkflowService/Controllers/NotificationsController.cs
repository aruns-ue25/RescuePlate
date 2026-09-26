using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Models;

namespace RequestWorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly RequestDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(RequestDbContext context, ILogger<NotificationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all notifications for the current authenticated user/org/donor.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCallerUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<List<NotificationResponseDto>>.Fail("Authentication claims missing."));
        }

        var list = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                RelatedId = n.RelatedId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<NotificationResponseDto>>.Ok(list));
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCallerUserId();
        var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notif == null)
        {
            return NotFound(ApiResponse<string>.Fail("Notification not found."));
        }

        notif.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Marked notification as read."));
    }

    /// <summary>
    /// Marks all notifications for current user as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCallerUserId();
        var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
        }
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("All notifications marked as read."));
    }

    /// <summary>
    /// Clears/deletes all read notifications for current user.
    /// </summary>
    [HttpDelete("clear-read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearReadNotifications()
    {
        var userId = GetCallerUserId();
        var readNotifs = await _context.Notifications.Where(n => n.UserId == userId && n.IsRead).ToListAsync();
        _context.Notifications.RemoveRange(readNotifs);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Cleared read notifications."));
    }

    private string GetCallerUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("id")?.Value 
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            var headerId = Request.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerId))
            {
                userIdClaim = headerId;
            }
        }
        return userIdClaim ?? string.Empty;
    }
}
