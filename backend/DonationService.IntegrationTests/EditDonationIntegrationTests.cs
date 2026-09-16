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
public class EditDonationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditDonationIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task UpdateDonation_ValidDataByOwner_UpdatesPostgresDatabaseAndPublishesKafkaEvent()
    {
        // Arrange - Seed DB with original donation
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-owner-1",
                DonorName = "Owner Bakery",
                FoodTitle = "Original Bread Title",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                Unit = "portions",
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = "Posted",
                Notes = "Original note"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-owner-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateDonationDto
        {
            FoodTitle = "Updated Gourmet Bread",
            Category = "Bakery",
            TotalQuantity = 20,
            Unit = "boxes",
            Notes = "Updated notes"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/donations/{donationId}", updateDto);

        // Assert HTTP Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<DonationResponseDto>>();
        apiResult!.Success.Should().BeTrue();
        apiResult.Data!.FoodTitle.Should().Be("Updated Gourmet Bread");
        apiResult.Data.TotalQuantity.Should().Be(20);
        apiResult.Data.RemainingQuantity.Should().Be(20);

        // Assert PostgreSQL Persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var updatedEntity = await db.Donations.FindAsync(donationId);
            updatedEntity!.FoodTitle.Should().Be("Updated Gourmet Bread");
            updatedEntity.TotalQuantity.Should().Be(20);
            updatedEntity.Notes.Should().Be("Updated notes");
        }

        // Assert Kafka Event Publication
        _factory.TestEventProducer.PublishedEvents.Should().HaveCount(1);
        var publishedEvent = _factory.TestEventProducer.PublishedEvents.First();
        publishedEvent.EventType.Should().Be("DonationUpdated");
        publishedEvent.DonationId.Should().Be(donationId);
        publishedEvent.FoodTitle.Should().Be("Updated Gourmet Bread");
    }

    [Fact]
    public async Task UpdateDonation_ByAnotherDonor_ReturnsForbiddenAndPreservesPostgresData()
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-owner-1",
                DonorName = "Owner Bakery",
                FoodTitle = "Protected Bread",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var attackerToken = AuthHelper.GenerateJwtToken("donor-attacker-999", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var updateDto = new UpdateDonationDto { FoodTitle = "Hacked Title" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/donations/{donationId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var entity = await db.Donations.FindAsync(donationId);
            entity!.FoodTitle.Should().Be("Protected Bread");
        }
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public async Task UpdateDonation_InRestrictedStatus_ReturnsBadRequestAndPreservesPostgresData(string restrictedStatus)
    {
        // Arrange
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-owner-1",
                DonorName = "Owner Bakery",
                FoodTitle = "Terminal Item",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 0,
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = restrictedStatus
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var token = AuthHelper.GenerateJwtToken("donor-owner-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateDonationDto { FoodTitle = "Attempted Modify" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/donations/{donationId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var entity = await db.Donations.FindAsync(donationId);
            entity!.FoodTitle.Should().Be("Terminal Item");
        }
    }

    [Fact]
    public async Task UpdateDonation_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var token = AuthHelper.GenerateJwtToken("donor-owner-1", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateDonationDto { FoodTitle = "Non Existent" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/donations/99999", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
