using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class ViewOwnDonationsIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ViewOwnDonationsIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetMyDonations_AuthenticatedDonor_ReturnsOnlyOwnDonationsFromPostgres()
    {
        // Arrange - Seed DB with donations for Donor A and Donor B
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            db.Donations.AddRange(
                new Models.Donation
                {
                    DonorId = "donor-A",
                    DonorName = "Donor A Shop",
                    FoodTitle = "Donor A Meal 1",
                    Category = "Cooked Meals",
                    TotalQuantity = 10,
                    RemainingQuantity = 10,
                    ExpiryTime = DateTime.UtcNow.AddHours(4),
                    Status = "Posted"
                },
                new Models.Donation
                {
                    DonorId = "donor-A",
                    DonorName = "Donor A Shop",
                    FoodTitle = "Donor A Meal 2",
                    Category = "Bakery",
                    TotalQuantity = 15,
                    RemainingQuantity = 15,
                    ExpiryTime = DateTime.UtcNow.AddHours(5),
                    Status = "Posted"
                },
                new Models.Donation
                {
                    DonorId = "donor-B",
                    DonorName = "Donor B Shop",
                    FoodTitle = "Donor B Secret Meal",
                    Category = "Cooked Meals",
                    TotalQuantity = 20,
                    RemainingQuantity = 20,
                    ExpiryTime = DateTime.UtcNow.AddHours(3),
                    Status = "Posted"
                }
            );
            await db.SaveChangesAsync();
        }

        var tokenA = AuthHelper.GenerateJwtToken("donor-A", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // Act
        var response = await _client.GetAsync("/api/donations/my-donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DonationResponseDto>>>();
        result.Should().NotBeNull();
        var donations = result.Data!;
        donations.Should().HaveCount(2);
        donations.Should().OnlyContain(d => d.DonorId == "donor-A");
        donations.Select(d => d.FoodTitle).Should().NotContain("Donor B Secret Meal");
    }

    [Fact]
    public async Task GetMyDonations_WhenDonorHasNoListings_ReturnsEmptyList()
    {
        // Arrange
        var tokenNew = AuthHelper.GenerateJwtToken("donor-new", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNew);

        // Act
        var response = await _client.GetAsync("/api/donations/my-donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DonationResponseDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDonations_WithoutJwtToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/donations/my-donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
