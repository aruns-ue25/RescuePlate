using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RequestWorkflowService.Events;
using RequestWorkflowService.Kafka;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class KafkaEventUnitTests
{
    [Fact]
    public void DonationRequestCreatedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new DonationRequestCreatedEvent
        {
            RequestId = 101,
            DonationId = 50,
            DonationTitle = "Surplus Rice Packs",
            OrganizationId = "org_100",
            OrganizationName = "City Food Bank",
            DonorId = "donor_200",
            RequestedQuantity = 10,
            Unit = "Bags",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<DonationRequestCreatedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RequestId.Should().Be(101);
        deserialized.DonationId.Should().Be(50);
        deserialized.OrganizationId.Should().Be("org_100");
        deserialized.DonorId.Should().Be("donor_200");
        deserialized.RequestedQuantity.Should().Be(10);
    }

    [Fact]
    public void DonationRequestAcceptedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new DonationRequestAcceptedEvent
        {
            RequestId = 102,
            DonationId = 50,
            OrganizationId = "org_100",
            DonorId = "donor_200",
            AcceptedQuantity = 10,
            AcceptedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<DonationRequestAcceptedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RequestId.Should().Be(102);
        deserialized.AcceptedQuantity.Should().Be(10);
    }

    [Fact]
    public void DonationRequestRejectedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new DonationRequestRejectedEvent
        {
            RequestId = 103,
            DonationId = 50,
            OrganizationId = "org_100",
            DonorId = "donor_200",
            Reason = "No longer available",
            RejectedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<DonationRequestRejectedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RequestId.Should().Be(103);
        deserialized.Reason.Should().Be("No longer available");
    }

    [Fact]
    public void FoodNeedRequestCreatedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new FoodNeedRequestCreatedEvent
        {
            NeedRequestId = 201,
            OrganizationId = "org_300",
            OrganizationName = "Hope Shelter",
            Title = "Need 50 Meal Packs",
            Quantity = 50,
            Unit = "Packs",
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<FoodNeedRequestCreatedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.NeedRequestId.Should().Be(201);
        deserialized.Quantity.Should().Be(50);
        deserialized.Status.Should().Be("OPEN");
    }

    [Fact]
    public void FoodOfferCreatedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new FoodOfferCreatedEvent
        {
            OfferId = 301,
            NeedRequestId = 201,
            DonorId = "donor_500",
            DonorName = "Chef Alex",
            Quantity = 30,
            Unit = "Packs",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<FoodOfferCreatedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OfferId.Should().Be(301);
        deserialized.Quantity.Should().Be(30);
    }

    [Fact]
    public void FoodOfferStatusChangedEvent_SerializationAndPayload_ShouldBeValid()
    {
        // Arrange
        var @event = new FoodOfferStatusChangedEvent
        {
            OfferId = 301,
            NeedRequestId = 201,
            OrganizationId = "org_300",
            DonorId = "donor_500",
            Status = "ACCEPTED",
            RespondedAt = DateTime.UtcNow
        };

        // Act
        string json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<FoodOfferStatusChangedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OfferId.Should().Be(301);
        deserialized.Status.Should().Be("ACCEPTED");
    }

    [Fact]
    public async Task KafkaProducer_WhenDisabledInConfig_ShouldReturnFalseWithoutThrowing()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "Kafka:EnableProducer", "false" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        var loggerMock = new Mock<ILogger<KafkaRequestEventProducer>>();

        using var producer = new KafkaRequestEventProducer(config, loggerMock.Object);

        var sampleEvent = new DonationRequestCreatedEvent { RequestId = 1 };

        // Act
        bool result = await producer.PublishEventAsync("request.created", "key1", sampleEvent);

        // Assert
        result.Should().BeFalse();
    }
}
