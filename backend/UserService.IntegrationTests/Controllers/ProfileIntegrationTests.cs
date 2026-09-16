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

    [Fact]
    public async Task UploadProfilePicture_ValidPng_Returns200OkAndUpdatesProfile()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "pic_upload_png@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Pic Bakery",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        var magicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(magicBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        // Act 1: Upload
        var uploadResponse = await authClient.PostAsync("/api/profile/me/picture", content);

        // Assert 1
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        uploadResult.GetProperty("success").GetBoolean().Should().BeTrue();
        var url = uploadResult.GetProperty("data").GetProperty("profilePictureUrl").GetString();
        url.Should().NotBeNullOrWhiteSpace();
        url.Should().Contain(".png");

        // Act 2: Verify Profile State
        var profileResponse = await authClient.GetAsync("/api/profile/me");
        var profileData = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        profileData.GetProperty("data").GetProperty("profilePictureUrl").GetString().Should().Be(url);
    }

    [Fact]
    public async Task UploadProfilePicture_ValidJpeg_Returns200OkAndUpdatesProfile()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "pic_upload_jpg@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Pic Bakery Jpg",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        var magicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(magicBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "avatar.jpg");

        // Act
        var uploadResponse = await authClient.PostAsync("/api/profile/me/picture", content);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        uploadResult.GetProperty("success").GetBoolean().Should().BeTrue();
        uploadResult.GetProperty("data").GetProperty("profilePictureUrl").GetString().Should().Contain(".jpg");
    }

    [Fact]
    public async Task UploadProfilePicture_InvalidFileType_Returns400BadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "pic_invalid@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Invalid Pic Bakery",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        var magicBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(magicBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "doc.pdf");

        // Act
        var uploadResponse = await authClient.PostAsync("/api/profile/me/picture", content);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePicture_OversizedFile_Returns400BadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "pic_oversized@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Oversize Bakery",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        // Max limit is 5MB. Let's send 5.1MB.
        var largeContent = new byte[5 * 1024 * 1024 + 100 * 1024];
        // Note: For integration tests, sending 5MB in-memory is acceptable, but if it fails we can adjust.
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(largeContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        // Act
        var uploadResponse = await authClient.PostAsync("/api/profile/me/picture", content);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        result.GetProperty("message").GetString().Should().Contain("size exceeds the 5MB limit");
    }

    [Fact]
    public async Task RemoveProfilePicture_ExistingPicture_Returns200OkAndUpdatesProfile()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "pic_remove_int@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Remove Pic Bakery",
            Location = "Local"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = regData.GetProperty("data").GetProperty("token").GetString()!;

        using var authClient = _factory.CreateClient().WithBearerToken(token);

        // Manually set a picture in DB first
        var userId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var user = db.Users.Find(userId);
            user!.ProfilePictureUrl = "/uploads/test.png";
            await db.SaveChangesAsync();
        }

        // Act 1: Remove Picture
        var removeResponse = await authClient.DeleteAsync("/api/profile/me/picture");

        // Assert 1
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Verify Profile State
        var profileResponse = await authClient.GetAsync("/api/profile/me");
        var profileData = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        profileData.GetProperty("data").GetProperty("profilePictureUrl").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetPublicProfile_OrganizationUser_ReturnsSafeFieldsWithCategories()
    {
        // Arrange (RP-21)
        var registerDto = new RegisterDto
        {
            Email = "public_org@charity.org",
            Password = "Password123!",
            Role = UserRole.ORGANIZATION,
            BusinessOrOrgName = "Public Shelter",
            Location = "Public St",
            AcceptedFoodTypes = new List<string> { "Cooked Meals", "Dairy & Chilled" }
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var userId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);

        // Act (Unauthenticated)
        var response = await _client.GetAsync($"/api/profile/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        
        var data = content.GetProperty("data");
        data.GetProperty("businessOrOrgName").GetString().Should().Be("Public Shelter");
        data.GetProperty("role").GetString().Should().Be("ORGANIZATION");
        
        var categories = data.GetProperty("acceptedFoodCategories").EnumerateArray().Select(e => e.GetString()).ToList();
        categories.Should().Contain("Cooked Meals");
        categories.Should().Contain("Dairy & Chilled");

        // Verify sensitive fields are excluded
        data.TryGetProperty("email", out _).Should().BeFalse();
        data.TryGetProperty("passwordHash", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicProfile_DeactivatedAccount_Returns200OkAsPerImplementation()
    {
        // Arrange (RP-21)
        var registerDto = new RegisterDto
        {
            Email = "public_deactivated@bakery.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Banned Bakery",
            Location = "Banned St"
        };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var regData = await regResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var userId = Guid.Parse(regData.GetProperty("data").GetProperty("userId").GetString()!);

        // Deactivate user in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var user = db.Users.Find(userId);
            user!.IsActive = false;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/profile/{userId}");

        // Assert
        // Current implementation does not check IsActive for public profiles, so it returns 200 OK.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        content.GetProperty("data").GetProperty("businessOrOrgName").GetString().Should().Be("Banned Bakery");
    }
}
