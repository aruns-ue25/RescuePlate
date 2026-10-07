using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryWorkflowService.Data;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.Models;

namespace DeliveryWorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly DeliveryDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(DeliveryDbContext context, ILogger<NotificationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all delivery notifications for the authenticated caller.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DeliveryNotificationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCallerUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<List<DeliveryNotificationResponseDto>>.Fail("Authentication claims missing."));
        }

        var notifications = await _context.DeliveryNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new DeliveryNotificationResponseDto
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

        return Ok(ApiResponse<List<DeliveryNotificationResponseDto>>.Ok(notifications));
    }

    /// <summary>
    /// Marks a single notification as read for the authenticated caller.
    /// </summary>
    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCallerUserId();
        var notification = await _context.DeliveryNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return NotFound(ApiResponse<string>.Fail("Notification not found."));
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Marked notification as read."));
    }

    /// <summary>
    /// Marks all unread notifications for the authenticated caller as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCallerUserId();
        var unread = await _context.DeliveryNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("All delivery notifications marked as read."));
    }

    /// <summary>
    /// Clears/deletes all read notifications for the authenticated caller.
    /// </summary>
    [HttpDelete("clear-read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearReadNotifications()
    {
        var userId = GetCallerUserId();
        var readNotifs = await _context.DeliveryNotifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync();

        _context.DeliveryNotifications.RemoveRange(readNotifs);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Cleared read delivery notifications."));
    }

    private string GetCallerUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        return userIdClaim ?? string.Empty;
    }
}
