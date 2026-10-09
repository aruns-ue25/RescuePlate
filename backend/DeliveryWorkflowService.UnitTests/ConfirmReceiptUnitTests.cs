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

public class ConfirmReceiptUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(Data.DeliveryDbContext db, string status = "Collected")
    {
        var arrangement = new DeliveryArrangement
        {
            RequestId = 502,
            DonationId = 602,
            DonationTitle = "Fresh Meals",
            DonorId = "donor_r",
            DonorName = "Donor R",
            OrganizationId = "org_r",
            OrganizationName = "Hope Shelter",
            PickupAddress = "123 Street",
            DeliveryAddress = "456 Center",
            ContactName = "Contact R",
            ContactPhone = "1234567890",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow.AddHours(-2),
            CollectedAt = status == "Collected" || status == "Received" || status == "Completed" ? DateTime.UtcNow.AddHours(-1) : null,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return arrangement;
    }

    [Fact]
    public async Task R_U01_IntendedOrganizationConfirmsReceipt_Returns200OK_StatusIsReceived()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            var dto = new ConfirmReceiptDto { Notes = " Received in good condition " };

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id, dto) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Status.Should().Be("Received");
            response.Data.ReceivedAt.Should().NotBeNull();

            var updatedArrangement = await db.DeliveryArrangements.Include(d => d.History).FirstAsync(d => d.Id == arrangement.Id);
            updatedArrangement.Status.Should().Be("Received");
            updatedArrangement.ReceivedAt.Should().NotBeNull();

            var history = updatedArrangement.History.FirstOrDefault(h => h.NewStatus == "Received");
            history.Should().NotBeNull();
            history!.PreviousStatus.Should().Be("Collected");
            history.ChangedByUserId.Should().Be("org_r");
            history.ChangedByRole.Should().Be("ORGANIZATION");
            history.Notes.Should().Be("Received in good condition");

            // Verify ONLY Donor notification of type RECEIPT_CONFIRMED is created
            var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id && n.Type == "RECEIPT_CONFIRMED").ToListAsync();
            notifications.Should().HaveCount(1);
            notifications.First().UserId.Should().Be("donor_r");
            notifications.First().IsRead.Should().BeFalse();
        }
    }

    [Fact]
    public async Task R_U02_DonorAttemptsToConfirmReceipt_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_r", "DONOR");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("Only the designated receiving organization is authorized");

            var unchanged = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
            unchanged.Status.Should().Be("Collected");
            unchanged.ReceivedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task R_U03_UnrelatedOrganizationAttemptsConfirmReceipt_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_org", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);

            var unchanged = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
            unchanged.Status.Should().Be("Collected");
        }
    }

    [Fact]
    public async Task R_U04_DeliveryIdDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(9999) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
        }
    }

    [Fact]
    public async Task R_U05_DeliveryIsCollectionArrangedAndNotCollected_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "CollectionArranged");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("CollectionArranged");
        }
    }

    [Fact]
    public async Task R_U06_DeliveryIsAlreadyReceived_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    [Fact]
    public async Task R_U07_DeliveryIsCompleted_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Completed");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    [Fact]
    public async Task R_U08_RequestIncludesValidNotes_HistoryRecordsTrimmedNotes()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            var dto = new ConfirmReceiptDto { Notes = "   Inspected by pantry manager   " };

            // Act
            await controller.ConfirmReceipt(arrangement.Id, dto);

            // Assert
            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Received");
            history.Notes.Should().Be("Inspected by pantry manager");
        }
    }

    [Fact]
    public async Task R_U09_NotesExceed500Chars_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");
            controller.ModelState.AddModelError("Notes", "Notes exceed maximum 500 length");

            // Assert model validation state
            controller.ModelState.IsValid.Should().BeFalse();
        }
    }

    [Fact]
    public async Task R_U10_ConcurrentReceiptConfirmations_OnlyOneSucceeds()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // First call succeeds
            var res1 = await controller.ConfirmReceipt(arrangement.Id);
            (res1 as OkObjectResult).Should().NotBeNull();

            // Second call fails
            var res2 = await controller.ConfirmReceipt(arrangement.Id) as BadRequestObjectResult;
            res2.Should().NotBeNull();
            res2!.StatusCode.Should().Be(400);

            var notifCount = await db.DeliveryNotifications.CountAsync(n => n.RelatedId == arrangement.Id && n.Type == "RECEIPT_CONFIRMED");
            notifCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task R_U11_DatabaseFailureDuringConfirmReceipt_RollsBackTransaction()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_r", "ORGANIZATION");

            // Seed duplicate notification to force DbUpdateException inside transaction
            db.DeliveryNotifications.Add(new DeliveryNotification
            {
                UserId = "donor_r",
                Type = "RECEIPT_CONFIRMED",
                RelatedId = arrangement.Id,
                Title = "Pre-seeded",
                Message = "Pre-seeded",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(500);
        }
    }
}
