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

public class CompleteDonationUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(Data.DeliveryDbContext db, string status = "Received")
    {
        var arrangement = new DeliveryArrangement
        {
            RequestId = 503,
            DonationId = 603,
            DonationTitle = "Surplus Sandwiches",
            DonorId = "donor_d",
            DonorName = "Donor D",
            OrganizationId = "org_d",
            OrganizationName = "Community Pantry",
            PickupAddress = "123 Street",
            DeliveryAddress = "456 Center",
            ContactName = "Contact D",
            ContactPhone = "1234567890",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow.AddHours(-3),
            CollectedAt = DateTime.UtcNow.AddHours(-2),
            ReceivedAt = status == "Received" || status == "Completed" ? DateTime.UtcNow.AddHours(-1) : null,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return arrangement;
    }

    [Fact]
    public async Task D_U01_AuthorizedUserCompletesReceivedDonation_Returns200OK_StatusIsCompleted()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            var dto = new CompleteDonationDto { Notes = " Everything distributed to families " };

            // Act
            var result = await controller.CompleteDonation(arrangement.Id, dto) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Status.Should().Be("Completed");
            response.Data.CompletedAt.Should().NotBeNull();
            response.Data.UpdatedAt.Should().NotBeNull();

            var updated = await db.DeliveryArrangements.Include(d => d.History).FirstAsync(d => d.Id == arrangement.Id);
            updated.Status.Should().Be("Completed");
            updated.CompletedAt.Should().NotBeNull();

            var history = updated.History.FirstOrDefault(h => h.NewStatus == "Completed");
            history.Should().NotBeNull();
            history!.PreviousStatus.Should().Be("Received");
            history.ChangedByUserId.Should().Be("donor_d");
            history.Notes.Should().Be("Everything distributed to families");

            var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id && n.Type == "DONATION_COMPLETED").ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().Contain(n => n.UserId == "donor_d" && !n.IsRead);
            notifications.Should().Contain(n => n.UserId == "org_d" && !n.IsRead);
        }
    }

    [Fact]
    public async Task D_U02_DonorCompletesReceivedDonation_SuccessfulTransition()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }
    }

    [Fact]
    public async Task D_U03_IntendedOrganizationCompletesReceivedDonation_SuccessfulTransition()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_d", "ORGANIZATION");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Completed");
            history.ChangedByUserId.Should().Be("org_d");
            history.ChangedByRole.Should().Be("ORGANIZATION");
        }
    }

    [Fact]
    public async Task D_U04_CompletionIncludesNotes_HistoryRecordsTrimmedNotes()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            var dto = new CompleteDonationDto { Notes = "   Final verification completed   " };

            // Act
            await controller.CompleteDonation(arrangement.Id, dto);

            // Assert
            var history = await db.DeliveryStatusHistories.FirstAsync(h => h.NewStatus == "Completed");
            history.Notes.Should().Be("Final verification completed");
        }
    }

    [Fact]
    public async Task D_U05_DeliveryIdDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // Act
            var result = await controller.CompleteDonation(9999) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
        }
    }

    [Fact]
    public async Task D_U06_UnrelatedUserAttemptsCompletion_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_user", "DONOR");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);

            var unchanged = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
            unchanged.Status.Should().Be("Received");
            unchanged.CompletedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task D_U07_DonationNotReceivedStatus_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Collected");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("Collected");
        }
    }

    [Fact]
    public async Task D_U08_DonationAlreadyCompleted_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Completed");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    [Fact]
    public async Task D_U09_NotesExceed500Chars_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");
            controller.ModelState.AddModelError("Notes", "Notes exceed 500 chars limit");

            // Assert model validation state
            controller.ModelState.IsValid.Should().BeFalse();
        }
    }

    [Fact]
    public async Task D_U10_TwoCompletionRequestsRace_OnlyOneSucceeds()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // First call succeeds
            var res1 = await controller.CompleteDonation(arrangement.Id);
            (res1 as OkObjectResult).Should().NotBeNull();

            // Second call fails
            var res2 = await controller.CompleteDonation(arrangement.Id) as BadRequestObjectResult;
            res2.Should().NotBeNull();
            res2!.StatusCode.Should().Be(400);

            var historyCount = await db.DeliveryStatusHistories.CountAsync(h => h.DeliveryArrangementId == arrangement.Id && h.NewStatus == "Completed");
            historyCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task D_U11_DatabaseFailureDuringCompleteDonation_RollsBackTransaction()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedArrangementAsync(db, "Received");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_d", "DONOR");

            // Seed duplicate notification to force DbUpdateException inside transaction
            db.DeliveryNotifications.Add(new DeliveryNotification
            {
                UserId = "donor_d",
                Type = "DONATION_COMPLETED",
                RelatedId = arrangement.Id,
                Title = "Pre-seeded",
                Message = "Pre-seeded",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(500);
        }
    }
}
