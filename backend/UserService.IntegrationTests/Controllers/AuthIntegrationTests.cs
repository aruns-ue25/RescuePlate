using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
using UserService.DTOs;
using UserService.IntegrationTests.Helpers;
using UserService.Models;
using Xunit;

namespace UserService.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task Register_ValidDonor_Returns201CreatedAndPersistsInDatabase()
    {
        // Arrange (TC-AUTH-01)
        var registerDto = new RegisterDto
        {
            Email = "integration_donor@bakery.com",
            Password = "DonorPassword123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Sweet Crust Bakery",
            ContactName = "Baker Bob",
            Phone = "+1-555-1234",
            Location = "123 Sugar Lane",
            DonorType = "Bakery"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("email").GetString().Should().Be("integration_donor@bakery.com");
        content.GetProperty("data").GetProperty("role").GetString().Should().Be("DONOR");
        content.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();

        // Verify Database state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
        var userInDb = db.Users.FirstOrDefault(u => u.Email == "integration_donor@bakery.com");
        userInDb.Should().NotBeNull();
        userInDb!.Role.Should().Be(UserRole.DONOR);
    }

    [Fact]
    public async Task Register_ValidOrganization_Returns201Created()
    {
        // Arrange (TC-AUTH-02)
        var registerDto = new RegisterDto
        {
            Email = "integration_charity@shelter.org",
            Password = "OrgPassword123!",
            Role = UserRole.ORGANIZATION,
            BusinessOrOrgName = "St. Jude Food Shelter",
            ContactName = "Sister Ann",
            Location = "456 Charity Road",
            AcceptedFoodTypes = new List<string> { "Cooked Meals", "Bakery" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("role").GetString().Should().Be("ORGANIZATION");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        // Arrange (TC-AUTH-03)
        var registerDto = new RegisterDto
        {
            Email = "dup_test@rescue.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "First Org",
            Location = "Downtown"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Act - Duplicate registration
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await duplicateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeFalse();
        content.GetProperty("message").GetString().Should().Contain("already exists");
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400BadRequest()
    {
        // Arrange (TC-AUTH-04)
        var invalidDto = new RegisterDto
        {
            Email = "invalid-email-format",
            Password = "123", // Short password (< 6 chars)
            BusinessOrOrgName = "",
            Location = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200OkWithJwtToken()
    {
        // Arrange (TC-AUTH-06)
        var email = "login_success@test.com";
        var password = "ValidPassword123!";
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Gourmet Kitchen",
            Location = "Central St"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        content.GetProperty("data").GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401Unauthorized()
    {
        // Arrange (TC-AUTH-07 / TC-AUTH-08)
        var loginDto = new LoginDto { Email = "nonexistent@user.com", Password = "WrongPassword123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeFalse();
        content.GetProperty("message").GetString().Should().Be("Invalid email address or password.");
    }

    [Fact]
    public async Task Login_DeactivatedAccount_Returns401UnauthorizedWithDeactivatedMessage()
    {
        // Arrange (TC-AUTH-09)
        var email = "deactivated_user@test.com";
        var password = "MyPassword123!";
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Banned Cafe",
            Location = "Outskirts"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Deactivate user in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var user = db.Users.First(u => u.Email == email);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }

        var loginDto = new LoginDto { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("message").GetString().Should().Be("Your account has been deactivated by the system administrator.");
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns200Ok()
    {
        // Arrange (TC-AUTH-12)
        var token = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "logout_test@test.com", "DONOR");
        using var clientWithAuth = _factory.CreateClient().WithBearerToken(token);

        // Act
        var response = await clientWithAuth.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange (TC-AUTH-13)
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_ValidPassword_PermanentlyDeletesAccount()
    {
        // Arrange (TC-AUTH-14)
        var email = "delete_me@test.com";
        var password = "PasswordToDelete123!";
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Temp Bakery",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);
        var deleteDto = new DeleteAccountDto { Password = password };

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        };

        // Act
        var response = await authClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is gone from DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
        db.Users.Any(u => u.Email == email).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_Returns400BadRequest()
    {
        // Arrange (TC-AUTH-15)
        var email = "delete_safe@test.com";
        var password = "RealPassword123!";
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Keep Safe Bistro",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);
        var deleteDto = new DeleteAccountDto { Password = "WrongPassword999!" };

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        };

        // Act
        var response = await authClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
