using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequestWorkflowService.Controllers;
using RequestWorkflowService.Data;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Events;
using RequestWorkflowService.Kafka;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class BulkRejectUnitTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    private IConfiguration GetTestConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Kafka:RequestTopic", "test-request-events" },
            { "Kafka:NeedRequestTopic", "test-need-events" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
    }

    private static ControllerContext CreateControllerContext(string userId, string role, string name = "Test User")
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("BusinessName", name));
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var identity = new ClaimsIdentity(claims, string.IsNullOrWhiteSpace(userId) ? null : "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }

    [Fact]
    public async Task BR_UT_01_RejectAllPendingRequests_AsDonor_RejectsAllPendingAndPublishesEvents()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        mockProducer
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockHttp = new Mock<IHttpClientFactory>();
        var config = GetTestConfiguration();
        var logger = NullLogger<RequestsController>.Instance;
        var controller = new RequestsController(db, mockHttp.Object, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("donor_100", "DONOR", "Fresh Bakery")
        };

        // Seed 2 PENDING requests for donation 1, 1 ACCEPTED for donation 1, and 1 PENDING for donation 2
        var req1 = new FoodRequest
        {
            DonationId = 1,
            DonationTitle = "Bread Loaves",
            OrganizationId = "org_1",
            OrganizationName = "Shelter A",
            DonorId = "donor_100",
            RequestedQuantity = 5,
            Status = "PENDING"
        };
        var req2 = new FoodRequest
        {
            DonationId = 1,
            DonationTitle = "Bread Loaves",
            OrganizationId = "org_2",
            OrganizationName = "Shelter B",
            DonorId = "donor_100",
            RequestedQuantity = 3,
            Status = "PENDING"
        };
        var reqAccepted = new FoodRequest
        {
            DonationId = 1,
            DonationTitle = "Bread Loaves",
            OrganizationId = "org_3",
            OrganizationName = "Shelter C",
            DonorId = "donor_100",
            RequestedQuantity = 10,
            Status = "ACCEPTED"
        };
        var reqOtherDonation = new FoodRequest
        {
            DonationId = 2,
            DonationTitle = "Croissants",
            OrganizationId = "org_1",
            OrganizationName = "Shelter A",
            DonorId = "donor_100",
            RequestedQuantity = 4,
            Status = "PENDING"
        };

        db.Requests.AddRange(req1, req2, reqAccepted, reqOtherDonation);
        await db.SaveChangesAsync();

        var dto = new RejectRequestDto { Reason = "All surplus distributed to first claimant" };

        // Act
        var result = await controller.RejectAllPendingRequestsForDonation(1, dto) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var apiResp = result.Value as ApiResponse<List<FoodRequestResponseDto>>;
        apiResp.Should().NotBeNull();
        apiResp!.Success.Should().BeTrue();
        apiResp.Data.Should().HaveCount(2);

        // Verify status updates in database
        var dbReq1 = await db.Requests.FindAsync(req1.Id);
        var dbReq2 = await db.Requests.FindAsync(req2.Id);
        var dbReqAccepted = await db.Requests.FindAsync(reqAccepted.Id);
        var dbReqOther = await db.Requests.FindAsync(reqOtherDonation.Id);

        dbReq1!.Status.Should().Be("REJECTED");
        dbReq1.RejectionReason.Should().Be("All surplus distributed to first claimant");
        dbReq2!.Status.Should().Be("REJECTED");
        dbReq2.RejectionReason.Should().Be("All surplus distributed to first claimant");

        dbReqAccepted!.Status.Should().Be("ACCEPTED");
        dbReqOther!.Status.Should().Be("PENDING");

        // Verify notifications generated for recipient organizations
        var notifs = await db.Notifications.ToListAsync();
        notifs.Should().HaveCount(2);
        notifs.Should().Contain(n => n.UserId == "org_1" && n.Type == "REQUEST_REJECTED" && n.RelatedId == req1.Id);
        notifs.Should().Contain(n => n.UserId == "org_2" && n.Type == "REQUEST_REJECTED" && n.RelatedId == req2.Id);

        // Verify Kafka event published for both rejected requests
        mockProducer.Verify(p => p.PublishEventAsync(
            "test-request-events",
            req1.Id.ToString(),
            It.Is<DonationRequestRejectedEvent>(e => e.RequestId == req1.Id && e.Reason == "All surplus distributed to first claimant"),
            It.IsAny<CancellationToken>()), Times.Once);

        mockProducer.Verify(p => p.PublishEventAsync(
            "test-request-events",
            req2.Id.ToString(),
            It.Is<DonationRequestRejectedEvent>(e => e.RequestId == req2.Id && e.Reason == "All surplus distributed to first claimant"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BR_UT_02_RejectAllPendingRequests_WhenNonePending_Returns200WithEmptyList()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        var mockHttp = new Mock<IHttpClientFactory>();
        var config = GetTestConfiguration();
        var logger = NullLogger<RequestsController>.Instance;
        var controller = new RequestsController(db, mockHttp.Object, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("donor_100", "DONOR")
        };

        // Act
        var result = await controller.RejectAllPendingRequestsForDonation(99) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var apiResp = result.Value as ApiResponse<List<FoodRequestResponseDto>>;
        apiResp.Should().NotBeNull();
        apiResp!.Success.Should().BeTrue();
        apiResp.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task BR_UT_03_RejectAllPendingRequests_UnauthorizedCaller_Returns401Unauthorized()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        var mockHttp = new Mock<IHttpClientFactory>();
        var config = GetTestConfiguration();
        var logger = NullLogger<RequestsController>.Instance;
        var controller = new RequestsController(db, mockHttp.Object, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("", "")
        };

        // Act
        var result = await controller.RejectAllPendingRequestsForDonation(1) as UnauthorizedObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task BR_UT_04_RejectAllPendingOffers_AsOwningOrg_RejectsAllPendingOffers()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        mockProducer
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var config = GetTestConfiguration();
        var logger = NullLogger<OrgNeedRequestsController>.Instance;
        var controller = new OrgNeedRequestsController(db, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("org_50", "ORGANIZATION", "City Meals Charity")
        };

        // Seed food need request
        var need = new OrgFoodNeedRequest
        {
            Id = 10,
            OrganizationId = "org_50",
            OrganizationName = "City Meals Charity",
            Title = "Need 50 Packed Lunches",
            Category = "Cooked Meals",
            QuantityNeeded = 50,
            FulfilledQuantity = 30,
            RemainingNeeded = 20,
            Unit = "Meals",
            Description = "For night shelter",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "PARTIALLY_FULFILLED",
            CreatedAt = DateTime.UtcNow
        };
        db.OrgNeedRequests.Add(need);

        // Seed 2 PENDING offers and 1 ACCEPTED offer
        var offer1 = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 10,
            OrgFoodNeedRequestTitle = need.Title,
            DonorId = "donor_1",
            DonorName = "Donor One",
            FoodType = "Rice & Curry",
            OfferedQuantity = 10,
            Unit = "Meals",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var offer2 = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 10,
            OrgFoodNeedRequestTitle = need.Title,
            DonorId = "donor_2",
            DonorName = "Donor Two",
            FoodType = "Sandwiches",
            OfferedQuantity = 15,
            Unit = "Meals",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var offerAccepted = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 10,
            OrgFoodNeedRequestTitle = need.Title,
            DonorId = "donor_3",
            DonorName = "Donor Three",
            FoodType = "Salad Bowls",
            OfferedQuantity = 30,
            AcceptedQuantity = 30,
            Unit = "Meals",
            Status = "ACCEPTED",
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        db.DonorOffers.AddRange(offer1, offer2, offerAccepted);
        await db.SaveChangesAsync();

        var dto = new RejectRequestDto { Reason = "Goal reached through bulk donation" };

        // Act
        var result = await controller.RejectAllPendingOffers(10, dto) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var apiResp = result.Value as ApiResponse<List<DonorOfferResponseDto>>;
        apiResp.Should().NotBeNull();
        apiResp!.Success.Should().BeTrue();
        apiResp.Data.Should().HaveCount(2);

        // Verify status updates in database
        var dbOffer1 = await db.DonorOffers.FindAsync(offer1.Id);
        var dbOffer2 = await db.DonorOffers.FindAsync(offer2.Id);
        var dbOfferAccepted = await db.DonorOffers.FindAsync(offerAccepted.Id);

        dbOffer1!.Status.Should().Be("REJECTED");
        dbOffer2!.Status.Should().Be("REJECTED");
        dbOfferAccepted!.Status.Should().Be("ACCEPTED");

        // Verify notifications generated for donors
        var notifs = await db.Notifications.ToListAsync();
        notifs.Should().HaveCount(2);
        notifs.Should().Contain(n => n.UserId == "donor_1" && n.Type == "OFFER_REJECTED" && n.RelatedId == 10);
        notifs.Should().Contain(n => n.UserId == "donor_2" && n.Type == "OFFER_REJECTED" && n.RelatedId == 10);

        // Verify Kafka events published for rejected offers
        mockProducer.Verify(p => p.PublishEventAsync(
            "test-need-events",
            offer1.Id.ToString(),
            It.Is<FoodOfferStatusChangedEvent>(e => e.OfferId == offer1.Id && e.Status == "REJECTED"),
            It.IsAny<CancellationToken>()), Times.Once);

        mockProducer.Verify(p => p.PublishEventAsync(
            "test-need-events",
            offer2.Id.ToString(),
            It.Is<FoodOfferStatusChangedEvent>(e => e.OfferId == offer2.Id && e.Status == "REJECTED"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BR_UT_05_RejectAllPendingOffers_AsUnrelatedOrg_Returns403Forbidden()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        var config = GetTestConfiguration();
        var logger = NullLogger<OrgNeedRequestsController>.Instance;
        var controller = new OrgNeedRequestsController(db, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("org_other", "ORGANIZATION")
        };

        var need = new OrgFoodNeedRequest
        {
            Id = 15,
            OrganizationId = "org_owner",
            OrganizationName = "Owner Org",
            Title = "Need Food",
            Category = "Pantry",
            QuantityNeeded = 20,
            Unit = "Items",
            Description = "Test",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(1),
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Act
        var result = await controller.RejectAllPendingOffers(15) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task BR_UT_06_RejectAllPendingOffers_WhenNeedRequestNotFound_Returns404NotFound()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        var config = GetTestConfiguration();
        var logger = NullLogger<OrgNeedRequestsController>.Instance;
        var controller = new OrgNeedRequestsController(db, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("org_1", "ORGANIZATION")
        };

        // Act
        var result = await controller.RejectAllPendingOffers(999) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task BR_UT_07_RejectAllPendingOffers_WhenNoPendingOffers_Returns200WithEmptyList()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockProducer = new Mock<IRequestEventProducer>();
        var config = GetTestConfiguration();
        var logger = NullLogger<OrgNeedRequestsController>.Instance;
        var controller = new OrgNeedRequestsController(db, mockProducer.Object, config, logger)
        {
            ControllerContext = CreateControllerContext("org_1", "ORGANIZATION")
        };

        var need = new OrgFoodNeedRequest
        {
            Id = 20,
            OrganizationId = "org_1",
            OrganizationName = "Test Org",
            Title = "Need Water",
            Category = "Beverages",
            QuantityNeeded = 50,
            Unit = "Bottles",
            Description = "Bottles of water",
            Location = "Uptown",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Act
        var result = await controller.RejectAllPendingOffers(20) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var apiResp = result.Value as ApiResponse<List<DonorOfferResponseDto>>;
        apiResp.Should().NotBeNull();
        apiResp!.Success.Should().BeTrue();
        apiResp.Data.Should().BeEmpty();
    }
}
