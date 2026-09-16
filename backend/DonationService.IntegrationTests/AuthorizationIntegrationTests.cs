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
public class AuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("/api/donations", "POST")]
    [InlineData("/api/donations/my-donations", "GET")]
    [InlineData("/api/donations/1", "PUT")]
    [InlineData("/api/donations/1/cancel", "PATCH")]
    [InlineData("/api/donations/1/availability", "PATCH")]
    public async Task ProtectedEndpoints_WithoutAuthorizationHeader_Return401Unauthorized(string endpoint, string httpMethod)
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        HttpResponseMessage response;
        if (httpMethod == "POST")
            response = await _client.PostAsJsonAsync(endpoint, new CreateDonationDto());
        else if (httpMethod == "PUT")
            response = await _client.PutAsJsonAsync(endpoint, new UpdateDonationDto());
        else if (httpMethod == "PATCH")
            response = await _client.PatchAsJsonAsync(endpoint, new CancelDonationDto());
        else
            response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDonation_WithOrganizationRoleToken_Returns403Forbidden()
    {
        // Arrange - Organization role attempting DONOR-only endpoint
        var token = AuthHelper.GenerateJwtToken("org-user-1", role: "ORGANIZATION");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateDonationDto
        {
            FoodTitle = "Test Food",
            Category = "Bakery",
            TotalQuantity = 10,
            ExpiryHours = 4.0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/donations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EditDonation_WithAnotherDonorToken_Returns403Forbidden()
    {
        // Arrange - Seed donation for donor-A
        int donationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var donation = new Models.Donation
            {
                DonorId = "donor-A",
                DonorName = "Donor A",
                FoodTitle = "Original Meal",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = "Posted"
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            donationId = donation.Id;
        }

        var tokenB = AuthHelper.GenerateJwtToken("donor-B", role: "DONOR");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/donations/{donationId}", new UpdateDonationDto { FoodTitle = "Hacked" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EndpointCall_WithInvalidSecretKeyJwtToken_Returns401Unauthorized()
    {
        // Arrange - Token signed with wrong secret key
        var invalidToken = AuthHelper.GenerateJwtToken("donor-A", role: "DONOR", secretKey: "Wrong_Secret_Key_For_Jwt_Authentication_2026_Invalid_Secret");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await _client.GetAsync("/api/donations/my-donations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
