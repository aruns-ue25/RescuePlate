using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.Events;
using DonationService.Kafka;
using DonationService.Services;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class KafkaIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public KafkaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EventProducer_DirectInvocation_TrapsEventInTestProducerWithCorrectMetadata()
    {
        // Arrange
        var testEvent = new DonationEvent
        {
            EventType = "DonationCreated",
            DonationId = 55,
            DonorId = "donor-kafka-1",
            DonorName = "Kafka Bakery",
            DonorEmail = "kafka@bakery.com",
            FoodTitle = "Whole Grain Bread",
            Category = "Bakery",
            TotalQuantity = 50,
            RemainingQuantity = 50,
            Unit = "portions",
            Location = "Test Location",
            Status = "Posted",
            ExpiryTime = DateTime.UtcNow.AddHours(4)
        };

        // Act
        var success = await _factory.TestEventProducer.PublishEventAsync(testEvent);

        // Assert
        success.Should().BeTrue();
        _factory.TestEventProducer.PublishedEvents.Should().HaveCount(1);

        var trappedEvent = _factory.TestEventProducer.PublishedEvents.First();
        trappedEvent.EventId.Should().NotBeEmpty();
        trappedEvent.EventType.Should().Be("DonationCreated");
        trappedEvent.DonationId.Should().Be(55);
        trappedEvent.DonorId.Should().Be("donor-kafka-1");
        trappedEvent.FoodTitle.Should().Be("Whole Grain Bread");
    }

    [Fact]
    public async Task KafkaProducerFailure_DoesNotRollbackOrFailDatabaseTransaction()
    {
        // Arrange - Create an event producer that throws an exception to simulate broker failure
        var failingProducerMock = new Mock<IDonationEventProducer>();
        failingProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<DonationEvent>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Simulated Kafka cluster connection failure"));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var service = new DonationServiceImpl(
            dbContext,
            NullLogger<DonationServiceImpl>.Instance,
            httpFactory,
            config,
            failingProducerMock.Object
        );

        var dto = new CreateDonationDto
        {
            FoodTitle = "Resilient Food Item",
            Category = "Bakery",
            TotalQuantity = 10,
            ExpiryHours = 4.0,
            Location = "123 Resilient St, Colombo"
        };

        // Act - Attempt donation creation; DB commit in PostgreSQL must succeed despite Kafka failure
        var result = await service.CreateDonationAsync("donor-resilient-1", "Resilient Business", "resilient@example.com", dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.FoodTitle.Should().Be("Resilient Food Item");

        var dbEntity = await dbContext.Donations.FindAsync(result.Data.Id);
        dbEntity.Should().NotBeNull();
        dbEntity!.FoodTitle.Should().Be("Resilient Food Item");
    }
}
