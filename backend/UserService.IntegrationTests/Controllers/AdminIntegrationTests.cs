using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UserService.Controllers;
using UserService.Data;
using UserService.DTOs;
using UserService.IntegrationTests.Helpers;
using UserService.Models;
using Xunit;

namespace UserService.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class AdminIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AdminIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetAllUsers_AdminUser_Returns200OkWithUserList()
    {
        // Arrange (TC-ADM-01)
        var registerDto = new RegisterDto
        {
            Email = "admin_view_user@donor.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Visible Bistro",
            Location = "Downtown"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // Act
        var response = await adminClient.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        var count = content.GetProperty("count").GetInt32();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllUsers_DonorUser_Returns403Forbidden()
    {
        // Arrange (TC-ADM-02)
        var donorToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "donor@rescueplate.org", "DONOR");
        using var donorClient = _factory.CreateClient().WithBearerToken(donorToken);

        // Act
        var response = await donorClient.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange (TC-ADM-03)
        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleUserStatus_AdminUser_DeactivatesAndActivatesUser()
    {
        // Arrange (TC-ADM-04)
        var registerDto = new RegisterDto
        {
            Email = "toggle_target@donor.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Toggle Bistro",
            Location = "Downtown"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var targetUserId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);

        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // Act 1: Deactivate
        var deactivateDto = new StatusUpdateDto { IsActive = false };
        var deactivateResponse = await adminClient.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/status", deactivateDto);

        // Assert 1
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactContent = await deactivateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        deactContent.GetProperty("message").GetString().Should().Be("User account has been deactivated.");

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var targetInDb = await db.Users.FindAsync(targetUserId);
            targetInDb!.IsActive.Should().BeFalse();
        }

        // Act 2: Reactivate
        var activateDto = new StatusUpdateDto { IsActive = true };
        var activateResponse = await adminClient.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/status", activateDto);

        // Assert 2
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var actContent = await activateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        actContent.GetProperty("message").GetString().Should().Be("User account has been activated.");

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var targetInDb = await db.Users.FindAsync(targetUserId);
            targetInDb!.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ToggleUserStatus_NonExistentUser_Returns404NotFound()
    {
        // Arrange (TC-ADM-05)
        var nonExistentId = Guid.NewGuid();
        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        var statusDto = new StatusUpdateDto { IsActive = false };

        // Act
        var response = await adminClient.PatchAsJsonAsync($"/api/admin/users/{nonExistentId}/status", statusDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleUserStatus_DonorUser_Returns403Forbidden()
    {
        // Arrange (TC-ADM-06)
        var targetId = Guid.NewGuid();
        var donorToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "donor@rescueplate.org", "DONOR");
        using var donorClient = _factory.CreateClient().WithBearerToken(donorToken);

        var statusDto = new StatusUpdateDto { IsActive = false };

        // Act
        var response = await donorClient.PatchAsJsonAsync($"/api/admin/users/{targetId}/status", statusDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
