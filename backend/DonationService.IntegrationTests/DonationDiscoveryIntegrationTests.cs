using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class DonationDiscoveryIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DonationDiscoveryIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetAllAvailableDonations_PublicEndpoint_ReturnsOnlyActiveListingsFromPostgres()
    {
        // Arrange - Seed PostgreSQL DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();

            var validAvailable = new Models.Donation
            {
                DonorId = "donor-disc-1",
                DonorName = "Fresh Bakery",
                FoodTitle = "Warm Cinnamon Rolls",
                Category = "Bakery",
                TotalQuantity = 15,
                RemainingQuantity = 15,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Posted"
            };

            var cancelled = new Models.Donation
            {
                DonorId = "donor-disc-1",
                DonorName = "Fresh Bakery",
                FoodTitle = "Cancelled Cake",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 0,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Cancelled"
            };

            var expired = new Models.Donation
            {
                DonorId = "donor-disc-2",
                DonorName = "City Cafe",
                FoodTitle = "Expired Soup",
                Category = "Cooked Meals",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(-1), // Expired
                Status = "Posted"
            };

            db.Donations.AddRange(validAvailable, cancelled, expired);
            await db.SaveChangesAsync();
        }

        // Act - Public GET call (unauthenticated)
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<DonationResponseDto>>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data.Should().HaveCount(1);
        apiResult.Data!.First().FoodTitle.Should().Be("Warm Cinnamon Rolls");
    }

    [Fact]
    public async Task GetAllAvailableDonations_WithCategoryFilter_ReturnsMatchingListingsOnly()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            db.Donations.AddRange(
                new Models.Donation
                {
                    DonorId = "donor-disc-1",
                    DonorName = "Bakery One",
                    FoodTitle = "Bagels",
                    Category = "Bakery",
                    TotalQuantity = 10,
                    RemainingQuantity = 10,
                    ExpiryTime = DateTime.UtcNow.AddHours(5),
                    Status = "Posted"
                },
                new Models.Donation
                {
                    DonorId = "donor-disc-2",
                    DonorName = "Diner Two",
                    FoodTitle = "Fried Rice",
                    Category = "Cooked Meals",
                    TotalQuantity = 20,
                    RemainingQuantity = 20,
                    ExpiryTime = DateTime.UtcNow.AddHours(5),
                    Status = "Posted"
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/donations?category=Bakery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<DonationResponseDto>>>();
        apiResult!.Data.Should().HaveCount(1);
        apiResult.Data!.First().FoodTitle.Should().Be("Bagels");
    }

    [Fact]
    public async Task GetAllAvailableDonations_WhenNoAvailableListingsExist_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<DonationResponseDto>>>();
        apiResult!.Data.Should().BeEmpty();
    }
}
