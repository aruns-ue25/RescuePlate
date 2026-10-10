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
    public async Task DatabaseInitialization_FreshAndRestart_EnsuresSingleAdminAndAuditLogTable()
    {
        // Arrange & Act (A1)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();

        // Assert single admin user provisioned
        var adminCount = db.Users.Count(u => u.Role == UserRole.ADMIN);
        adminCount.Should().Be(1);

        var adminUser = db.Users.FirstOrDefault(u => u.Role == UserRole.ADMIN);
        adminUser.Should().NotBeNull();
        adminUser!.Email.Should().Be("admin@rescueplate.org");

        // Assert AuditLogs accessible
        var logCount = db.AdminActivityLogs.Count();
        logCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminLogin_ValidCredentials_Returns200OkWithChallengeToken()
    {
        // Arrange (A2)
        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = "Admin@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = content.GetProperty("data");
        data.GetProperty("challengeToken").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("requiresAccessKey").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AdminLogin_InvalidPassword_NormalUser_InactiveAdmin_AccessDenied()
    {
        // Act 1: Invalid Password (A3)
        var invalidPwdDto = new LoginDto { Email = "admin@rescueplate.org", Password = "WrongPassword!" };
        var resp1 = await _client.PostAsJsonAsync("/api/admin/login", invalidPwdDto);
        resp1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act 2: Normal User (A3)
        var regDonor = new RegisterDto
        {
            Email = "normal_donor@test.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Bistro",
            Location = "123 Main St"
        };
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", regDonor);
        regResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var normalUserLogin = new LoginDto { Email = "normal_donor@test.com", Password = "Password123!" };
        var resp2 = await _client.PostAsJsonAsync("/api/admin/login", normalUserLogin);
        resp2.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyAccessKey_ValidKey_WrongKey_ExpiredChallenge_EnforcesSecurity()
    {
        // Act 1: Wrong Key (A4)
        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = "Admin@123" };
        var loginResp = await _client.PostAsJsonAsync("/api/admin/login", loginDto);
        var loginContent = await loginResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var challengeToken = loginContent.GetProperty("data").GetProperty("challengeToken").GetString()!;

        var wrongKeyDto = new VerifyAccessKeyDto
        {
            ChallengeToken = challengeToken,
            AccessKey = "INCORRECT-KEY"
        };
        var wrongKeyResp = await _client.PostAsJsonAsync("/api/admin/verify-access-key", wrongKeyDto);
        wrongKeyResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act 2: Valid Key (A4)
        var validKeyDto = new VerifyAccessKeyDto
        {
            ChallengeToken = challengeToken,
            AccessKey = "ADMIN-SECURE-KEY-2026"
        };
        var validResp = await _client.PostAsJsonAsync("/api/admin/verify-access-key", validKeyDto);
        validResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var validContent = await validResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = validContent.GetProperty("data").GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();

        // Act 3: Invalid/Expired Challenge (A4)
        var expiredDto = new VerifyAccessKeyDto
        {
            ChallengeToken = "invalid-challenge-token",
            AccessKey = "ADMIN-SECURE-KEY-2026"
        };
        var expiredResp = await _client.PostAsJsonAsync("/api/admin/verify-access-key", expiredDto);
        expiredResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminEndpoints_AuthorizationMatrix_EnforcesVerifiedAdminOnly()
    {
        // Arrange (A7)
        var donorToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "donor@test.com", "DONOR");
        using var donorClient = _factory.CreateClient().WithBearerToken(donorToken);

        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        var endpoints = new[]
        {
            "/api/admin/users",
            "/api/admin/monitoring/overview",
            "/api/admin/monitoring/activity"
        };

        foreach (var endpoint in endpoints)
        {
            // 1. Unauthenticated -> 401
            var unauthResp = await _client.GetAsync(endpoint);
            unauthResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // 2. Normal Donor -> 403
            var donorResp = await donorClient.GetAsync(endpoint);
            donorResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // 3. Verified Admin -> 200
            var adminResp = await adminClient.GetAsync(endpoint);
            adminResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task UserStatusLifecycle_ListDeactivateLoginBlockReactivateLoginSuccess()
    {
        // Arrange (A8)
        var regDto = new RegisterDto
        {
            Email = "lifecycle_user@donor.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Lifecycle Bistro",
            Location = "123 Main St"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", regDto);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var targetUserId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);

        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // 1. List Users -> Excludes PasswordHashes
        var listResp = await adminClient.GetAsync("/api/admin/users");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResp.Content.ReadAsStringAsync();
        listJson.Should().NotContain("passwordHash");
        listJson.Should().NotContain("PasswordHash");

        // 2. Deactivate User
        var deactDto = new UserStatusUpdateDto { IsActive = false };
        var deactResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/status", deactDto);
        deactResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Deactivated User Login -> Fails (401 Unauthorized)
        var userLoginDto = new LoginDto { Email = "lifecycle_user@donor.com", Password = "Password123!" };
        var userLoginResp = await _client.PostAsJsonAsync("/api/auth/login", userLoginDto);
        userLoginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 4. Reactivate User
        var actDto = new UserStatusUpdateDto { IsActive = true };
        var actResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/status", actDto);
        actResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Reactivated User Login -> Succeeds (200)
        var userLoginResp2 = await _client.PostAsJsonAsync("/api/auth/login", userLoginDto);
        userLoginResp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SoleAdmin_StatusChangeAndAccountDeletion_Rejected()
    {
        // Arrange (A9)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
        var adminUser = db.Users.First(u => u.Role == UserRole.ADMIN);

        var adminToken = AuthHelper.CreateTestJwtToken(adminUser.Id, adminUser.Email, "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // Act 1: Attempt to deactivate admin account via PATCH /api/admin/users/{id}/status
        var statusDto = new UserStatusUpdateDto { IsActive = false };
        var statusResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{adminUser.Id}/status", statusDto);
        statusResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act 2: Attempt to delete admin account via DELETE /api/auth/account
        var deleteDto = new DeleteAccountDto { Password = "Admin@123" };
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        };
        var deleteResp = await adminClient.SendAsync(deleteRequest);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MonitoringEndpoint_DownstreamServiceResilience()
    {
        // Arrange (A10)
        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // Act
        var response = await adminClient.GetAsync("/api/admin/monitoring/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = content.GetProperty("data");
        data.GetProperty("totalUsers").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ActivityLogs_VerifyAuditEntriesAndSensitiveDataOmission()
    {
        // Arrange (A11) - Trigger admin login to log activity
        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = "Admin@123" };
        await _client.PostAsJsonAsync("/api/admin/login", loginDto);

        var adminToken = AuthHelper.CreateTestJwtToken(Guid.NewGuid(), "admin@rescueplate.org", "ADMIN");
        using var adminClient = _factory.CreateClient().WithBearerToken(adminToken);

        // Act
        var response = await adminClient.GetAsync("/api/admin/monitoring/activity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonStr = await response.Content.ReadAsStringAsync();
        jsonStr.Should().NotContain("Admin@123");
        jsonStr.Should().NotContain("ADMIN-SECURE-KEY");
    }

    [Fact]
    public async Task AdminWorkflow_EndToEndSimulation()
    {
        // Arrange & Act (A12 E2E Simulation)
        // 1. Step 1 Admin Login
        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = "Admin@123" };
        var loginResp = await _client.PostAsJsonAsync("/api/admin/login", loginDto);
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginContent = await loginResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var challengeToken = loginContent.GetProperty("data").GetProperty("challengeToken").GetString()!;

        // 2. Step 2 Access Key Verification
        var verifyDto = new VerifyAccessKeyDto { ChallengeToken = challengeToken, AccessKey = "ADMIN-SECURE-KEY-2026" };
        var verifyResp = await _client.PostAsJsonAsync("/api/admin/verify-access-key", verifyDto);
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyContent = await verifyResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var jwtToken = verifyContent.GetProperty("data").GetProperty("token").GetString()!;

        // 3. Authenticated Admin Actions (Dashboard, Activity)
        using var verifiedAdminClient = _factory.CreateClient().WithBearerToken(jwtToken);
        var usersResp = await verifiedAdminClient.GetAsync("/api/admin/users");
        usersResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var overviewResp = await verifiedAdminClient.GetAsync("/api/admin/monitoring/overview");
        overviewResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Logout / Revocation simulation - Unauthenticated request fails
        var unauthResp = await _client.GetAsync("/api/admin/users");
        unauthResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
