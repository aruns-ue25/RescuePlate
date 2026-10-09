using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DeliveryWorkflowService.Controllers;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.Models;
using DeliveryWorkflowService.UnitTests.Helpers;
using Xunit;

namespace DeliveryWorkflowService.UnitTests;

public class NotificationUnitTests
{
    private (NotificationsController controller, DeliveriesController deliveriesController, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var notifLogger = NullLogger<NotificationsController>.Instance;
        var delivLogger = NullLogger<DeliveriesController>.Instance;
        var notifController = new NotificationsController(db, notifLogger);
        var delivController = new DeliveriesController(db, delivLogger);
        return (notifController, delivController, db, conn);
    }

    [Fact]
    public async Task N_U01_CollectionArrangementSucceeds_CreatesUnreadCollectionArrangedNotifications()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 901, donationId: 1001, donorId: "donor_n1", orgId: "org_n1");
            db.ChangeTracker.Clear();
            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_n1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 901,
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            await delivController.ArrangeCollection(dto);

            // Assert
            var notifications = await db.DeliveryNotifications.ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().AllSatisfy(n =>
            {
                n.Type.Should().Be("COLLECTION_ARRANGED");
                n.IsRead.Should().BeFalse();
            });
            notifications.Select(n => n.UserId).Should().BeEquivalentTo(new[] { "donor_n1", "org_n1" });
        }
    }

    [Fact]
    public async Task N_U02_CollectionIsRecorded_CreatesUnreadDonationCollectedNotifications()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 902,
                DonationId = 1002,
                DonorId = "donor_n2",
                DonorName = "Donor N2",
                OrganizationId = "org_n2",
                OrganizationName = "Org N2",
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "CollectionArranged",
                ArrangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_n2", "DONOR");

            // Act
            await delivController.RecordCollection(arrangement.Id);

            // Assert
            var notifications = await db.DeliveryNotifications.Where(n => n.Type == "DONATION_COLLECTED").ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
            notifications.Select(n => n.UserId).Should().BeEquivalentTo(new[] { "donor_n2", "org_n2" });
        }
    }

    [Fact]
    public async Task N_U03_ReceiptIsConfirmed_CreatesUnreadReceiptConfirmedNotificationForDonorOnly()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 903,
                DonationId = 1003,
                DonorId = "donor_n3",
                DonorName = "Donor N3",
                OrganizationId = "org_n3",
                OrganizationName = "Org N3",
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "Collected",
                ArrangedAt = DateTime.UtcNow.AddHours(-2),
                CollectedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("org_n3", "ORGANIZATION");

            // Act
            await delivController.ConfirmReceipt(arrangement.Id);

            // Assert
            var notifications = await db.DeliveryNotifications.Where(n => n.Type == "RECEIPT_CONFIRMED").ToListAsync();
            notifications.Should().HaveCount(1);
            notifications.First().UserId.Should().Be("donor_n3");
            notifications.First().IsRead.Should().BeFalse();
        }
    }

    [Fact]
    public async Task N_U04_DonationIsCompleted_CreatesUnreadDonationCompletedNotifications()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 904,
                DonationId = 1004,
                DonorId = "donor_n4",
                DonorName = "Donor N4",
                OrganizationId = "org_n4",
                OrganizationName = "Org N4",
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "Received",
                ArrangedAt = DateTime.UtcNow.AddHours(-3),
                CollectedAt = DateTime.UtcNow.AddHours(-2),
                ReceivedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_n4", "DONOR");

            // Act
            await delivController.CompleteDonation(arrangement.Id);

            // Assert
            var notifications = await db.DeliveryNotifications.Where(n => n.Type == "DONATION_COMPLETED").ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
            notifications.Select(n => n.UserId).Should().BeEquivalentTo(new[] { "donor_n4", "org_n4" });
        }
    }

    [Fact]
    public async Task N_U05_WorkflowOperationIsRejected_NoNotificationIsCreated()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_n1", "DONOR");

            var dto = new ArrangeCollectionDto { RequestId = 99999 }; // Unknown request

            // Act
            await delivController.ArrangeCollection(dto);

            // Assert
            (await db.DeliveryNotifications.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task N_U06_SameNotificationTypeAndRelatedArrangementAttempted_UniqueConstraintPreventsDuplicate()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var notif1 = new DeliveryNotification
            {
                UserId = "user_dupe",
                Title = "Arranged",
                Message = "Test",
                Type = "COLLECTION_ARRANGED",
                RelatedId = 55,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryNotifications.Add(notif1);
            await db.SaveChangesAsync();

            var notifDuplicate = new DeliveryNotification
            {
                UserId = "user_dupe",
                Title = "Arranged Again",
                Message = "Test Duplicate",
                Type = "COLLECTION_ARRANGED",
                RelatedId = 55,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryNotifications.Add(notifDuplicate);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task N_U07_DonorAndOrganizationIdsIdenticalOrBlank_NoDuplicateOrFailure()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 907, donationId: 1007, donorId: "same_user", orgId: "same_user");
            db.ChangeTracker.Clear();
            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("same_user", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 907,
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await delivController.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(201);
            var notifications = await db.DeliveryNotifications.Where(n => n.UserId == "same_user").ToListAsync();
            notifications.Should().HaveCount(1); // Safely deduplicated in AddNotificationIfUnique helper
        }
    }

    [Fact]
    public async Task N_U08_WorkflowTransactionFails_NotificationsRollBack()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 908, donationId: 1008, donorId: "donor_n8", orgId: "org_n8");

            // Seed duplicate notification to cause unique index violation during SaveChanges inside transaction
            db.DeliveryNotifications.Add(new DeliveryNotification
            {
                UserId = "donor_n8",
                Type = "COLLECTION_ARRANGED",
                RelatedId = 1, // Conflicts when arrangement gets ID 1
                Title = "Pre-seeded",
                Message = "Pre-seeded",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            delivController.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_n8", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 908,
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            await delivController.ArrangeCollection(dto);

            // Assert: arrangement insert was rolled back by transaction
            (await db.DeliveryArrangements.CountAsync(d => d.RequestId == 908)).Should().Be(0);
        }
    }

    [Fact]
    public async Task N_U09_UserRequestsNotifications_ReturnsOnlyCallersNotificationsNewestFirst()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var now = DateTime.UtcNow;
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_A", Title = "Older", Message = "M1", Type = "COLLECTION_ARRANGED", RelatedId = 1, CreatedAt = now.AddHours(-2) },
                new DeliveryNotification { UserId = "user_A", Title = "Newer", Message = "M2", Type = "DONATION_COLLECTED", RelatedId = 1, CreatedAt = now.AddHours(-1) },
                new DeliveryNotification { UserId = "user_B", Title = "User B Notif", Message = "M3", Type = "COLLECTION_ARRANGED", RelatedId = 2, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_A");

            // Act
            var result = await notifController.GetMyNotifications() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<List<DeliveryNotificationResponseDto>>;
            response!.Data.Should().HaveCount(2);
            response.Data![0].Title.Should().Be("Newer");
            response.Data[1].Title.Should().Be("Older");
            response.Data.Should().NotContain(n => n.UserId == "user_B");
        }
    }

    [Fact]
    public async Task N_U10_UserHasNoNotifications_Returns200WithEmptyList()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("empty_user");

            // Act
            var result = await notifController.GetMyNotifications() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<List<DeliveryNotificationResponseDto>>;
            response!.Data.Should().NotBeNull();
            response.Data.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task N_U11_UserMarksOwnUnreadNotificationAsRead_Returns200OK()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var notif = new DeliveryNotification { UserId = "user_mark", Title = "T", Message = "M", Type = "T", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_mark");

            // Act
            var result = await notifController.MarkAsRead(notif.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var updated = await db.DeliveryNotifications.FirstAsync(n => n.Id == notif.Id);
            updated.IsRead.Should().BeTrue();
        }
    }

    [Fact]
    public async Task N_U12_UserMarksAlreadyReadNotificationAsRead_IdempotentSuccess()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var notif = new DeliveryNotification { UserId = "user_mark", Title = "T", Message = "M", Type = "T", RelatedId = 1, IsRead = true, CreatedAt = DateTime.UtcNow };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_mark");

            // Act
            var result = await notifController.MarkAsRead(notif.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var updated = await db.DeliveryNotifications.FirstAsync(n => n.Id == notif.Id);
            updated.IsRead.Should().BeTrue();
        }
    }

    [Fact]
    public async Task N_U13_UserTriesToMarkAnotherUsersNotificationAsRead_Returns404NotFound()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            var notif = new DeliveryNotification { UserId = "user_other", Title = "T", Message = "M", Type = "T", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_attacker");

            // Act
            var result = await notifController.MarkAsRead(notif.Id) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
            var unchanged = await db.DeliveryNotifications.FirstAsync(n => n.Id == notif.Id);
            unchanged.IsRead.Should().BeFalse();
        }
    }

    [Fact]
    public async Task N_U14_UserMarksAllNotificationsAsRead_OnlyCallersUnreadChange()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_A", Title = "T1", Message = "M1", Type = "T1", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_A", Title = "T2", Message = "M2", Type = "T2", RelatedId = 2, IsRead = false, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_B", Title = "TB", Message = "MB", Type = "TB", RelatedId = 3, IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_A");

            // Act
            var result = await notifController.MarkAllAsRead() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            var userANotifs = await db.DeliveryNotifications.Where(n => n.UserId == "user_A").ToListAsync();
            userANotifs.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());

            var userBNotif = await db.DeliveryNotifications.FirstAsync(n => n.UserId == "user_B");
            userBNotif.IsRead.Should().BeFalse();
        }
    }

    [Fact]
    public async Task N_U15_UserClearsReadNotifications_OnlyCallersReadNotificationsAreDeleted()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            db.DeliveryNotifications.AddRange(
                new DeliveryNotification { UserId = "user_A", Title = "Read A", Message = "M", Type = "T", RelatedId = 1, IsRead = true, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_A", Title = "Unread A", Message = "M", Type = "T", RelatedId = 2, IsRead = false, CreatedAt = DateTime.UtcNow },
                new DeliveryNotification { UserId = "user_B", Title = "Read B", Message = "M", Type = "T", RelatedId = 3, IsRead = true, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_A");

            // Act
            var result = await notifController.ClearReadNotifications() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            var remainingUserA = await db.DeliveryNotifications.Where(n => n.UserId == "user_A").ToListAsync();
            remainingUserA.Should().HaveCount(1);
            remainingUserA.First().Title.Should().Be("Unread A");

            var remainingUserB = await db.DeliveryNotifications.Where(n => n.UserId == "user_B").ToListAsync();
            remainingUserB.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task N_U16_UserClearsWhenNoReadNotificationsExist_SuccessfulNoOpResult()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            db.DeliveryNotifications.Add(new DeliveryNotification { UserId = "user_A", Title = "Unread", Message = "M", Type = "T", RelatedId = 1, IsRead = false, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_A");

            // Act
            var result = await notifController.ClearReadNotifications() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            (await db.DeliveryNotifications.CountAsync(n => n.UserId == "user_A")).Should().Be(1);
        }
    }

    [Fact]
    public async Task N_U17_NotificationIdDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            notifController.ControllerContext = ControllerTestHelper.CreateControllerContext("user_A");

            // Act
            var result = await notifController.MarkAsRead(9999) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
        }
    }

    [Fact]
    public async Task N_U18_AuthenticatedRequestLacksUserIdClaim_GetReturns401Unauthorized()
    {
        // Arrange
        var (notifController, delivController, db, conn) = SetupTest();
        using (conn)
        {
            // Identity with no claims
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            notifController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await notifController.GetMyNotifications() as UnauthorizedObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(401);
        }
    }
}
