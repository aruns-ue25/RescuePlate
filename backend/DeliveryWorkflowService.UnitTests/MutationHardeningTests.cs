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

/// <summary>
/// Targeted mutation-killing unit test suite for Sprint 4 (DeliveryWorkflowService).
/// Designed with concise, parameterized boundary tests and exact assertion checks
/// to kill relational, conditional, null-coalescing, and state-transition mutants in DeliveriesController.
/// </summary>
public class MutationHardeningTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    // =========================================================================
    // 1. Boundary & Relational Mutator Tests (Time check: <= vs <)
    // =========================================================================

    [Theory]
    [InlineData(0)]   // Exact present boundary (UtcNow) - must be rejected
    [InlineData(-10)]  // 10 seconds in the past - must be rejected
    [InlineData(-3600)] // 1 hour in the past - must be rejected
    public async Task ArrangeCollection_TimeInPastOrExactPresent_RejectsWithBadRequest(int secondsOffset)
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 501, donationId: 601, donorId: "donor_mut", orgId: "org_mut", status: "ACCEPTED");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mut", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 501,
                PickupAddress = "123 Test St",
                DeliveryAddress = "456 Main St",
                ContactName = "John",
                ContactPhone = "+123456789",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddSeconds(secondsOffset)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull("time <= UtcNow must trigger BadRequest");
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("ScheduledCollectionTime must be a valid future timestamp");
        }
    }

    [Fact]
    public async Task ArrangeCollection_DefaultOrNullTime_RejectsWithBadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 502, donationId: 602, donorId: "donor_mut", orgId: "org_mut", status: "ACCEPTED");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mut", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 502,
                PickupAddress = "123 Test St",
                DeliveryAddress = "456 Main St",
                ContactName = "John",
                ContactPhone = "+123456789",
                ScheduledCollectionTime = default(DateTimeOffset)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }
    }

    // =========================================================================
    // 2. Exact Value & Fallback String Mutator Tests (Null-coalescing mutants)
    // =========================================================================

    [Fact]
    public async Task ArrangeCollection_WhitespaceRequestTitles_AppliesExactFallbackDefaults()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(
                db,
                requestId: 503,
                donationId: 603,
                donorId: "donor_mut",
                orgId: "org_mut",
                status: "ACCEPTED",
                title: "   ",
                orgName: "   ");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mut", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 503,
                PickupAddress = "  123 Test St  ",
                DeliveryAddress = "  456 Main St  ",
                ContactName = "  John Doe  ",
                ContactPhone = "  +123456789  ",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddHours(2)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            var response = result!.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Data.Should().NotBeNull();
            
            // Kills mutants in string.IsNullOrWhiteSpace fallbacks
            response.Data!.DonationTitle.Should().Be("Surplus Food Donation");
            response.Data.OrganizationName.Should().Be("Organization User");
            response.Data.DonorName.Should().Be("Donor User");

            // Kills mutants in String.Trim()
            response.Data.PickupAddress.Should().Be("123 Test St");
            response.Data.DeliveryAddress.Should().Be("456 Main St");
            response.Data.ContactName.Should().Be("John Doe");
            response.Data.ContactPhone.Should().Be("+123456789");
        }
    }

    // =========================================================================
    // 3. State-Machine & Status Transition Mutator Tests (Collected -> Received -> Completed)
    // =========================================================================

    [Theory]
    [InlineData("CollectionArranged")]
    [InlineData("Completed")]
    public async Task ConfirmReceipt_InvalidPreconditionStatuses_RejectsWithBadRequest(string invalidStatus)
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 504,
                DonationId = 604,
                DonorId = "donor_mut",
                OrganizationId = "org_mut",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = invalidStatus,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_mut", "ORGANIZATION");

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id) as BadRequestObjectResult;

            // Assert - Kills relational/equality mutants in status checking
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Match(m => m.Contains("must be 'Collected'") || m.Contains("Receipt has already been confirmed"));
        }
    }

    [Theory]
    [InlineData("CollectionArranged")]
    [InlineData("Collected")]
    public async Task CompleteDonation_InvalidPreconditionStatuses_RejectsWithBadRequest(string invalidStatus)
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 505,
                DonationId = 605,
                DonorId = "donor_mut",
                OrganizationId = "org_mut",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = invalidStatus,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mut", "DONOR");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as BadRequestObjectResult;

            // Assert - Kills relational/equality mutants in CompleteDonation status guard
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("must be 'Received'");
        }
    }

    // =========================================================================
    // 4. Role Authorization & Admin Bypass Mutator Tests (GetTrackingById / RequestId)
    // =========================================================================

    [Theory]
    [InlineData("donor_actual", "DONOR", 200)]
    [InlineData("org_actual", "ORGANIZATION", 200)]
    [InlineData("admin_user", "ADMIN", 200)]
    [InlineData("unrelated_stranger", "DONOR", 403)]
    [InlineData("unrelated_org", "ORGANIZATION", 403)]
    public async Task GetTrackingById_RoleAndOwnershipCombinations_EnforcesExactAccess(
        string callerId,
        string callerRole,
        int expectedStatusCode)
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 506,
                DonationId = 606,
                DonorId = "donor_actual",
                OrganizationId = "org_actual",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "CollectionArranged",
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext(callerId, callerRole);

            // Act
            var actionResult = await controller.GetTrackingById(arrangement.Id);

            // Assert - Kills compound boolean mutators (&& vs ||) in authorization guards
            if (expectedStatusCode == 200)
            {
                var okResult = actionResult as OkObjectResult;
                okResult.Should().NotBeNull();
                okResult!.StatusCode.Should().Be(200);
            }
            else
            {
                var statusResult = actionResult as ObjectResult;
                statusResult.Should().NotBeNull();
                statusResult!.StatusCode.Should().Be(403);
            }
        }
    }

    // =========================================================================
    // 5. Notification Creation & Side Effect Verification (Mutation Statement Removal)
    // =========================================================================

    [Fact]
    public async Task CompleteDonation_ValidFlow_CreatesDualNotificationsAndTimestamp()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 507,
                DonationId = 607,
                DonationTitle = "Fresh Bread",
                DonorId = "donor_notif",
                OrganizationId = "org_notif",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "Received",
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_notif", "ORGANIZATION");

            // Act
            var result = await controller.CompleteDonation(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            // Kills statement-removal mutants for notification creation
            var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id).ToListAsync();
            notifications.Should().HaveCount(2, "Must generate exactly one notification for Donor and one for Organization");
            notifications.Should().Contain(n => n.UserId == "donor_notif" && n.Type == "DONATION_COMPLETED");
            notifications.Should().Contain(n => n.UserId == "org_notif" && n.Type == "DONATION_COMPLETED");

            // Kills statement-removal / field-assignment mutants on UpdatedAt & CompletedAt
            var persisted = await db.DeliveryArrangements.FindAsync(arrangement.Id);
            persisted.Should().NotBeNull();
            persisted!.Status.Should().Be("Completed");
            persisted.CompletedAt.Should().NotBeNull();
            persisted.UpdatedAt.Should().NotBeNull();
        }
    }
}
