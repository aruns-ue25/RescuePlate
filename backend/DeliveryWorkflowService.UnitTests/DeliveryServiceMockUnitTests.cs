using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using DeliveryWorkflowService.Controllers;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.Models;
using DeliveryWorkflowService.UnitTests.Helpers;
using Xunit;

namespace DeliveryWorkflowService.UnitTests;

/// <summary>
/// Unit test suite for DeliveryWorkflowService using Moq mocks and stubs,
/// satisfying ASE project requirements for isolated testing with mocked dependencies.
/// </summary>
public class DeliveryServiceMockUnitTests
{
    private static void VerifyLoggerCall<T>(Mock<ILogger<T>> loggerMock, LogLevel level)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task ArrangeCollection_WithMockLogger_LogsInformation_AndCreatesDeliveryArrangement()
    {
        // Arrange (Stub DB + Mock Logger)
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 1001, donationId: 2001, donorId: "donor_mock", orgId: "org_mock", status: "ACCEPTED");
            db.ChangeTracker.Clear();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mock", "DONOR")
            };

            var dto = new ArrangeCollectionDto
            {
                RequestId = 1001,
                PickupAddress = "100 Baker St",
                DeliveryAddress = "200 Community Center",
                ContactName = "Bob Smith",
                ContactPhone = "+1555123456",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(2),
                Notes = "Fragile baked goods"
            };

            // Act
            var result = await controller.ArrangeCollection(dto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var created = (result as CreatedAtActionResult)!.Value as ApiResponse<DeliveryTrackingResponseDto>;
            created.Should().NotBeNull();
            created!.Success.Should().BeTrue();
            created.Data!.Status.Should().Be("CollectionArranged");

            // Verify in DB stub
            var inDb = await db.DeliveryArrangements.FirstOrDefaultAsync(d => d.RequestId == 1001);
            inDb.Should().NotBeNull();
            inDb!.Status.Should().Be("CollectionArranged");

            // Verify Mock Logger was called with Information level
            VerifyLoggerCall(loggerMock, LogLevel.Information);
        }
    }

    [Fact]
    public async Task RecordCollection_WithMockLogger_LogsInformation_AndTransitionsToCollected()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 1002,
                DonationId = 2002,
                DonorId = "donor_mock",
                OrganizationId = "org_mock",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddHours(2),
                Status = "CollectionArranged",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mock", "DONOR")
            };

            var dto = new RecordCollectionDto
            {
                Notes = "Driver arrived on time"
            };

            // Act
            var result = await controller.RecordCollection(arrangement.Id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (result as OkObjectResult)!.Value as ApiResponse<DeliveryTrackingResponseDto>;
            ok!.Success.Should().BeTrue();
            ok.Data!.Status.Should().Be("Collected");

            // Verify Mock Logger
            VerifyLoggerCall(loggerMock, LogLevel.Information);
        }
    }

    [Fact]
    public async Task ConfirmReceipt_WithMockLogger_LogsInformation_AndTransitionsToReceived()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 1003,
                DonationId = 2003,
                DonorId = "donor_mock",
                OrganizationId = "org_mock",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddHours(-1),
                Status = "Collected",
                CollectedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("org_mock", "ORGANIZATION")
            };

            var dto = new ConfirmReceiptDto
            {
                Notes = "Received in good condition"
            };

            // Act
            var result = await controller.ConfirmReceipt(arrangement.Id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (result as OkObjectResult)!.Value as ApiResponse<DeliveryTrackingResponseDto>;
            ok!.Success.Should().BeTrue();
            ok.Data!.Status.Should().Be("Received");

            // Verify Mock Logger
            VerifyLoggerCall(loggerMock, LogLevel.Information);
        }
    }

    [Fact]
    public async Task CompleteDonation_WithMockLogger_LogsInformation_AndTransitionsToCompleted()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 1004,
                DonationId = 2004,
                DonorId = "donor_mock",
                OrganizationId = "org_mock",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddHours(-2),
                Status = "Received",
                CollectedAt = DateTime.UtcNow.AddHours(-2),
                ReceivedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mock", "DONOR")
            };

            var dto = new CompleteDonationDto
            {
                Notes = "Donation cycle concluded successfully"
            };

            // Act
            var result = await controller.CompleteDonation(arrangement.Id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (result as OkObjectResult)!.Value as ApiResponse<DeliveryTrackingResponseDto>;
            ok!.Success.Should().BeTrue();
            ok.Data!.Status.Should().Be("Completed");

            // Verify Mock Logger
            VerifyLoggerCall(loggerMock, LogLevel.Information);
        }
    }

    [Fact]
    public async Task GetTrackingById_ExistingAndNotFound_WithMockLogger_ReturnsExpectedResult()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 1005,
                DonationId = 2005,
                DonorId = "donor_mock",
                OrganizationId = "org_mock",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddHours(1),
                Status = "CollectionArranged"
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mock", "DONOR")
            };

            // Act 1: Existing
            var existingResult = await controller.GetTrackingById(arrangement.Id);
            existingResult.Should().BeOfType<OkObjectResult>();

            // Act 2: Not found
            var notFoundResult = await controller.GetTrackingById(99999);
            notFoundResult.Should().BeOfType<NotFoundObjectResult>();
        }
    }

    [Fact]
    public async Task GetTrackingByRequestId_ExistingAndNotFound_WithMockLogger_ReturnsExpectedResult()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var arrangement = new DeliveryArrangement
            {
                RequestId = 1006,
                DonationId = 2006,
                DonorId = "donor_mock",
                OrganizationId = "org_mock",
                PickupAddress = "A",
                DeliveryAddress = "B",
                ContactName = "C",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddHours(1),
                Status = "CollectionArranged"
            };
            db.DeliveryArrangements.Add(arrangement);
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("donor_mock", "DONOR")
            };

            // Act 1: Existing
            var existingResult = await controller.GetTrackingByRequestId(1006);
            existingResult.Should().BeOfType<OkObjectResult>();

            // Act 2: Not found
            var notFoundResult = await controller.GetTrackingByRequestId(99999);
            notFoundResult.Should().BeOfType<NotFoundObjectResult>();
        }
    }

    [Fact]
    public async Task GetAdminDeliverySummary_WithMockLogger_ReturnsSummaryMetrics()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            db.DeliveryArrangements.AddRange(
                new DeliveryArrangement { RequestId = 1, DonationId = 1, DonorId = "d1", OrganizationId = "o1", PickupAddress = "A", DeliveryAddress = "B", ContactName = "C", ContactPhone = "1", Status = "CollectionArranged" },
                new DeliveryArrangement { RequestId = 2, DonationId = 2, DonorId = "d2", OrganizationId = "o2", PickupAddress = "A", DeliveryAddress = "B", ContactName = "C", ContactPhone = "1", Status = "Collected" },
                new DeliveryArrangement { RequestId = 3, DonationId = 3, DonorId = "d3", OrganizationId = "o3", PickupAddress = "A", DeliveryAddress = "B", ContactName = "C", ContactPhone = "1", Status = "Completed" }
            );
            await db.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<DeliveriesController>>();
            var controller = new DeliveriesController(db, loggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("admin_1", "ADMIN")
            };

            // Act
            var result = await controller.GetAdminDeliverySummary();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (result as OkObjectResult)!.Value as ApiResponse<object>;
            ok!.Success.Should().BeTrue();
            ok.Data.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task NotificationsController_WithMockLogger_RetrievesAndMarksRead()
    {
        // Arrange
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        using (conn)
        {
            var notif = new DeliveryNotification
            {
                UserId = "user_mock",
                Title = "Driver Assigned",
                Message = "Your food is on the way",
                Type = "DRIVER_ASSIGNED",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryNotifications.Add(notif);
            await db.SaveChangesAsync();

            var notifLoggerMock = new Mock<ILogger<NotificationsController>>();
            var controller = new NotificationsController(db, notifLoggerMock.Object)
            {
                ControllerContext = ControllerTestHelper.CreateControllerContext("user_mock", "DONOR")
            };

            // Act 1: Get notifications
            var getResult = await controller.GetMyNotifications();
            getResult.Should().BeOfType<OkObjectResult>();

            // Act 2: Mark as read
            var markResult = await controller.MarkAsRead(notif.Id);
            markResult.Should().BeOfType<OkObjectResult>();

            var inDb = await db.DeliveryNotifications.FirstAsync(n => n.Id == notif.Id);
            inDb.IsRead.Should().BeTrue();
        }
    }
}
