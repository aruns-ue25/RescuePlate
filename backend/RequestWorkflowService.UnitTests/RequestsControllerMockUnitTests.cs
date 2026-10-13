using System.Net;
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

public class RequestsControllerMockUnitTests
{
    [Fact]
    public async Task CreateRequest_ValidDonation_ReturnsCreated_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DonationRequestCreatedEvent>()))
            .ReturnsAsync(true);

        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new
            {
                id = 55,
                foodTitle = "Warm Meals",
                donorId = "donor_alpha",
                totalQuantity = 50,
                remainingQuantity = 40,
                unit = "boxes",
                status = "Available",
                expiryTime = DateTime.UtcNow.AddHours(6).ToString("O")
            }
        });

        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            eventProducerMock,
            stubHttp,
            callerId: "org_hope",
            callerName: "Hope Foundation",
            callerRole: "ORGANIZATION");

        var dto = new CreateFoodRequestDto
        {
            DonationId = 55,
            RequestedQuantity = 10,
            Notes = "For local community"
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (result as CreatedAtActionResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        created.Should().NotBeNull();
        created!.Success.Should().BeTrue();
        created.Data.Should().NotBeNull();
        created.Data!.RequestedQuantity.Should().Be(10);
        created.Data.DonationId.Should().Be(55);
        created.Data.Status.Should().Be("PENDING");

        // Verify DbContext saved request & notification
        var savedRequest = await db.Requests.FirstOrDefaultAsync(r => r.DonationId == 55);
        savedRequest.Should().NotBeNull();
        savedRequest!.Status.Should().Be("PENDING");

        var savedNotification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == "donor_alpha");
        savedNotification.Should().NotBeNull();
        savedNotification!.Title.Should().Be("New Food Request");

        // Verify Kafka event mock was called exactly once
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            savedRequest.Id.ToString(),
            It.Is<DonationRequestCreatedEvent>(e => e.RequestId == savedRequest.Id && e.RequestedQuantity == 10)),
            Times.Once());
    }

    [Fact]
    public async Task CreateRequest_ZeroOrNegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var controller = ControllerTestHelpers.CreateRequestsController(db);

        var dto = new CreateFoodRequestDto
        {
            DonationId = 1,
            RequestedQuantity = 0
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        badRequest!.Success.Should().BeFalse();
        badRequest.Message.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task CreateRequest_DonationNotFound_ReturnsNotFound()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.NotFound, null);
        var controller = ControllerTestHelpers.CreateRequestsController(db, httpClientFactory: stubHttp);

        var dto = new CreateFoodRequestDto
        {
            DonationId = 999,
            RequestedQuantity = 5
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = (result as NotFoundObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        notFound!.Success.Should().BeFalse();
        notFound.Message.Should().Contain("was not found");
    }

    [Fact]
    public async Task CreateRequest_ExpiredDonation_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new
            {
                id = 12,
                foodTitle = "Expired Soup",
                donorId = "donor_1",
                totalQuantity = 10,
                remainingQuantity = 10,
                unit = "bowls",
                status = "Available",
                expiryTime = DateTime.UtcNow.AddHours(-2).ToString("O")
            }
        });
        var controller = ControllerTestHelpers.CreateRequestsController(db, httpClientFactory: stubHttp);

        var dto = new CreateFoodRequestDto
        {
            DonationId = 12,
            RequestedQuantity = 2
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        badRequest!.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task CreateRequest_FullyClaimedDonation_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new
            {
                id = 15,
                foodTitle = "Claimed Meals",
                donorId = "donor_1",
                totalQuantity = 10,
                remainingQuantity = 0,
                unit = "boxes",
                status = "FullyClaimed",
                expiryTime = DateTime.UtcNow.AddHours(3).ToString("O")
            }
        });
        var controller = ControllerTestHelpers.CreateRequestsController(db, httpClientFactory: stubHttp);

        var dto = new CreateFoodRequestDto
        {
            DonationId = 15,
            RequestedQuantity = 1
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        badRequest!.Message.Should().Contain("no longer available");
    }

    [Fact]
    public async Task CreateRequest_QuantityExceedsRemaining_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new
            {
                id = 20,
                foodTitle = "Pies",
                donorId = "donor_1",
                totalQuantity = 10,
                remainingQuantity = 3,
                unit = "pieces",
                status = "Available",
                expiryTime = DateTime.UtcNow.AddHours(5).ToString("O")
            }
        });
        var controller = ControllerTestHelpers.CreateRequestsController(db, httpClientFactory: stubHttp);

        var dto = new CreateFoodRequestDto
        {
            DonationId = 20,
            RequestedQuantity = 5
        };

        // Act
        var result = await controller.CreateRequest(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        badRequest!.Message.Should().Contain("exceeds available quantity");
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOnlyCallerOrganizationRequests()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest
            {
                DonationId = 1,
                OrganizationId = "org_target",
                OrganizationName = "Target Org",
                DonorId = "d1",
                RequestedQuantity = 5,
                Status = "PENDING"
            },
            new FoodRequest
            {
                DonationId = 2,
                OrganizationId = "org_other",
                OrganizationName = "Other Org",
                DonorId = "d2",
                RequestedQuantity = 8,
                Status = "PENDING"
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateRequestsController(db, callerId: "org_target");

        // Act
        var result = await controller.GetMyRequests();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<FoodRequestResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(1);
        ok.Data!.First().OrganizationId.Should().Be("org_target");
    }

    [Fact]
    public async Task GetReceivedRequests_ReturnsOnlyCallerDonorRequests()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest
            {
                DonationId = 1,
                OrganizationId = "org_1",
                DonorId = "donor_mine",
                RequestedQuantity = 10,
                Status = "PENDING"
            },
            new FoodRequest
            {
                DonationId = 2,
                OrganizationId = "org_2",
                DonorId = "donor_mine",
                RequestedQuantity = 5,
                Status = "ACCEPTED"
            },
            new FoodRequest
            {
                DonationId = 3,
                OrganizationId = "org_3",
                DonorId = "donor_someone_else",
                RequestedQuantity = 12,
                Status = "PENDING"
            }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            callerId: "donor_mine",
            callerRole: "DONOR");

        // Act
        var result = await controller.GetReceivedRequests(status: "PENDING");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<FoodRequestResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(1);
        ok.Data!.First().Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task AcceptRequest_PendingRequest_DeductsQuantity_UpdatesStatus_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 100,
            DonationTitle = "Apples",
            OrganizationId = "org_recipient",
            OrganizationName = "Hope Center",
            DonorId = "donor_owner",
            RequestedQuantity = 15,
            Status = "PENDING"
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DonationRequestAcceptedEvent>()))
            .ReturnsAsync(true);

        // Stub DonationService deduct-quantity returning success
        var stubHttp = ControllerTestHelpers.CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new { remainingQuantity = 5, status = "PartiallyClaimed" }
        });

        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            eventProducerMock,
            stubHttp,
            callerId: "donor_owner",
            callerRole: "DONOR");

        // Act
        var result = await controller.AcceptRequest(request.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Status.Should().Be("ACCEPTED");
        ok.Data.AcceptedQuantity.Should().Be(15);

        // Verify status in DB
        var updated = await db.Requests.FirstAsync(r => r.Id == request.Id);
        updated.Status.Should().Be("ACCEPTED");
        updated.AcceptedQuantity.Should().Be(15);

        // Verify notification for recipient org
        var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == "org_recipient");
        notif.Should().NotBeNull();
        notif!.Type.Should().Be("REQUEST_ACCEPTED");

        // Verify Kafka event mock was called
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            request.Id.ToString(),
            It.Is<DonationRequestAcceptedEvent>(e => e.RequestId == request.Id && e.AcceptedQuantity == 15)),
            Times.Once());
    }

    [Fact]
    public async Task AcceptRequest_NonPendingRequest_ReturnsBadRequest()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 101,
            DonorId = "donor_owner",
            OrganizationId = "org_1",
            RequestedQuantity = 5,
            Status = "ACCEPTED"
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            callerId: "donor_owner",
            callerRole: "DONOR");

        // Act
        var result = await controller.AcceptRequest(request.Id);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (result as BadRequestObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        badRequest!.Message.Should().Contain("already been");
    }

    [Fact]
    public async Task RejectRequest_PendingRequest_UpdatesStatus_AndPublishesKafkaEvent()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 102,
            DonationTitle = "Milk cartons",
            DonorId = "donor_owner",
            OrganizationId = "org_1",
            RequestedQuantity = 8,
            Status = "PENDING"
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DonationRequestRejectedEvent>()))
            .ReturnsAsync(true);

        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            eventProducerMock,
            callerId: "donor_owner",
            callerRole: "DONOR");

        var dto = new RejectRequestDto
        {
            Reason = "No longer suitable for transport"
        };

        // Act
        var result = await controller.RejectRequest(request.Id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<FoodRequestResponseDto>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Status.Should().Be("REJECTED");
        ok.Data.RejectionReason.Should().Be("No longer suitable for transport");

        // Verify status in DB
        var updated = await db.Requests.FirstAsync(r => r.Id == request.Id);
        updated.Status.Should().Be("REJECTED");
        updated.RejectionReason.Should().Be("No longer suitable for transport");

        // Verify Kafka event mock was called
        eventProducerMock.Verify(p => p.PublishEventAsync(
            It.IsAny<string>(),
            request.Id.ToString(),
            It.Is<DonationRequestRejectedEvent>(e => e.RequestId == request.Id && e.Reason == dto.Reason)),
            Times.Once());
    }

    [Fact]
    public async Task RejectAllPendingRequestsForDonation_MultiplePendingRequests_RejectsAll()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest
            {
                DonationId = 300,
                DonorId = "donor_owner",
                OrganizationId = "org_1",
                RequestedQuantity = 5,
                Status = "PENDING"
            },
            new FoodRequest
            {
                DonationId = 300,
                DonorId = "donor_owner",
                OrganizationId = "org_2",
                RequestedQuantity = 10,
                Status = "PENDING"
            }
        );
        await db.SaveChangesAsync();

        var eventProducerMock = new Mock<IRequestEventProducer>();
        var controller = ControllerTestHelpers.CreateRequestsController(
            db,
            eventProducerMock,
            callerId: "donor_owner",
            callerRole: "DONOR");

        var dto = new RejectRequestDto { Reason = "Donation expired" };

        // Act
        var result = await controller.RejectAllPendingRequestsForDonation(300, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<List<FoodRequestResponseDto>>;
        ok!.Success.Should().BeTrue();
        ok.Data!.Should().HaveCount(2);
        ok.Data!.All(r => r.Status == "REJECTED").Should().BeTrue();

        // Verify in DB
        var inDb = await db.Requests.Where(r => r.DonationId == 300).ToListAsync();
        inDb.All(r => r.Status == "REJECTED").Should().BeTrue();
    }

    [Fact]
    public async Task GetRequestById_ExistingAndNotFound_ReturnsExpectedResult()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 1,
            DonorId = "d1",
            OrganizationId = "o1",
            RequestedQuantity = 5,
            Status = "PENDING"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateRequestsController(db);

        // Act 1: Existing
        var foundResult = await controller.GetRequestById(req.Id);
        // Assert 1
        foundResult.Should().BeOfType<OkObjectResult>();

        // Act 2: Non-existent
        var notFoundResult = await controller.GetRequestById(9999);
        // Assert 2
        notFoundResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAdminRequestSummary_ReturnsMetrics()
    {
        // Arrange
        using var db = ControllerTestHelpers.CreateInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest { DonationId = 1, DonorId = "d1", OrganizationId = "o1", RequestedQuantity = 10, AcceptedQuantity = 10, Status = "ACCEPTED" },
            new FoodRequest { DonationId = 2, DonorId = "d2", OrganizationId = "o2", RequestedQuantity = 5, Status = "PENDING" },
            new FoodRequest { DonationId = 3, DonorId = "d3", OrganizationId = "o3", RequestedQuantity = 3, Status = "REJECTED" }
        );
        await db.SaveChangesAsync();

        var controller = ControllerTestHelpers.CreateRequestsController(db, callerRole: "ADMIN");

        // Act
        var result = await controller.GetAdminRequestSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (result as OkObjectResult)!.Value as ApiResponse<object>;
        ok!.Success.Should().BeTrue();
        ok.Data.Should().NotBeNull();
    }
}

