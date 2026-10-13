using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Controllers;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class NotificationsControllerMockUnitTests
{
    [Fact]
    public async Task GetMyNotifications_ReturnsNotificationsForCallerUser()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Notifications.AddRange(
            new Notification
            {
                UserId = "user_alpha",
                Title = "Alert 1",
                Message = "Message 1",
                Type = "INFO",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Notification
            {
                UserId = "user_alpha",
                Title = "Alert 2",
                Message = "Message 2",
                Type = "INFO",
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                UserId = "user_beta",
                Title = "Other User Alert",
                Message = "Should not see",
                Type = "INFO",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateNotificationsController(db, callerId: "user_alpha");

        // Act
        var result = await controller.GetMyNotifications();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<NotificationResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(2);
        ok.Data!.All(n => n.UserId == "user_alpha").Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_ExistingNotification_MarksAsRead()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var notif = new Notification
        {
            UserId = "user_alpha",
            Title = "Unread Alert",
            Message = "Need reading",
            Type = "ALERT",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notif);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateNotificationsController(db, callerId: "user_alpha");

        // Act
        var result = await controller.MarkAsRead(notif.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updated = await db.Notifications.FirstAsync(n => n.Id == notif.Id);
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_ReturnsNotFound()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var controller = ControllerTestHelpers.CreateNotificationsController(db, callerId: "user_alpha");

        // Act
        var result = await controller.MarkAsRead(9999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnreadNotificationsAsRead()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Notifications.AddRange(
            new Notification
            {
                UserId = "user_alpha",
                Title = "Alert 1",
                Message = "Message 1",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                UserId = "user_alpha",
                Title = "Alert 2",
                Message = "Message 2",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateNotificationsController(db, callerId: "user_alpha");

        // Act
        var result = await controller.MarkAllAsRead();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var notifications = await db.Notifications.Where(n => n.UserId == "user_alpha").ToListAsync();
        notifications.All(n => n.IsRead).Should().BeTrue();
    }

    [Fact]
    public async Task ClearReadNotifications_DeletesOnlyReadNotifications()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Notifications.AddRange(
            new Notification
            {
                UserId = "user_alpha",
                Title = "Read Notification",
                Message = "Done",
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                UserId = "user_alpha",
                Title = "Unread Notification",
                Message = "Still active",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateNotificationsController(db, callerId: "user_alpha");

        // Act
        var result = await controller.ClearReadNotifications();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var remaining = await db.Notifications.Where(n => n.UserId == "user_alpha").ToListAsync();
        remaining.Should().HaveCount(1);
        remaining.First().IsRead.Should().BeFalse();
    }
}
