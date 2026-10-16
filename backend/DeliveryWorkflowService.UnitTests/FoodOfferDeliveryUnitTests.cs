using System;
using System.Data;
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

public class FoodOfferDeliveryUnitTests
{
    private (DeliveriesController controller, Data.DeliveryDbContext db, Microsoft.Data.Sqlite.SqliteConnection conn) SetupTest()
    {
        var db = TestDbContextFactory.CreateDbContext(out var conn);
        var logger = NullLogger<DeliveriesController>.Instance;
        var controller = new DeliveriesController(db, logger);
        return (controller, db, conn);
    }

    [Fact]
    public async Task FOD_U01_DonorArrangesCollection_ForAcceptedFoodOffer_Returns201Created()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedFoodOffer(
                db, 
                offerId: 501, 
                orgFoodNeedRequestId: 301, 
                donorId: "donor_food_1", 
                orgId: "org_food_1", 
                status: "ACCEPTED", 
                needTitle: "50 Warm Lunch Boxes", 
                donorName: "Bakery Chef",
                orgName: "City Shelter");

            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_food_1", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 501, // Corresponds to DonorOffers.Id
                PickupAddress = "100 Baker St, Kitchen",
                DeliveryAddress = "200 Community Center, Dock 2",
                ContactName = "Chef Mario",
                ContactPhone = "+1 555-0101",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddHours(4),
                Notes = "Packed in temperature-controlled carriers"
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
            response.Data!.DonationTitle.Should().Be("50 Warm Lunch Boxes");
            response.Data.DonorName.Should().Be("Bakery Chef");
            response.Data.OrganizationName.Should().Be("City Shelter");

            var arrangement = await db.DeliveryArrangements.Include(d => d.History).FirstOrDefaultAsync(d => d.RequestId == 501);
            arrangement.Should().NotBeNull();
            arrangement!.Status.Should().Be("CollectionArranged");
            arrangement.DonorId.Should().Be("donor_food_1");
            arrangement.DonorName.Should().Be("Bakery Chef");
            arrangement.OrganizationId.Should().Be("org_food_1");
            arrangement.OrganizationName.Should().Be("City Shelter");
            arrangement.DonationTitle.Should().Be("50 Warm Lunch Boxes");
            arrangement.PickupAddress.Should().Be("100 Baker St, Kitchen");
            arrangement.DeliveryAddress.Should().Be("200 Community Center, Dock 2");

            arrangement.History.Should().HaveCount(1);
            var history = arrangement.History.First();
            history.PreviousStatus.Should().Be("Accepted");
            history.NewStatus.Should().Be("CollectionArranged");
            history.ChangedByUserId.Should().Be("donor_food_1");
            history.ChangedByRole.Should().Be("DONOR");

