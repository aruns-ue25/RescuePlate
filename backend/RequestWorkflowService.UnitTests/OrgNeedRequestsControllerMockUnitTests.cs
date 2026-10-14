using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RequestWorkflowService.Controllers;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Events;
using RequestWorkflowService.Kafka;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class OrgNeedRequestsControllerMockUnitTests
{
    [Fact]
    public async Task CreateNeedRequest_ValidData_ReturnsCreated_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FoodNeedRequestCreatedEvent>()))
            .ReturnsAsync(true);

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(
            db,
            eventProducerMock,
            callerId: "org_relief",
            callerName: "City Relief",
            callerRole: "ORGANIZATION");

        var dto = new CreateOrgNeedRequestDto
        {
            Title = "50 Meal Kits",
            Category = "Cooked Meals",
            QuantityNeeded = 50,
            Unit = "Kits",
            Description = "Urgent need for weekend food shelter",
            Location = "Central Shelter",
            NeededByDate = DateTime.UtcNow.AddDays(3)
        };

        // Act
        var result = await controller.CreateNeedRequest(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (result as CreatedAtActionResult)!.Value as ApiResponse<OrgNeedRequestResponseDto>;
        created.Should().NotBeNull();
        created!.Success.Should().BeTrue();
        created.Data!.Status.Should().Be("OPEN");
        created.Data.QuantityNeeded.Should().Be(50);

        // Verify in DB
        var saved = await db.OrgNeedRequests.FirstOrDefaultAsync(n => n.OrganizationId == "org_relief");
        saved.Should().NotBeNull();
        saved!.Status.Should().Be("OPEN");

        // Verify Kafka event mock was called
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            saved.Id.ToString(),
            It.Is<FoodNeedRequestCreatedEvent>(e => e.NeedRequestId == saved.Id && e.Quantity == 50)),
            Times.Once());
    }

    [Fact]
    public async Task CreateNeedRequest_PastNeededByDate_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db);

        var dto = new CreateOrgNeedRequestDto
        {
            Title = "Expired Need",
            Category = "Bakery",
            QuantityNeeded = 20,
            Unit = "Pcs",
            Description = "Past needed date",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = await controller.CreateNeedRequest(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<OrgNeedRequestResponseDto>;
        badRequest!.Message.Should().Contain("must be in the future");
    }

    [Fact]
    public async Task GetActiveNeedRequests_ReturnsOnlyOpenRequests()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.OrgNeedRequests.AddRange(
            new OrgFoodNeedRequest
            {
                OrganizationId = "org_1",
                Title = "Open Need",
                Category = "Produce",
                QuantityNeeded = 10,
                RemainingNeeded = 10,
                Unit = "kg",
                Description = "Fresh veg",
                Location = "North",
                NeededByDate = DateTime.UtcNow.AddDays(1),
                Status = "OPEN"
            },
            new OrgFoodNeedRequest
            {
                OrganizationId = "org_2",
                Title = "Fulfilled Need",
                Category = "Produce",
                QuantityNeeded = 20,
                RemainingNeeded = 0,
                Unit = "kg",
                Description = "Fresh fruits",
                Location = "South",
                NeededByDate = DateTime.UtcNow.AddDays(1),
                Status = "CLAIMED"
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db);

        // Act
        var result = await controller.GetActiveNeedRequests();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<OrgNeedRequestResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(1);
        ok.Data!.First().Status.Should().Be("OPEN");
    }

    [Fact]
    public async Task CreateDonorOffer_ValidOffer_ReturnsCreated_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_shelter",
            OrganizationName = "City Shelter",
            Title = "Need 30 Meals",
            Category = "Cooked Meals",
            QuantityNeeded = 30,
            RemainingNeeded = 30,
            Unit = "boxes",
            Description = "Evening distribution",
            Location = "Uptown",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FoodOfferCreatedEvent>()))
            .ReturnsAsync(true);

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(
            db,
            eventProducerMock,
            callerId: "donor_chef",
            callerName: "Chef Mario",
            callerRole: "DONOR");

        var dto = new CreateDonorOfferDto
        {
            FoodType = "Pasta",
            OfferedQuantity = 25,
            Notes = "Freshly prepared pasta"
        };

        // Act
        var result = await controller.CreateDonorOffer(need.Id, dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (result as CreatedAtActionResult)!.Value as ApiResponse<DonorOfferResponseDto>;
        created.Should().NotBeNull();
        created!.Success.Should().BeTrue();
        created.Data!.Status.Should().Be("PENDING");
        created.Data.OfferedQuantity.Should().Be(25);

        // Verify notification saved for organization
        var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == "org_shelter");
        notif.Should().NotBeNull();
        notif!.Title.Should().Be("New Food Offer");

        // Verify Kafka event mock called
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            created.Data.Id.ToString(),
            It.Is<FoodOfferCreatedEvent>(e => e.NeedRequestId == need.Id && e.Quantity == 25)),
            Times.Once());
    }

    [Fact]
    public async Task AcceptDonorOffer_AcceptOffer_UpdatesNeedAndOffer_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Need Bread",
            Category = "Bakery",
            QuantityNeeded = 50,
            RemainingNeeded = 50,
            Unit = "loaves",
            Description = "Breakfast bread",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(1),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = need.Id,
            DonorId = "donor_baker",
            DonorName = "Baker Bob",
            FoodType = "Baguettes",
            OfferedQuantity = 50,
            Unit = "loaves",
            Status = "PENDING"
        };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FoodOfferStatusChangedEvent>()))
            .ReturnsAsync(true);

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(
            db,
            eventProducerMock,
            callerId: "org_owner",
            callerRole: "ORGANIZATION");

        // Act
        var result = await controller.AcceptDonorOffer(offer.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<DonorOfferResponseDto>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Status.Should().Be("ACCEPTED");

        // Verify status in DB
        var updatedOffer = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        updatedOffer.Status.Should().Be("ACCEPTED");

        // Verify need request updated to CLAIMED because offer covered all 50
        var updatedNeed = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        updatedNeed.Status.Should().Be("CLAIMED");
        updatedNeed.RemainingNeeded.Should().Be(0);

        // Verify notification sent to donor
        var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == "donor_baker");
        notif.Should().NotBeNull();
        notif!.Type.Should().Be("OFFER_ACCEPTED");

        // Verify Kafka event mock called
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            offer.Id.ToString(),
            It.Is<FoodOfferStatusChangedEvent>(e => e.OfferId == offer.Id && e.Status == "ACCEPTED")),
            Times.Once());
    }

    [Fact]
    public async Task RejectDonorOffer_RejectOffer_UpdatesOffer_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Need Milk",
            Category = "Dairy",
            QuantityNeeded = 20,
            RemainingNeeded = 20,
            Unit = "liters",
            Description = "Fresh milk",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(1),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = need.Id,
            DonorId = "donor_dairy",
            DonorName = "Farm Dairy",
            FoodType = "Whole Milk",
            OfferedQuantity = 20,
            Unit = "liters",
            Status = "PENDING"
        };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FoodOfferStatusChangedEvent>()))
            .ReturnsAsync(true);

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(
            db,
            eventProducerMock,
            callerId: "org_owner",
            callerRole: "ORGANIZATION");

        // Act
        var result = await controller.RejectDonorOffer(offer.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<DonorOfferResponseDto>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Status.Should().Be("REJECTED");

        var updatedOffer = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        updatedOffer.Status.Should().Be("REJECTED");

        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            offer.Id.ToString(),
            It.Is<FoodOfferStatusChangedEvent>(e => e.OfferId == offer.Id && e.Status == "REJECTED")),
            Times.Once());
    }

    [Fact]
    public async Task GetMyNeedRequests_ReturnsOrganizationNeedRequests_WithOfferCounts()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Need Veggies",
            Category = "Produce",
            QuantityNeeded = 30,
            RemainingNeeded = 30,
            Unit = "kg",
            Description = "Fresh vegetables",
            Location = "Downtown",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        db.DonorOffers.Add(new DonorFoodOffer
        {
            OrgFoodNeedRequestId = need.Id,
            DonorId = "d1",
            FoodType = "Carrots",
            OfferedQuantity = 10,
            Status = "PENDING"
        });
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, callerId: "org_owner");

        // Act
        var result = await controller.GetMyNeedRequests();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<OrgNeedRequestResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(1);
        ok.Data!.First().PendingOffersCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateNeedRequest_ValidUpdate_UpdatesRequestInDb()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Old Title",
            Category = "General",
            QuantityNeeded = 20,
            RemainingNeeded = 20,
            Unit = "boxes",
            Description = "Old desc",
            Location = "Old loc",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, callerId: "org_owner");

        var dto = new UpdateOrgNeedRequestDto
        {
            Title = "New Title",
            Category = "Bakery",
            Description = "New desc",
            Location = "New loc",
            NeededByDate = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var result = await controller.UpdateNeedRequest(need.Id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<OrgNeedRequestResponseDto>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Title.Should().Be("New Title");

        var inDb = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        inDb.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task CancelNeedRequest_ExistingRequest_CancelsSuccessfully()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "To Cancel",
            QuantityNeeded = 10,
            RemainingNeeded = 10,
            NeededByDate = DateTime.UtcNow.AddDays(1),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, callerId: "org_owner");

        // Act
        var result = await controller.CancelNeedRequest(need.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var inDb = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        inDb.Status.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task GetNeedRequestById_ExistingAndNotFound_ReturnsExpectedResult()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_1",
            Title = "Details",
            QuantityNeeded = 15,
            RemainingNeeded = 15,
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db);

        // Act 1: Existing
        var foundResult = await controller.GetNeedRequestById(need.Id);
        foundResult.Should().BeOfType<OkObjectResult>();

        // Act 2: Not found
        var notFoundResult = await controller.GetNeedRequestById(9999);
        notFoundResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetOffersForNeedRequest_OwnerOrganization_ReturnsOffers()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Need with Offers",
            QuantityNeeded = 10,
            RemainingNeeded = 10,
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        db.DonorOffers.Add(new DonorFoodOffer
        {
            OrgFoodNeedRequestId = need.Id,
            DonorId = "d1",
            FoodType = "Rice",
            OfferedQuantity = 5,
            Status = "PENDING"
        });
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, callerId: "org_owner");

        // Act
        var result = await controller.GetOffersForNeedRequest(need.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<DonorOfferResponseDto>>;
        ok!.Data!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMySubmittedOffers_Donor_ReturnsSubmittedOffers()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.DonorOffers.AddRange(
            new DonorFoodOffer { DonorId = "donor_alpha", FoodType = "Bread", OfferedQuantity = 10, Status = "PENDING" },
            new DonorFoodOffer { DonorId = "donor_beta", FoodType = "Soup", OfferedQuantity = 5, Status = "PENDING" }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, callerId: "donor_alpha", callerRole: "DONOR");

        // Act
        var result = await controller.GetMySubmittedOffers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<DonorOfferResponseDto>>;
        ok!.Data!.Should().HaveCount(1);
        ok.Data!.First().DonorId.Should().Be("donor_alpha");
    }

    [Fact]
    public async Task GetOfferById_ExistingAndNotFound_ReturnsExpectedResult()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var offer = new DonorFoodOffer
        {
            DonorId = "d1",
            FoodType = "Cake",
            OfferedQuantity = 2,
            Status = "PENDING"
        };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db);

        // Act 1: Existing
        var foundResult = await controller.GetOfferById(offer.Id);
        foundResult.Should().BeOfType<OkObjectResult>();

        // Act 2: Not found
        var notFoundResult = await controller.GetOfferById(9999);
        notFoundResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectAllPendingOffers_MultiplePendingOffers_RejectsAll()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_owner",
            Title = "Need Bulk Reject",
            QuantityNeeded = 50,
            RemainingNeeded = 50,
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        db.DonorOffers.AddRange(
            new DonorFoodOffer { OrgFoodNeedRequestId = need.Id, DonorId = "d1", FoodType = "Apples", OfferedQuantity = 10, Status = "PENDING" },
            new DonorFoodOffer { OrgFoodNeedRequestId = need.Id, DonorId = "d2", FoodType = "Oranges", OfferedQuantity = 20, Status = "PENDING" }
        );
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        var controller = ControllerTestHelpers.CreateOrgNeedRequestsController(db, eventProducerMock, callerId: "org_owner");

        var dto = new RejectRequestDto { Reason = "Requirement fulfilled elsewhere" };

        // Act
        var result = await controller.RejectAllPendingOffers(need.Id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<DonorOfferResponseDto>>;
        ok!.Data!.Should().HaveCount(2);
        ok.Data!.All(o => o.Status == "REJECTED").Should().BeTrue();

        var inDb = await db.DonorOffers.Where(o => o.OrgFoodNeedRequestId == need.Id).ToListAsync();
        inDb.All(o => o.Status == "REJECTED").Should().BeTrue();
    }
}

