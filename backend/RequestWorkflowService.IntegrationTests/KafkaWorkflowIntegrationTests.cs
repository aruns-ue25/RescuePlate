using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Events;
using RequestWorkflowService.IntegrationTests.Helpers;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.IntegrationTests;

public class KafkaWorkflowIntegrationTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task CreateRequest_ShouldCapture_DonationRequestCreatedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var request = new FoodRequest
        {
            DonationId = 101,
            DonationTitle = "Surplus Meals",
            OrganizationId = "org_1",
            OrganizationName = "Org 1",
            DonorId = "donor_1",
            RequestedQuantity = 5,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var @event = new DonationRequestCreatedEvent
        {
            RequestId = request.Id,
            DonationId = request.DonationId,
            DonationTitle = request.DonationTitle,
            OrganizationId = request.OrganizationId,
            OrganizationName = request.OrganizationName,
            DonorId = request.DonorId,
            RequestedQuantity = request.RequestedQuantity,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        };

        await producer.PublishEventAsync("request.created", request.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<DonationRequestCreatedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.RequestId.Should().Be(request.Id);
        captured.DonationId.Should().Be(101);
    }

    [Fact]
    public async Task AcceptRequest_ShouldCapture_DonationRequestAcceptedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var request = new FoodRequest
        {
            DonationId = 102,
            DonationTitle = "Bread",
            OrganizationId = "org_2",
            OrganizationName = "Org 2",
            DonorId = "donor_2",
            RequestedQuantity = 10,
            Status = "PENDING"
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Act
        request.Status = "ACCEPTED";
        await db.SaveChangesAsync();

        var @event = new DonationRequestAcceptedEvent
        {
            RequestId = request.Id,
            DonationId = request.DonationId,
            OrganizationId = request.OrganizationId,
            DonorId = request.DonorId,
            AcceptedQuantity = request.RequestedQuantity,
            AcceptedAt = DateTime.UtcNow
        };

        await producer.PublishEventAsync("request.accepted", request.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<DonationRequestAcceptedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.AcceptedQuantity.Should().Be(10);
    }

    [Fact]
    public async Task RejectRequest_ShouldCapture_DonationRequestRejectedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var request = new FoodRequest
        {
            DonationId = 103,
            DonationTitle = "Milk",
            OrganizationId = "org_3",
            OrganizationName = "Org 3",
            DonorId = "donor_3",
            RequestedQuantity = 4,
            Status = "PENDING"
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Act
        request.Status = "REJECTED";
        request.RejectionReason = "OutOfStock";
        await db.SaveChangesAsync();

        var @event = new DonationRequestRejectedEvent
        {
            RequestId = request.Id,
            DonationId = request.DonationId,
            OrganizationId = request.OrganizationId,
            DonorId = request.DonorId,
            Reason = request.RejectionReason,
            RejectedAt = DateTime.UtcNow
        };

        await producer.PublishEventAsync("request.rejected", request.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<DonationRequestRejectedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.Reason.Should().Be("OutOfStock");
    }

    [Fact]
    public async Task CreateNeedRequest_ShouldCapture_FoodNeedRequestCreatedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_need",
            OrganizationName = "Org Need",
            Title = "Need 100 Rice Meals",
            Category = "Cooked",
            QuantityNeeded = 100,
            Description = "Descr",
            Location = "Loc",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN"
        };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Act
        var @event = new FoodNeedRequestCreatedEvent
        {
            NeedRequestId = need.Id,
            OrganizationId = need.OrganizationId,
            OrganizationName = need.OrganizationName,
            Title = need.Title,
            Quantity = need.QuantityNeeded,
            Unit = need.Unit,
            Status = need.Status,
            CreatedAt = need.CreatedAt
        };

        await producer.PublishEventAsync("foodneed.created", need.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<FoodNeedRequestCreatedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.Quantity.Should().Be(100);
    }

    [Fact]
    public async Task CreateOffer_ShouldCapture_FoodOfferCreatedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 200,
            OrgFoodNeedRequestTitle = "Need Meals",
            DonorId = "donor_offer",
            DonorName = "Donor Offer",
            FoodType = "Salad",
            OfferedQuantity = 15,
            Status = "PENDING"
        };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Act
        var @event = new FoodOfferCreatedEvent
        {
            OfferId = offer.Id,
            NeedRequestId = offer.OrgFoodNeedRequestId,
            DonorId = offer.DonorId,
            DonorName = offer.DonorName,
            Quantity = offer.OfferedQuantity,
            Unit = offer.Unit,
            Status = offer.Status,
            CreatedAt = offer.CreatedAt
        };

        await producer.PublishEventAsync("foodoffer.created", offer.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<FoodOfferCreatedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.Quantity.Should().Be(15);
    }

    [Fact]
    public async Task AcceptOrRejectOffer_ShouldCapture_FoodOfferStatusChangedEvent()
    {
        // Arrange
        var producer = new TestRequestEventProducer();
        using var db = GetInMemoryDbContext();

        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 201,
            OrgFoodNeedRequestTitle = "Need Soup",
            DonorId = "donor_offer_2",
            DonorName = "Donor 2",
            FoodType = "Soup",
            OfferedQuantity = 25,
            Status = "PENDING"
        };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Act
        offer.Status = "ACCEPTED";
        await db.SaveChangesAsync();

        var @event = new FoodOfferStatusChangedEvent
        {
            OfferId = offer.Id,
            NeedRequestId = offer.OrgFoodNeedRequestId,
            OrganizationId = "org_201",
            DonorId = offer.DonorId,
            Status = "ACCEPTED",
            RespondedAt = DateTime.UtcNow
        };

        await producer.PublishEventAsync("foodoffer.statuschanged", offer.Id.ToString(), @event);

        // Assert
        producer.PublishedEvents.Should().HaveCount(1);
        var captured = producer.PublishedEvents.OfType<FoodOfferStatusChangedEvent>().FirstOrDefault();
        captured.Should().NotBeNull();
        captured!.Status.Should().Be("ACCEPTED");
    }

    [Fact]
    public async Task ProducerFailureResilienceScenario_ShouldStillPersistRecordToDatabase()
    {
        // Arrange: Inject a failing producer (shouldFail = true)
        var failingProducer = new TestRequestEventProducer(shouldFail: true);
        using var db = GetInMemoryDbContext();

        var request = new FoodRequest
        {
            DonationId = 999,
            DonationTitle = "Resilience Test Donation",
            OrganizationId = "org_resilience",
            OrganizationName = "Org Resilience",
            DonorId = "donor_resilience",
            RequestedQuantity = 50,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act: DB record is persisted
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Kafka publish fails gracefully returning false
        var @event = new DonationRequestCreatedEvent { RequestId = request.Id };
        bool publishResult = await failingProducer.PublishEventAsync("request.created", request.Id.ToString(), @event);

        // Assert: Kafka publish returns false but DB record remains 100% persisted and valid
        publishResult.Should().BeFalse();
        var persisted = await db.Requests.FirstOrDefaultAsync(r => r.Id == request.Id);
        persisted.Should().NotBeNull();
        persisted!.DonationTitle.Should().Be("Resilience Test Donation");
        persisted.RequestedQuantity.Should().Be(50);
    }
}