            var notifications = await db.DeliveryNotifications.ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Should().Contain(n => n.UserId == "donor_food_1" && n.Type == "COLLECTION_ARRANGED");
            notifications.Should().Contain(n => n.UserId == "org_food_1" && n.Type == "COLLECTION_ARRANGED");
        }
    }

    [Fact]
    public async Task FOD_U02_OrganizationArrangesCollection_ForAcceptedFoodOffer_Returns201WithOrgAudit()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedFoodOffer(
                db, 
                offerId: 502, 
                orgFoodNeedRequestId: 302, 
                donorId: "donor_food_2", 
                orgId: "org_food_2", 
                status: "ACCEPTED", 
                needTitle: "Fresh Fruit Crates",
                donorName: "Orchard Farm",
                orgName: "Youth Foundation");

            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_food_2", "ORGANIZATION");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 502,
                PickupAddress = "Farm Gate 3, Orchard Rd",
                DeliveryAddress = "Youth Center Main Hall",
                ContactName = "Coordinator Sarah",
                ContactPhone = "+1 555-0202",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(201);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response!.Success.Should().BeTrue();

            var history = await db.DeliveryStatusHistories.FirstOrDefaultAsync(h => h.ChangedByUserId == "org_food_2");
            history.Should().NotBeNull();
            history!.ChangedByRole.Should().Be("ORGANIZATION");

            var arrangement = await db.DeliveryArrangements.FirstOrDefaultAsync(d => d.RequestId == 502);
            arrangement.Should().NotBeNull();
            arrangement!.OrganizationId.Should().Be("org_food_2");
            arrangement.DonorId.Should().Be("donor_food_2");
        }
    }

    [Fact]
    public async Task FOD_U03_ArrangeCollection_FoodOfferNotAccepted_Returns400BadRequest()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedFoodOffer(
                db, 
                offerId: 503, 
                orgFoodNeedRequestId: 303, 
                donorId: "donor_food_3", 
                orgId: "org_food_3", 
                status: "PENDING"); // Pending, not accepted

            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_food_3", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 503,
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
            (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task FOD_U04_ArrangeCollection_UnrelatedUserForFoodOffer_Returns403Forbidden()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedFoodOffer(
                db, 
                offerId: 504, 
                orgFoodNeedRequestId: 304, 
                donorId: "donor_food_4", 
                orgId: "org_food_4", 
                status: "ACCEPTED");

            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("unrelated_party", "DONOR");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 504,
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
    public async Task FOD_U05_CompleteDonation_ForFoodOffer_SynchronizesDonorOfferStatusToCompleted()
    {
        // Arrange
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedFoodOffer(
                db, 
                offerId: 505, 
                orgFoodNeedRequestId: 305, 
                donorId: "donor_food_5", 
                orgId: "org_food_5", 
                status: "ACCEPTED", 
                needTitle: "Winter Relief Soup Cans");

            // Pre-seed delivery arrangement in 'Received' status
            var delivery = new DeliveryArrangement
            {
                RequestId = 505,
                DonationId = 305,
                DonationTitle = "Winter Relief Soup Cans",
                DonorId = "donor_food_5",
                DonorName = "Soup Kitchen Donor",
                OrganizationId = "org_food_5",
                OrganizationName = "City Shelter",
                PickupAddress = "50 Soup St",
                DeliveryAddress = "100 Shelter St",
                ContactName = "Contact Person",
                ContactPhone = "123456",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(-1),
                Status = "Received",
                ArrangedAt = DateTime.UtcNow.AddDays(-1),
                CollectedAt = DateTime.UtcNow.AddHours(-10),
                ReceivedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            db.DeliveryArrangements.Add(delivery);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Set caller as Organization
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_food_5", "ORGANIZATION");

            var dto = new CompleteDonationDto
            {
                Notes = "All soup cans inspected and stored safely in pantry."
            };

            // Act
            var result = await controller.CompleteDonation(delivery.Id, dto) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Status.Should().Be("Completed");

            // Verify DeliveryArrangement in DB
            var updatedDelivery = await db.DeliveryArrangements.FindAsync(delivery.Id);
            updatedDelivery.Should().NotBeNull();
            updatedDelivery!.Status.Should().Be("Completed");
            updatedDelivery.CompletedAt.Should().NotBeNull();

            // Authoritative verification: DonorOffers table in DB updated to 'COMPLETED'
            var rawConn = db.Database.GetDbConnection();
            if (rawConn.State != ConnectionState.Open)
            {
                rawConn.Open();
            }
            using var cmd = rawConn.CreateCommand();
            cmd.CommandText = "SELECT \"Status\" FROM \"DonorOffers\" WHERE \"Id\" = 505";
            var offerStatus = (string?)cmd.ExecuteScalar();
            offerStatus.Should().Be("COMPLETED");
        }
    }

    [Fact]
    public async Task FOD_Collision_Offer16_Accepted_While_Request16_Completed_ArrangesOfferSuccessfully()
    {
        // Arrange: Seed Request 16 as COMPLETED (different user) and Offer 16 as ACCEPTED (Kathisan Foods)
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            TestDbContextFactory.SeedRequest(
                db,
                requestId: 16,
                donationId: 10,
                donorId: "donor_old",
                orgId: "org_old",
                status: "COMPLETED",
                title: "Old Surplus Apples");

            TestDbContextFactory.SeedFoodOffer(
                db,
                offerId: 16,
                orgFoodNeedRequestId: 25,
                donorId: "donor_active",
                orgId: "org_kathisan",
                status: "ACCEPTED",
                needTitle: "Request dry foods for 50 peoples",
                donorName: "Arun Foods",
                orgName: "Kathisan Foods");

            db.ChangeTracker.Clear();
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_kathisan", "ORGANIZATION");

            var dto = new ArrangeCollectionDto
            {
                RequestId = 16,
                RequestType = "OFFER",
                PickupAddress = "Arun Foods Kitchen",
                DeliveryAddress = "Kathisan Shelter Dock",
                ContactName = "Kathisan Manager",
                ContactPhone = "+1 555-0199",
                ScheduledCollectionTime = DateTimeOffset.UtcNow.AddHours(2),
                Notes = "Dry food pickup"
            };

            // Act
            var result = await controller.ArrangeCollection(dto) as CreatedAtActionResult;

            // Assert: Must succeed with 201 Created instead of 400 Bad Request
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(201);
            var response = result.Value as ApiResponse<DeliveryTrackingResponseDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.RequestType.Should().Be("OFFER");
            response.Data.OrganizationName.Should().Be("Kathisan Foods");
            response.Data.DonationTitle.Should().Be("Request dry foods for 50 peoples");

            var arrangement = await db.DeliveryArrangements.FirstOrDefaultAsync(d => d.RequestId == 16 && d.RequestType == "OFFER");
            arrangement.Should().NotBeNull();
            arrangement!.OrganizationId.Should().Be("org_kathisan");
        }
    }

    [Fact]
    public async Task FOD_Collision_Request19_And_Offer19_TrackIndependently_Without403()
    {
        // Arrange: Offer 19 is arranged between Donor A and Org A.
        // Request 19 has NO delivery arranged yet for Donor B and Org B.
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            var offerDelivery = new DeliveryArrangement
            {
                RequestId = 19,
                RequestType = "OFFER",
                DonationId = 88,
                DonationTitle = "Offer 19 Meal",
                DonorId = "donor_A",
                DonorName = "Donor A",
                OrganizationId = "org_A",
                OrganizationName = "Org A",
                PickupAddress = "123 Street",
                DeliveryAddress = "456 Avenue",
                ContactName = "Person A",
                ContactPhone = "111111",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
                Status = "CollectionArranged",
                ArrangedAt = DateTime.UtcNow
            };
            db.DeliveryArrangements.Add(offerDelivery);
            await db.SaveChangesAsync();

            // Act 1: Donor B queries tracking for Request 19 (which has NO delivery)
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("donor_B", "DONOR");
            var resB = await controller.GetTrackingByRequestId(19, type: "REQUEST") as ObjectResult;

            // Assert 1: Must return 404 Not Found so frontend can render Arrange button (NOT 403 Forbidden!)
            resB.Should().NotBeNull();
            resB!.StatusCode.Should().Be(404);

            // Act 2: Org A queries tracking for Offer 19
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_A", "ORGANIZATION");
            var resA = await controller.GetTrackingByRequestId(19, type: "OFFER") as OkObjectResult;

            // Assert 2: Must return 200 OK with Offer 19 tracking
            resA.Should().NotBeNull();
            resA!.StatusCode.Should().Be(200);
            var responseA = resA.Value as ApiResponse<DeliveryTrackingResponseDto>;
            responseA.Should().NotBeNull();
            responseA!.Data!.DonationTitle.Should().Be("Offer 19 Meal");
        }
    }

    [Fact]
    public async Task FOD_Collision_CompleteOffer_DoesNotCorruptRequestWithSameId()
    {
        var (controller, db, conn) = SetupTest();
        using (conn)
        {
            // Seed Request 77 as ACCEPTED
            TestDbContextFactory.SeedRequest(
                db,
                requestId: 77,
                donationId: 10,
                donorId: "donor_req",
                orgId: "org_req",
                status: "ACCEPTED",
                title: "Surplus Listing 77");

            // Seed Offer 77 as ACCEPTED
            TestDbContextFactory.SeedFoodOffer(
                db,
                offerId: 77,
                orgFoodNeedRequestId: 30,
                donorId: "donor_off",
                orgId: "org_off",
                status: "ACCEPTED",
                needTitle: "Food Need Offer 77");

            // Create Delivery for Offer 77 in Received status
            var offerDelivery = new DeliveryArrangement
            {
                RequestId = 77,
                RequestType = "OFFER",
                DonationId = 30,
                DonationTitle = "Food Need Offer 77",
                DonorId = "donor_off",
                DonorName = "Donor Off",
                OrganizationId = "org_off",
                OrganizationName = "Org Off",
                PickupAddress = "Pickup",
                DeliveryAddress = "Delivery",
                ContactName = "Contact",
                ContactPhone = "999",
                ScheduledCollectionTime = DateTime.UtcNow.AddDays(-1),
                Status = "Received",
                ArrangedAt = DateTime.UtcNow.AddDays(-1),
                ReceivedAt = DateTime.UtcNow.AddHours(-1)
            };
            db.DeliveryArrangements.Add(offerDelivery);
            await db.SaveChangesAsync();

            // Act: Complete delivery for Offer 77
            controller.ControllerContext = ControllerTestHelper.CreateControllerContext("org_off", "ORGANIZATION");
            var result = await controller.CompleteDonation(offerDelivery.Id) as OkObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            // Assert: DonorOffers 77 must be COMPLETED, but Requests 77 must remain ACCEPTED!
            var rawConn = db.Database.GetDbConnection();
            if (rawConn.State != ConnectionState.Open) rawConn.Open();

            using var cmd1 = rawConn.CreateCommand();
            cmd1.CommandText = "SELECT \"Status\" FROM \"DonorOffers\" WHERE \"Id\" = 77";
            var offerStatus = (string?)cmd1.ExecuteScalar();
            offerStatus.Should().Be("COMPLETED");

            using var cmd2 = rawConn.CreateCommand();
            cmd2.CommandText = "SELECT \"Status\" FROM \"Requests\" WHERE \"Id\" = 77";
            var reqStatus = (string?)cmd2.ExecuteScalar();
            reqStatus.Should().Be("ACCEPTED"); // Must NOT be corrupted to COMPLETED!
        }
    }
}
