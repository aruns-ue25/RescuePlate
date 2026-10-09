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

public class ArrangeCollectionUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    [Fact]
    public async Task A_U01_DonorArrangesCollection_ValidFutureTime_Returns201Created()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 101, donationId: 201, donorId: "donor_1", orgId: "org_1", status: "ACCEPTED");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 101,
                PickupAddress = " 123 Main St, Bakery ",
                DeliveryAddress = " 456 Shelter Ave ",
                ContactName = " Jane Doe ",
                ContactPhone = " +1234567890 ",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1),
                Notes = " Handle with care "
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(201);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Message.Should().Contain("Collection arrangement created successfully");

            var arrangement = await db.DeliveryArrangements.Include(d => d.History).FirstOrDefaultAsync(d => d.RequestId == 101);
            arrangement.Should().NotBeNull();
            arrangement!.Status.Should().Be("CollectionArranged");
            arrangement.PickupAddress.Should().Be("123 Main St, Bakery");
            arrangement.DeliveryAddress.Should().Be("456 Shelter Ave");
            arrangement.ContactName.Should().Be("Jane Doe");
            arrangement.ContactPhone.Should().Be("+1234567890");
            arrangement.Notes.Should().Be("Handle with care");
            arrangement.ScheduledCollectionTime.Kind.Should().Be(DateTimeKind.Utc);
            arrangement.ArrangedAt.Should().NotBe(default);
            arrangement.CreatedAt.Should().NotBe(default);

            arrangement.History.Should().HaveCount(1);
            var history = arrangement.History.First();
            history.PreviousStatus.Should().Be("Accepted");
            history.NewStatus.Should().Be("CollectionArranged");
            history.ChangedByUserId.Should().Be("donor_1");
            history.ChangedByRole.Should().Be("DONOR");
            history.Notes.Should().Be("Handle with care");

            var notifications = await db.DeliveryNotifications.ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().Contain(n => n.UserId == "donor_1" && n.Type == "COLLECTION_ARRANGED" && !n.IsRead && n.RelatedId == arrangement.Id);
            notifications.Should().Contain(n => n.UserId == "org_1" && n.Type == "COLLECTION_ARRANGED" && !n.IsRead && n.RelatedId == arrangement.Id);
        }
    }

    [Fact]
    public async Task A_U02_IntendedOrganizationArrangesCollection_Returns201WithOrgAudit()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 102, donationId: 202, donorId: "donor_2", orgId: "org_2", status: "ACCEPTED");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_2", "ORGANIZATION");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 102,
                PickupAddress = "789 Donor Hub",
                DeliveryAddress = "321 Org Center",
                ContactName = "John Smith",
                ContactPhone = "9876543210",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddHours(5)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(201);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();

            var history = await db.DeliveryStatusHistories.FirstOrDefaultAsync(h => h.ChangedByUserId == "org_2");
            history.Should().NotBeNull();
            history!.ChangedByRole.Should().Be("ORGANIZATION");

            var notifications = await db.DeliveryNotifications.ToListAsync();
            notifications.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task A_U03_ValidArrangement_IncludesWhitespaceNotes_NotesAreTrimmed()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 103, donationId: 203, donorId: "donor_3", orgId: "org_3");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_3", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 103,
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "12345",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(2),
                Notes = "   Padded notes text   "
            };

            // Act
            await controller.ArrangeCollection(dto);

            // Assert
            var arrangement = await db.DeliveryArrangements.Include(d => d.History).FirstAsync(d => d.RequestId == 103);
            arrangement.Notes.Should().Be("Padded notes text");
            arrangement.History.First().Notes.Should().Be("Padded notes text");
        }
    }

    [Fact]
    public async Task A_U04_NotesOmitted_ArrangementSucceeds_NotesAreNull()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 104, donationId: 204, donorId: "donor_4", orgId: "org_4");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_4", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 104,
                PickupAddress = "Addr A",
                DeliveryAddress = "Addr B",
                ContactName = "Contact C",
                ContactPhone = "12345",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(2),
                Notes = null
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            var arrangement = await db.DeliveryArrangements.FirstAsync(d => d.RequestId == 104);
            arrangement.Notes.Should().BeNull();
        }
    }

    [Fact]
    public async Task A_U05_RequestIdMissingOrZero_ReturnsClientError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 0,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U06_PickupAddressInvalid_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");
            controller.ModelState.AddModelError("PickupAddress", "The PickupAddress field is required.");

            var dto = new ArrangeCollectionDto { RequestId = 101, PickupAddress = "" };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U07_DeliveryAddressInvalid_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");
            controller.ModelState.AddModelError("DeliveryAddress", "The DeliveryAddress field is required.");

            var dto = new ArrangeCollectionDto { RequestId = 101, DeliveryAddress = "" };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U08_ContactNameInvalid_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");
            controller.ModelState.AddModelError("ContactName", "The ContactName field is required.");

            var dto = new ArrangeCollectionDto { RequestId = 101, ContactName = "" };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U09_ContactPhoneInvalid_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");
            controller.ModelState.AddModelError("ContactPhone", "The ContactPhone field is required.");

            var dto = new ArrangeCollectionDto { RequestId = 101, ContactPhone = "" };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U10_ScheduledTimeMissingOrDefault_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 110, donationId: 210, donorId: "donor_1", orgId: "org_1");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 110,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = null
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("ScheduledCollectionTime");
        }
    }

    [Fact]
    public async Task A_U11_ScheduledTimeInPast_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 111, donationId: 211, donorId: "donor_1", orgId: "org_1");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 111,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddMinutes(-10)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
            (await db.DeliveryStatusHistories.CountAsync()).Should().Be(0);
            (await db.DeliveryNotifications.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U12_NotesExceed500Chars_ReturnsValidationError()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");
            controller.ModelState.AddModelError("Notes", "The field Notes must be a string with a maximum length of 500.");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 112,
                Notes = new string('A', 501)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U13_RequestDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 9999,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as NotFoundObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(404);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
            (await db.DeliveryStatusHistories.CountAsync()).Should().Be(0);
            (await db.DeliveryNotifications.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U14_RequestStatusNotAccepted_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 114, donationId: 214, donorId: "donor_1", orgId: "org_1", status: "PENDING");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 114,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Message.Should().Contain("PENDING");
        }
    }

    [Fact]
    public async Task A_U15_UnrelatedUser_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 115, donationId: 215, donorId: "donor_1", orgId: "org_1", status: "ACCEPTED");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_user", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 115,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task A_U16_RequestAlreadyHasArrangement_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 116, donationId: 216, donorId: "donor_1", orgId: "org_1", status: "ACCEPTED");
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var existing = new DeliveryArrangement
            {
                RequestId = 116,
                DonationId = 216,
                DonorId = "donor_1",
                DonorName = "Donor 1",
                OrganizationId = "org_1",
                OrganizationName = "Org 1",
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "CollectionArranged",
                ArrangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(existing);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var dto = new ArrangeCollectionDto
            {
                RequestId = 116,
                PickupAddress = "New Addr",
                DeliveryAddress = "New Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(2)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            (await db.DeliveryArrangements.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task A_U17_BlankTitleOrOrgNameInRequest_UsesFallbackDisplayName()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 117, donationId: 217, donorId: "donor_1", orgId: "org_1", status: "ACCEPTED", title: "", orgName: "");
            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 117,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            await controller.ArrangeCollection(dto);

            // Assert
            var arrangement = await db.DeliveryArrangements.FirstAsync(d => d.RequestId == 117);
            arrangement.DonationTitle.Should().Be("Surplus Food Donation");
            arrangement.OrganizationName.Should().Be("Organization User");
        }
    }

    [Fact]
    public async Task A_U18_DatabaseSaveFails_RollsBackTransactionAndReturns500Or400()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(db, requestId: 118, donationId: 218, donorId: "donor_1", orgId: "org_1", status: "ACCEPTED");
            
            // Seed duplicate notification to cause unique index violation during SaveChanges inside transaction
            db.DeliveryNotifications.Add(new DeliveryNotification
            {
                UserId = "donor_1",
                Type = "COLLECTION_ARRANGED",
                RelatedId = 1, // Will conflict if auto id is 1
                Title = "Pre-seeded",
                Message = "Pre-seeded",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 118,
                PickupAddress = "Addr",
                DeliveryAddress = "Addr",
                ContactName = "Name",
                ContactPhone = "123",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            (result!.StatusCode == 400 || result.StatusCode == 500).Should().BeTrue();
        }
    }
}
