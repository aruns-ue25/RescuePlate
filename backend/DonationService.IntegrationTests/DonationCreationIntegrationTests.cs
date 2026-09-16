using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class DonationCreationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DonationCreationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateDonation_WithValidPayloadAndDonorToken_PersistsInPostgresAndPublishesKafkaEvent()
    {
        // Arrange
        var token = AuthHelper.GenerateJwtToken("donor-integration-1", role: "DONOR", businessName: "City Bakery", email: "bakery@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateDonationDto
        {
            FoodTitle = "Fresh Surplus Doughnuts",
            Category = "Bakery",
            TotalQuantity = 30,
            Unit = "boxes",
            ExpiryHours = 6.0,
            Location = "123 Test Street, Colombo",
            Notes = "Freshly made today",
            DietaryTags = "Vegetarian"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/donations", dto);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();

        var createdDto = apiResult.Data!;
        createdDto.Id.Should().BeGreaterThan(0);
        createdDto.DonorId.Should().Be("donor-integration-1");
        createdDto.FoodTitle.Should().Be("Fresh Surplus Doughnuts");
        createdDto.TotalQuantity.Should().Be(30);
        createdDto.RemainingQuantity.Should().Be(30);
        createdDto.Status.Should().Be("Available");

        // Assert PostgreSQL DB persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var dbEntity = await db.Donations.FirstOrDefaultAsync(d => d.Id == createdDto.Id);

            dbEntity.Should().NotBeNull();
            dbEntity!.DonorId.Should().Be("donor-integration-1");
            dbEntity.FoodTitle.Should().Be("Fresh Surplus Doughnuts");
            dbEntity.TotalQuantity.Should().Be(30);
            dbEntity.RemainingQuantity.Should().Be(30);
            dbEntity.Status.Should().Be("Posted");
        }

        // Assert Kafka Event Publication
        _factory.TestEventProducer.PublishedEvents.Should().HaveCount(1);
        var publishedEvent = _factory.TestEventProducer.PublishedEvents.First();
        publishedEvent.EventType.Should().Be("DonationCreated");
        publishedEvent.DonationId.Should().Be(createdDto.Id);
        publishedEvent.DonorId.Should().Be("donor-integration-1");
        publishedEvent.FoodTitle.Should().Be("Fresh Surplus Doughnuts");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateDonation_WithZeroOrNegativeQuantity_ReturnsBadRequestAndDoesNotPersistInPostgres(int invalidQty)
    {
        // Arrange
        var token = AuthHelper.GenerateJwtToken("donor-integration-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateDonationDto
        {
            FoodTitle = "Rice Bowls",
            Category = "Cooked Meals",
            TotalQuantity = invalidQty,
            ExpiryHours = 4.0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/donations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var count = await db.Donations.CountAsync();
            count.Should().Be(0);
        }

        _factory.TestEventProducer.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateDonation_WithPastExpiryTime_ReturnsBadRequestAndDoesNotPersistInPostgres()
    {
        // Arrange
        var token = AuthHelper.GenerateJwtToken("donor-integration-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateDonationDto
        {
            FoodTitle = "Salad Bowl",
            Category = "Fresh Produce",
            TotalQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/donations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var count = await db.Donations.CountAsync();
            count.Should().Be(0);
        }

        _factory.TestEventProducer.PublishedEvents.Should().BeEmpty();
    }
}
