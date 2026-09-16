using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.IntegrationTests.Helpers;
using DonationService.Services;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class AvailabilityAndExpiryIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AvailabilityAndExpiryIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task UpdateAvailability_ValidExpiryHours_PersistsInPostgres()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-avail-1",
                DonorName = "Avail Bakery",
                FoodTitle = "Sandwiches",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(1),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-avail-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateAvailabilityDto { ExpiryHours = 8 };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/donations/{donationId}/availability", dto);

        // Assert HTTP Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data!.ExpiryTime.Should().BeAfter(DateTime.UtcNow.AddHours(7));

        // Assert PostgreSQL Persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var updatedEntity = await db.Donations.FindAsync(donationId);
            updatedEntity!.ExpiryTime.Should().BeAfter(DateTime.UtcNow.AddHours(7));
        }
    }

    [Fact]
    public async Task ProcessExpiredDonations_ExecutionAgainstPostgres_TransitionsPastDeadlineDonationsToExpired()
    {
        // Arrange - Seed past-deadline donation and valid donation in PostgreSQL
        int pastDeadlineId, activeId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var pastDonation = new Models.Donation
            {
                DonorId = "donor-avail-1",
                DonorName = "Avail Bakery",
                FoodTitle = "Old Doughnut",
                Category = "Bakery",
                TotalQuantity = 5,
                RemainingQuantity = 5,
                ExpiryTime = DateTime.UtcNow.AddHours(-2), // Past deadline
                Status = "Posted"
            };

            var activeDonation = new Models.Donation
            {
                DonorId = "donor-avail-1",
                DonorName = "Avail Bakery",
                FoodTitle = "Fresh Doughnut",
                Category = "Bakery",
                TotalQuantity = 5,
                RemainingQuantity = 5,
                ExpiryTime = DateTime.UtcNow.AddHours(3), // Future
                Status = "Posted"
            };

            db.Donations.AddRange(pastDonation, activeDonation);
            await db.SaveChangesAsync();
            pastDeadlineId = pastDonation.Id;
            activeId = activeDonation.Id;
        }

        // Act - Invoke expiry processing directly via scope
        int expiredCount;
        using (var scope = _factory.Services.CreateScope())
        {
            var donationService = scope.ServiceProvider.GetRequiredService<IDonationService>();
            expiredCount = await donationService.ProcessExpiredDonationsAsync();
        }

        // Assert
        expiredCount.Should().Be(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var pastEntity = await db.Donations.FindAsync(pastDeadlineId);
            pastEntity!.Status.Should().Be("Expired");

            var activeEntity = await db.Donations.FindAsync(activeId);
            activeEntity!.Status.Should().Be("Posted");
        }
    }

    [Fact]
    public async Task UpdateAvailability_WithPastExpiryTime_ReturnsBadRequest()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-avail-1",
                DonorName = "Avail Bakery",
                FoodTitle = "Sandwiches",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(2),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-avail-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateAvailabilityDto { ExpiryTime = DateTime.UtcNow.AddHours(-1) };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/donations/{donationId}/availability", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
