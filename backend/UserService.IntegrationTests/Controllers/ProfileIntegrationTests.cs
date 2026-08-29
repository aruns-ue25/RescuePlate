using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using UserService.DTOs;
using UserService.IntegrationTests.Helpers;
using UserService.Models;
using Xunit;

namespace UserService.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class ProfileIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProfileIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetMyProfile_AuthenticatedDonor_Returns200OkWithDonorData()
    {
        // Arrange (TC-PROF-01)
        var registerDto = new RegisterDto
        {
            Email = "prof_donor@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Profile Bakery",
            ContactName = "Baker Alice",
            Location = "12 Main St",
            DonorType = "Bakery"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        // Act
        var response = await authClient.GetAsync("/api/profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("businessOrOrgName").GetString().Should().Be("Profile Bakery");
        content.GetProperty("data").GetProperty("donorType").GetString().Should().Be("Bakery");
        content.GetProperty("data").GetProperty("role").GetString().Should().Be("DONOR");
    }

    [Fact]
    public async Task GetMyProfile_AuthenticatedOrganization_Returns200OkWithCategories()
    {
        // Arrange (TC-PROF-02)
        var registerDto = new RegisterDto
        {
            Email = "prof_org@charity.org",
            Password = "Password123!",
            Role = UserRole.ORGANIZATION,
            BusinessOrOrgName = "Charity Hub",
            ContactName = "Manager John",
            Location = "99 Hope Rd",
            AcceptedFoodTypes = new List<string> { "Cooked Meals", "Bakery" }
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        // Act
        var response = await authClient.GetAsync("/api/profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("data").GetProperty("role").GetString().Should().Be("ORGANIZATION");
        var categories = content.GetProperty("data").GetProperty("acceptedFoodCategories").EnumerateArray().Select(e => e.GetString()).ToList();
        categories.Should().Contain("Cooked Meals");
        categories.Should().Contain("Bakery");
    }

    [Fact]
    public async Task GetMyProfile_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange (TC-PROF-03)
        // Act
        var response = await _client.GetAsync("/api/profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidUpdates_PersistsAndReturnsUpdatedData()
    {
        // Arrange (TC-PROF-05)
        var registerDto = new RegisterDto
        {
            Email = "update_test@bistro.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Initial Bistro",
            Location = "Initial Location"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        var updateDto = new UpdateProfileDto
        {
            BusinessOrOrgName = "Renamed Artisan Bistro",
            ContactName = "Executive Chef Dan",
            Phone = "+1-555-8888",
            Address = "New Location 555",
            BioOrDescription = "Award winning bistro cuisine"
        };

        // Act
        var response = await authClient.PutAsJsonAsync("/api/profile/me", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("businessOrOrgName").GetString().Should().Be("Renamed Artisan Bistro");
        content.GetProperty("data").GetProperty("contactName").GetString().Should().Be("Executive Chef Dan");
        content.GetProperty("data").GetProperty("address").GetString().Should().Be("New Location 555");
    }

    [Fact]
    public async Task GetPublicProfile_ExistingUser_Returns200OkWithPublicSafeFields()
    {
        // Arrange (TC-PROF-08)
        var registerDto = new RegisterDto
        {
            Email = "public_view@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Public Crust",
            Location = "Public Lane 1"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var userId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);

        // Act (Unauthenticated public call)
        var response = await _client.GetAsync($"/api/profile/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("businessOrOrgName").GetString().Should().Be("Public Crust");

        // Verify sensitive fields are not leaked in public view
        content.GetProperty("data").TryGetProperty("email", out _).Should().BeFalse();
        content.GetProperty("data").TryGetProperty("passwordHash", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicProfile_NonExistentUser_Returns404NotFound()
    {
        // Arrange (TC-PROF-09)
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/profile/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
