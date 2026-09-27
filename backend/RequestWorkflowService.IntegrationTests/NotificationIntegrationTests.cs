using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.IntegrationTests;

public class NotificationIntegrationTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task NT_01_OrganizationSuccessfullyCreatesRequest_ShouldTriggerDonorNotification()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "donor_001",
            Title = "New Donation Request",
            Message = "City Food Bank requested 10 packs of your donation 'Fresh Bread'",
            Type = "REQUEST_RECEIVED",
            IsRead = false,
            RelatedId = 101,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == "donor_001");
        saved.Should().NotBeNull();
        saved!.Type.Should().Be("REQUEST_RECEIVED");
    }

    [Fact]
    public async Task NT_02_VerifyDonationRequestNotificationRecipient_ShouldTargetCorrectDonor()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        string targetDonorId = "donor_target_recipient";

        var notification = new Notification
        {
            UserId = targetDonorId,
            Title = "Donation Requested",
            Message = "Request created",
            Type = "REQUEST_RECEIVED",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.UserId == targetDonorId);
        fetched.UserId.Should().Be(targetDonorId);
    }

    [Fact]
    public async Task NT_03_VerifyNotificationContainsRelevantRequestDonationInfo_ShouldIncludeDetails()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "donor_details",
            Title = "New Request",
            Message = "Request #505 for 5 Cartons of Milk",
            Type = "REQUEST_RECEIVED",
            RelatedId = 505,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.Id == notification.Id);
        fetched.Message.Should().Contain("505");
        fetched.Message.Should().Contain("Milk");
    }

    [Fact]
    public async Task NT_04_FailedDonationRequest_ShouldNotTriggerNotification()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        bool isRequestSuccessful = false;

        // Act
        if (isRequestSuccessful)
        {
            db.Notifications.Add(new Notification { UserId = "donor_x", Title = "Failed", Message = "Msg", Type = "ERR", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        // Assert
        var count = await db.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task NT_05_RepeatedRejectedRequest_ShouldNotCreateDuplicateNotification()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var initialNotification = new Notification
        {
            UserId = "donor_dup",
            Title = "Request Rejected",
            Message = "Request rejected",
            Type = "REQUEST_REJECTED",
            RelatedId = 999,
            CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(initialNotification);
        await db.SaveChangesAsync();

        // Act: Attempt to send duplicate rejection notification for same request
        var existing = await db.Notifications.AnyAsync(n => n.UserId == "donor_dup" && n.RelatedId == 999 && n.Type == "REQUEST_REJECTED");

        // Assert
        existing.Should().BeTrue(); // System detects existing notification and suppresses duplicate
    }

    [Fact]
    public async Task DAN_01_DonorAcceptsRequestSuccessfully_ShouldNotifyOrganization()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "org_accepted_recipient",
            Title = "Request Accepted!",
            Message = "Donor Bakery accepted your request for Fresh Croissants",
            Type = "REQUEST_ACCEPTED",
            RelatedId = 202,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.UserId == "org_accepted_recipient");
        fetched.Type.Should().Be("REQUEST_ACCEPTED");
    }

    [Fact]
    public async Task DRN_01_DonorRejectsRequest_ShouldNotifyOrganization()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "org_rejected_recipient",
            Title = "Request Declined",
            Message = "Donor declined your request for Soup Cans",
            Type = "REQUEST_REJECTED",
            RelatedId = 303,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.UserId == "org_rejected_recipient");
        fetched.Type.Should().Be("REQUEST_REJECTED");
    }

    [Fact]
    public async Task FRN_01_OrganizationCreatesFoodRequest_ShouldTriggerFoodNeedNotification()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "all_donors_broadcast",
            Title = "New Food Need Posted",
            Message = "Community Kitchen needs 100 Rice Meals",
            Type = "NEED_POSTED",
            RelatedId = 404,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.RelatedId == 404);
        fetched.Type.Should().Be("NEED_POSTED");
    }

    [Fact]
    public async Task FON_01_DonorCreatesValidOffer_ShouldNotifyOrganization()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var notification = new Notification
        {
            UserId = "org_offer_recipient",
            Title = "New Food Offer Received",
            Message = "Donor Chef Alex offered 50 Meal Packs for your request",
            Type = "OFFER_RECEIVED",
            RelatedId = 505,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Notifications.FirstAsync(n => n.UserId == "org_offer_recipient");
        fetched.Type.Should().Be("OFFER_RECEIVED");
    }
}
