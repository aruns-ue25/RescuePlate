using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DeliveryWorkflowService.Data;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.IntegrationTests.Helpers;
using DeliveryWorkflowService.Models;
using Xunit;

namespace DeliveryWorkflowService.IntegrationTests;

public class NotificationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task N_I01_CompleteEachWorkflowTransition_NotificationsPersistCorrectly()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 6001, donationId: 7001, donorId: "donor_notif_it", orgId: "org_notif_it", status: "ACCEPTED");

        var donorClient = _factory.CreateClient();
        donorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("donor_notif_it", "DONOR"));

        var orgClient = _factory.CreateClient();
        orgClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("org_notif_it", "ORGANIZATION"));

        // 1. Arrange
        var arrangeDto = new ArrangeCollectionDto
        {
            RequestId = 6001,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var arrangeRes = await donorClient.PostAsJsonAsync("/api/deliveries/arrange", arrangeDto);
        var arrangeBody = await arrangeRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        int deliveryId = arrangeBody!.Data!.Id;

        // 2. Collect
        await donorClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/collect", new RecordCollectionDto());

        // 3. Receive
        await orgClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/receive", new ConfirmReceiptDto());

        // 4. Complete
        await donorClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/complete", new CompleteDonationDto());

        // Assert
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var notifications = await db.DeliveryNotifications.ToListAsync();
        
        notifications.Should().Contain(n => n.UserId == "donor_notif_it" && n.Type == "COLLECTION_ARRANGED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "org_notif_it" && n.Type == "COLLECTION_ARRANGED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "donor_notif_it" && n.Type == "DONATION_COLLECTED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "org_notif_it" && n.Type == "DONATION_COLLECTED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "donor_notif_it" && n.Type == "RECEIPT_CONFIRMED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "donor_notif_it" && n.Type == "DONATION_COMPLETED" && n.RelatedId == deliveryId);
        notifications.Should().Contain(n => n.UserId == "org_notif_it" && n.Type == "DONATION_COMPLETED" && n.RelatedId == deliveryId);
    }

    [Fact]
    public async Task N_I02_AttemptInvalidTransition_NoNotificationsAdded()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("donor_invalid", "DONOR"));

        // Act
        var res = await client.PostAsJsonAsync("/api/deliveries/9999/collect", new RecordCollectionDto());

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        (await db.DeliveryNotifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task N_I03_RequestNotificationsAsUserA_ReturnsOnlyUserARecordsSortedNewestFirst()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            var now = DateTime.UtcNow;
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_A_it", Title = "T1", Message = "M1", Type = "COLLECTION_ARRANGED", RelatedId = 10, CreatedAt = now.AddMinutes(-10) },
                new DeliveryNotification { UserId = "user_A_it", Title = "T2", Message = "M2", Type = "DONATION_COLLECTED", RelatedId = 10, CreatedAt = now.AddMinutes(-5) },
                new DeliveryNotification { UserId = "user_B_it", Title = "T3", Message = "M3", Type = "COLLECTION_ARRANGED", RelatedId = 20, CreatedAt = now }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_A_it"));

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeliveryNotificationResponseDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().HaveCount(2);
        body.Data![0].Title.Should().Be("T2");
        body.Data[1].Title.Should().Be("T1");
    }

    [Fact]
    public async Task N_I04_UserHasNoNotifications_Returns200WithEmptyData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_empty_it"));

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeliveryNotificationResponseDto>>>();
        body!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task N_I05_MarkOwnNotificationAsRead_PersistsIsReadTrue()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        int notifId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            var notif = new DeliveryNotification { UserId = "user_read_it", Title = "T", Message = "M", Type = "T", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();
            notifId = notif.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_read_it"));

        // Act
        var response = await client.PatchAsync($"/api/notifications/{notifId}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scopeAssert = _factory.Services.CreateScope();
        var dbAssert = scopeAssert.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var updated = await dbAssert.DeliveryNotifications.FirstAsync(n => n.Id == notifId);
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task N_I06_AttemptToMarkAnotherUsersNotificationAsRead_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        int notifId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            var notif = new DeliveryNotification { UserId = "user_victim", Title = "T", Message = "M", Type = "T", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();
            notifId = notif.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_attacker"));

        // Act
        var response = await client.PatchAsync($"/api/notifications/{notifId}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task N_I07_MarkAllAsRead_UpdatesOnlyCallersRecords()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_all_A", Title = "T1", Message = "M1", Type = "T1", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_all_B", Title = "TB", Message = "MB", Type = "TB", RelatedId = 2, IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_all_A"));

        // Act
        var response = await client.PatchAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scopeAssert = _factory.Services.CreateScope();
        var dbAssert = scopeAssert.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        (await dbAssert.DeliveryNotifications.FirstAsync(n => n.UserId == "user_all_A")).IsRead.Should().BeTrue();
        (await dbAssert.DeliveryNotifications.FirstAsync(n => n.UserId == "user_all_B")).IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task N_I08_ClearReadNotifications_DeletesOnlyCallersReadRows()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_clr_A", Title = "Read", Message = "M", Type = "T", RelatedId = 1, IsRead = true, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_clr_A", Title = "Unread", Message = "M", Type = "T", RelatedId = 2, IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("user_clr_A"));

        // Act
        var response = await client.DeleteAsync("/api/notifications/clear-read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scopeAssert = _factory.Services.CreateScope();
        var dbAssert = scopeAssert.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var userARecords = await dbAssert.DeliveryNotifications.Where(n => n.UserId == "user_clr_A").ToListAsync();
        userARecords.Should().HaveCount(1);
        userARecords.First().Title.Should().Be("Unread");
    }

    [Fact]
    public async Task N_I09_DuplicateNotificationInsert_UniqueIndexPreventsDuplicate()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var notif1 = new DeliveryNotification { UserId = "user_dupe_idx", Title = "T", Message = "M", Type = "TYPE1", RelatedId = 99, IsRead = false, CreatedAt = DateTime.UtcNow };
        db.DeliveryNotifications.Add(notif1);
        await db.SaveChangesAsync();

        var notif2 = new DeliveryNotification { UserId = "user_dupe_idx", Title = "T2", Message = "M2", Type = "TYPE1", RelatedId = 99, IsRead = false, CreatedAt = DateTime.UtcNow };
        db.DeliveryNotifications.Add(notif2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
    }

    [Fact]
    public async Task N_I10_MissingBearerToken_Returns401()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient(); // No auth header

        // Act
        var resGet = await client.GetAsync("/api/notifications");
        var resPatch = await client.PatchAsync("/api/notifications/read-all", null);
        var resDelete = await client.DeleteAsync("/api/notifications/clear-read");

        // Assert
        resGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resPatch.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resDelete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
