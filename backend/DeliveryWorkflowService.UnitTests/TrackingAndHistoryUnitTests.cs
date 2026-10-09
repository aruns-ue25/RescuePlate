using System;
using System.Collections.Generic;
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

public class TrackingAndHistoryUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    private async Task<DeliveryArrangement> SeedFullLifecycleArrangementAsync(Data.DeliveryDbContext db)
    {
        var now = DateTime.UtcNow;
        var arrangement = new DeliveryArrangement
        {
            RequestId = 505,
            DonationId = 605,
            DonationTitle = "Organic Vegetables",
            DonorId = "donor_t",
            DonorName = "Farm Fresh",
            OrganizationId = "org_t",
            OrganizationName = "City Pantry",
            PickupAddress = "123 Farm Rd",
            DeliveryAddress = "456 City Center",
            ContactName = "Farmer Joe",
            ContactPhone = "555-1234",
            ScheduledCollectionTime = now.AddHours(2),
            Status = "Completed",
            ArrangedAt = now.AddHours(-4),
            CollectedAt = now.AddHours(-3),
            ReceivedAt = now.AddHours(-2),
            CompletedAt = now.AddHours(-1),
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-1)
        };

        arrangement.History.Add(new DeliveryStatusHistory
        {
            PreviousStatus = "Accepted",
            NewStatus = "CollectionArranged",
            ChangedByUserId = "donor_t",
            ChangedByRole = "DONOR",
            Notes = "Arranged pickup",
            Timestamp = now.AddHours(-4)
        });
        arrangement.History.Add(new DeliveryStatusHistory
        {
            PreviousStatus = "CollectionArranged",
            NewStatus = "Collected",
            ChangedByUserId = "donor_t",
            ChangedByRole = "DONOR",
            Notes = "Picked up",
            Timestamp = now.AddHours(-3)
        });
        arrangement.History.Add(new DeliveryStatusHistory
        {
            PreviousStatus = "Collected",
            NewStatus = "Received",
            ChangedByUserId = "org_t",
            ChangedByRole = "ORGANIZATION",
            Notes = "Received at facility",
            Timestamp = now.AddHours(-2)
        });
        arrangement.History.Add(new DeliveryStatusHistory
        {
            PreviousStatus = "Received",
            NewStatus = "Completed",
            ChangedByUserId = "org_t",
            ChangedByRole = "ORGANIZATION",
            Notes = "Distributed",
            Timestamp = now.AddHours(-1)
        });

        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        return arrangement;
    }

    [Fact]
    public async Task T_U01_DonorViewsTrackingByDeliveryId_Returns200OK()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(arrangement.Id);
            response.Data.Status.Should().Be("Completed");
        }
    }

    [Fact]
    public async Task T_U02_IntendedOrganizationViewsTrackingByDeliveryId_Returns200OK()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_t", "ORGANIZATION");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Data!.OrganizationId.Should().Be("org_t");
        }
    }

    [Fact]
    public async Task T_U03_AdminViewsTracking_Returns200OK()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("admin_user", "ADMIN");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Data.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task T_U04_AuthorizedUserViewsTrackingByRequestId_Returns200OK()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingByRequestId(arrangement.RequestId) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Data!.RequestId.Should().Be(505);
        }
    }

    [Fact]
    public async Task T_U05_DeliveryOrRequestIdDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var res1 = await controller.GetTrackingById(9999) as NotFoundObjectResult;
            var res2 = await controller.GetTrackingByRequestId(8888) as NotFoundObjectResult;

            // Assert
            res1.Should().NotBeNull();
            res1!.StatusCode.Should().Be(404);
            res2.Should().NotBeNull();
            res2!.StatusCode.Should().Be(404);
        }
    }

    [Fact]
    public async Task T_U06_UnrelatedAuthenticatedUser_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_user", "DONOR");

            // Act
            var res1 = await controller.GetTrackingById(arrangement.Id) as ObjectResult;
            var res2 = await controller.GetTrackingByRequestId(arrangement.RequestId) as ObjectResult;

            // Assert
            res1.Should().NotBeNull();
            res1!.StatusCode.Should().Be(403);
            res2.Should().NotBeNull();
            res2!.StatusCode.Should().Be(403);
        }
    }

    [Fact]
    public async Task T_U07_ArrangementHasMultipleStatusChanges_ReturnsChronologicalHistory()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            var history = response!.Data!.StatusHistory;
            history.Should().HaveCount(4);
            history[0].NewStatus.Should().Be("CollectionArranged");
            history[1].NewStatus.Should().Be("Collected");
            history[2].NewStatus.Should().Be("Received");
            history[3].NewStatus.Should().Be("Completed");
            history.Should().BeInAscendingOrder(h => h.Timestamp);
        }
    }

    [Fact]
    public async Task T_U08_ArrangementHasNoHistoryRows_ReturnsEmptyHistoryList()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 707,
                DonationId = 807,
                DonorId = "donor_t",
                DonorName = "Donor T",
                OrganizationId = "org_t",
                OrganizationName = "Org T",
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Contact",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "CollectionArranged",
                ArrangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Data!.StatusHistory.Should().NotBeNull();
            response.Data.StatusHistory.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task T_U09_HistoryIncludesActorRoleNotesAndTimestamp()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            var item = response!.Data!.StatusHistory.First();
            item.ChangedByUserId.Should().Be("donor_t");
            item.ChangedByRole.Should().Be("DONOR");
            item.Notes.Should().Be("Arranged pickup");
            item.Timestamp.Should().NotBe(default);
        }
    }

    [Fact]
    public async Task T_U10_TrackingResponseForEachStatus_TimestampsAgree()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var arrangement = await SeedFullLifecycleArrangementAsync(db);
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_t", "DONOR");

            // Act
            var result = await controller.GetTrackingById(arrangement.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            var data = response!.Data!;
            data.Status.Should().Be("Completed");
            data.ArrangedAt.Should().NotBe(default);
            data.CollectedAt.Should().NotBeNull();
            data.ReceivedAt.Should().NotBeNull();
            data.CompletedAt.Should().NotBeNull();
        }
    }
}
