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
public class CancelDonationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CancelDonationIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task CancelDonation_ValidRequestByOwner_UpdatesPostgresStatusToCancelledAndPublishesKafkaEvent()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-cancel-owner",
                DonorName = "Owner Bakery",
                FoodTitle = "Surplus Cakes",
                Category = "Bakery",
                TotalQuantity = 15,
                RemainingQuantity = 15,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-cancel-owner", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var cancelDto = new CancelDonationDto { Reason = "Baking batch ruined" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/donations/{donationId}/cancel", cancelDto);

        // Assert HTTP Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data!.Status.Should().Be("Cancelled");
        apiResult.Data.RemainingQuantity.Should().Be(0);

        // Assert PostgreSQL Persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var cancelledEntity = await db.Donations.FindAsync(donationId);
            cancelledEntity!.Status.Should().Be("Cancelled");
            cancelledEntity.RemainingQuantity.Should().Be(0);
            cancelledEntity.Notes.Should().Contain("[Cancelled by donor: Baking batch ruined]");
        }

        // Assert Kafka Event Publication
        _factory.TestEventProducer.PublishedEvents.Should().HaveCount(1);
        var publishedEvent = _factory.TestEventProducer.PublishedEvents.First();
        publishedEvent.EventType.Should().Be("DonationCancelled");
        publishedEvent.DonationId.Should().Be(donationId);
        publishedEvent.RemainingQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CancelDonation_ByAnotherDonor_ReturnsForbiddenAndPreservesPostgresStatus()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-cancel-owner",
                DonorName = "Owner Bakery",
                FoodTitle = "Protected Cakes",
                TotalQuantity = 15,
                RemainingQuantity = 15,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var attackerToken = AuthHelper.GenerateJwtToken("donor-attacker", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/donations/{donationId}/cancel", new CancelDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var entity = await db.Donations.FindAsync(donationId);
            entity!.Status.Should().Be("Posted");
        }
    }

    [Fact]
    public async Task CancelDonation_RepeatedCancellation_ReturnsBadRequest()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-cancel-owner",
                DonorName = "Owner Bakery",
                FoodTitle = "Already Cancelled Cake",
                TotalQuantity = 15,
                RemainingQuantity = 0,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Cancelled"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-cancel-owner", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/donations/{donationId}/cancel", new CancelDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelDonation_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var token = AuthHelper.GenerateJwtToken("donor-cancel-owner", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PatchAsJsonAsync("/api/donations/99999/cancel", new CancelDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
