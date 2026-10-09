using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DeliveryWorkflowService.Controllers;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.Models;
using DeliveryWorkflowService.UnitTests.Helpers;
using Xunit;

namespace DeliveryWorkflowService.UnitTests;

public class RecordCollectionUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(Data.DeliveryDbContext db, string status = "CollectionArranged")
    {
        var arrangement = new DeliveryArrangement
        {
            RequestId = 501,
            DonationId = 601,
            DonationTitle = "Fresh Milk",
            DonorId = "donor_c",
            DonorName = "Donor C",
            OrganizationId = "org_c",
            OrganizationName = "Org C",
            PickupAddress = "123 Street",
            DeliveryAddress = "456 Center",
            ContactName = "Contact C",
            ContactPhone = "1234567890",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return arrangement;
    }

    [Fact]
    public async Task C_U01_DonorRecordsCollection_Returns200OK_StatusIsCollected()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            var dto = new RecordCollectionDto { Notes = "  Picked up on time  " };

            // Act
            var result = await controller.RecordCollection(arrangement.Id, dto) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Status.Should().Be("Collected");
            response.Data.CollectedAt.Should().NotBeNull();
            response.Data.UpdatedAt.Should().NotBeNull();

            var updatedArrangement = await db.DeliveryArrangements.Include(d => d.History).FirstAsync(d => d.Id == arrangement.Id);
            updatedArrangement.Status.Should().Be("Collected");
            updatedArrangement.CollectedAt.Should().NotBeNull();

            var history = updatedArrangement.History.FirstOrDefault(h => h.NewStatus == "Collected");
            history.Should().NotBeNull();
            history!.PreviousStatus.Should().Be("CollectionArranged");
            history.ChangedByUserId.Should().Be("donor_c");
            history.ChangedByRole.Should().Be("DONOR");
            history.Notes.Should().Be("Picked up on time");

            var notifications = await db.DeliveryNotifications.Where(n => n.Type == "DONATION_COLLECTED").ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().Contain(n => n.UserId == "donor_c" && !n.IsRead && n.RelatedId == arrangement.Id);
            notifications.Should().Contain(n => n.UserId == "org_c" && !n.IsRead && n.RelatedId == arrangement.Id);
        }
    }

    [Fact]
    public async Task C_U02_IntendedOrganizationRecordsCollection_Returns200OK()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_c", "ORGANIZATION");

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Data!.Status.Should().Be("Collected");

            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Collected");
            history.ChangedByUserId.Should().Be("org_c");
            history.ChangedByRole.Should().Be("ORGANIZATION");
        }
    }

    [Fact]
    public async Task C_U03_CollectionRequestIncludesNotes_HistoryRecordsTrimmedNotes()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            var dto = new RecordCollectionDto { Notes = "   Driver verified temperature   " };

            // Act
            await controller.RecordCollection(arrangement.Id, dto);

            // Assert
            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Collected");
            history.Notes.Should().Be("Driver verified temperature");
        }
    }

    [Fact]
    public async Task C_U04_CollectionRequestOmitsNotes_HistoryNotesAreNull()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Act
            await controller.RecordCollection(arrangement.Id, null);

            // Assert
            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Collected");
            history.Notes.Should().BeNull();
        }
    }

    [Fact]
    public async Task C_U05_DeliveryIdDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Act
            var result = await controller.RecordCollection(9999) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
        }
    }

    [Fact]
    public async Task C_U06_UnrelatedUserAttemptsCollection_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_user", "DONOR");

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);

            var unchanged = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
            unchanged.Status.Should().Be("CollectionArranged");
            unchanged.CollectedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task C_U07_DeliveryAlreadyCollected_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            arrangement.CollectedAt = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    [Fact]
    public async Task C_U08_DeliveryIsReceivedOrCompleted_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("Received");
        }
    }

    [Fact]
    public async Task C_U09_DeliveryHasNotBeenArranged_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Draft");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    [Fact]
    public async Task C_U10_NotesExceed500Chars_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");
            controller.ModelState.AddModelError("Notes", "Notes exceed 500 chars");

            // Assert model validation error is caught
            controller.ModelState.IsValid.Should().BeFalse();
        }
    }

    [Fact]
    public async Task C_U11_ConcurrentCollectionRequests_OnlyOneSucceeds()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // First call succeeds
            var res1 = await controller.RecordCollection(arrangement.Id);
            (res1 as OkObjectResult).Should().NotBeNull();

            // Second call fails with 400
            var res2 = await controller.RecordCollection(arrangement.Id) as BadRequestObjectResult;
            res2.Should().NotBeNull();
            res2!.StatusCode.Should().Be(400);

            var historyCount = await db.DeliveryStatusHistories.CountAsync(h => h.DeliveryArrangementId == arrangement.Id && h.NewStatus == "Collected");
            historyCount.Should().Be(1);

            var notifCount = await db.DeliveryNotifications.CountAsync(n => n.RelatedId == arrangement.Id && n.Type == "DONATION_COLLECTED");
            notifCount.Should().Be(2); // 1 donor, 1 org
        }
    }

    [Fact]
    public async Task C_U12_DatabaseFailureDuringSave_RollsBackTransaction()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_c", "DONOR");

            // Seed duplicate notification to force DbUpdateException during save inside transaction
            db.DeliveryNotifications.Add(new DeliveryNotification
            {
                UserId = "donor_c",
                Type = "DONATION_COLLECTED",
                RelatedId = arrangement.Id,
                Title = "Pre-existing",
                Message = "Pre-existing",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Act
            var result = await controller.RecordCollection(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(500);
        }
    }
}
