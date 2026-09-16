using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class DonationDetailsIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DonationDetailsIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetDonationById_ExistingDonationInPostgres_ReturnsCorrectDetailsAnd200OK()
    {
        // Arrange - Seed PostgreSQL DB
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-details-1",
                DonorName = "Grand Cafe",
                DonorEmail = "cafe@example.com",
                FoodTitle = "Gourmet Pastries",
                Category = "Bakery",
                TotalQuantity = 25,
                RemainingQuantity = 25,
                Unit = "boxes",
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = "Posted",
                Location = "Central Road, Colombo",
                Notes = "Store at room temp",
                DietaryTags = "Vegetarian"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        // Act
        var response = await _client.GetAsync($"/api/donations/{donationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult.Should().NotBeNull();
        apiResult!.Success.Should().BeTrue();

        var dto = apiResult.Data!;
        dto.Id.Should().Be(donationId);
        dto.DonorId.Should().Be("donor-details-1");
        dto.DonorName.Should().Be("Grand Cafe");
        dto.FoodTitle.Should().Be("Gourmet Pastries");
        dto.TotalQuantity.Should().Be(25);
        dto.RemainingQuantity.Should().Be(25);
        dto.Unit.Should().Be("boxes");
        dto.Status.Should().Be("Available");
        dto.Location.Should().Be("Central Road, Colombo");
        dto.Notes.Should().Be("Store at room temp");
        dto.DietaryTags.Should().Be("Vegetarian");
        dto.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GetDonationById_ExpiredDonation_ReturnsExpiredStatusAndIsExpiredTrue()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-details-1",
                DonorName = "Grand Cafe",
                FoodTitle = "Past Deadline Cupcakes",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(-1),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        // Act
        var response = await _client.GetAsync($"/api/donations/{donationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult!.Data!.IsExpired.Should().BeTrue();
        apiResult.Data.Status.Should().Be("Expired");
    }

    [Fact]
    public async Task GetDonationById_NonExistentId_ReturnsNotFound404()
    {
        // Act
        var response = await _client.GetAsync("/api/donations/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
