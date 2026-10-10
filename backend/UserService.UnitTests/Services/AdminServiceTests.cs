using System.Net;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<AdminService>> _loggerMock;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
        _tokenServiceMock = new Mock<ITokenService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<AdminService>>();

        var configValues = new Dictionary<string, string?>
        {
            {"AdminSettings:Email", "admin@rescueplate.org"},
            {"AdminSettings:AccessKey", "ADMIN-SECURE-KEY-2026"},
            {"AdminSettings:ChallengeTokenExpiryMinutes", "5"},
            {"AdminSettings:MaxFailedAttempts", "5"},
            {"AdminSettings:LockoutMinutes", "15"},
            {"Jwt:SecretKey", "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood"},
            {"Jwt:Issuer", "RescuePlate.UserService"},
            {"Jwt:Audience", "RescuePlate.Client"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _adminService = new AdminService(
            _dbContext,
            _tokenServiceMock.Object,
            _configuration,
            _memoryCache,
            _httpClientFactoryMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task AdminLoginAsync_ValidAdminCredentials_ReturnsChallengeTokenNoFullJwt()
    {
        // Arrange (A2)
        var password = "Admin@123";
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11),
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        _tokenServiceMock
            .Setup(t => t.GeneratePreAuthChallengeToken(It.Is<User>(u => u.Id == adminUser.Id), 5))
            .Returns("valid-challenge-token-123");

        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = password };

        // Act
        var (success, message, data) = await _adminService.AdminLoginAsync(loginDto, "127.0.0.1");

        // Assert
        success.Should().BeTrue();
        message.Should().Contain("Access Key required");
        data.Should().NotBeNull();
        data!.Email.Should().Be("admin@rescueplate.org");
        data.ChallengeToken.Should().Be("valid-challenge-token-123");
        data.RequiresAccessKey.Should().BeTrue();

        var audit = await _dbContext.AdminActivityLogs.FirstOrDefaultAsync();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("ADMIN_LOGIN_SUCCESS");
    }

    [Fact]
    public async Task AdminLoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange (A3)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Email = "admin@rescueplate.org", Password = "WrongPassword!" };

        // Act
        var (success, message, data) = await _adminService.AdminLoginAsync(loginDto, "127.0.0.1");

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Invalid administrator credentials.");
        data.Should().BeNull();

        var audit = await _dbContext.AdminActivityLogs.FirstOrDefaultAsync();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("ADMIN_LOGIN_FAILED");
    }

    [Fact]
    public async Task AdminLoginAsync_InactiveAdmin_ReturnsRejection()
    {
        // Arrange (A3)
        var password = "AdminPassword123!";
        var inactiveAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive_admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.ADMIN,
            IsActive = false
        };
        _dbContext.Users.Add(inactiveAdmin);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Email = "inactive_admin@rescueplate.org", Password = password };

        // Act
        var (success, message, data) = await _adminService.AdminLoginAsync(loginDto, "127.0.0.1");

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Administrator account is inactive.");
        data.Should().BeNull();
    }

    [Fact]
    public async Task AdminLoginAsync_NonAdminUser_ReturnsAccessDenied()
    {
        // Arrange (A3)
        var password = "DonorPassword123!";
        var donorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor@restaurant.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(donorUser);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Email = "donor@restaurant.com", Password = password };

        // Act
        var (success, message, data) = await _adminService.AdminLoginAsync(loginDto, "127.0.0.1");

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("not an administrator");
        data.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAccessKeyAsync_ValidKey_ReturnsVerifiedAdminJwt()
    {
        // Arrange (A4)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        var challengeToken = "valid-challenge-token-xyz";

        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim("purpose", "admin_access_key_challenge")
            })
        );

        _tokenServiceMock
            .Setup(t => t.ValidateChallengeToken(challengeToken))
            .Returns(claimsPrincipal);

        _tokenServiceMock
            .Setup(t => t.GenerateVerifiedAdminToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("verified-admin-jwt-token-999");

        var dto = new VerifyAccessKeyDto
        {
            ChallengeToken = challengeToken,
            AccessKey = "ADMIN-SECURE-KEY-2026"
        };

        // Act
        var (success, message, data) = await _adminService.VerifyAccessKeyAsync(dto, "127.0.0.1");

        // Assert
        success.Should().BeTrue();
        message.Should().Contain("verified successfully");
        data.Should().NotBeNull();
        data!.Token.Should().Be("verified-admin-jwt-token-999");
        data.Role.Should().Be("ADMIN");

        var audit = await _dbContext.AdminActivityLogs.FirstOrDefaultAsync(a => a.Action == "ACCESS_KEY_SUCCESS");
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyAccessKeyAsync_InvalidKey_FailsAndIncrementsCount()
    {
        // Arrange (A4)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        var challengeToken = "valid-challenge-token-xyz";

        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim("purpose", "admin_access_key_challenge")
            })
        );

        _tokenServiceMock
            .Setup(t => t.ValidateChallengeToken(challengeToken))
            .Returns(claimsPrincipal);

        var dto = new VerifyAccessKeyDto
        {
            ChallengeToken = challengeToken,
            AccessKey = "WRONG-KEY-9999"
        };

        // Act
        var (success, message, data) = await _adminService.VerifyAccessKeyAsync(dto, "127.0.0.1");

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Invalid Administrator Access Key.");
        data.Should().BeNull();

        var audit = await _dbContext.AdminActivityLogs.FirstOrDefaultAsync(a => a.Action == "ACCESS_KEY_FAILED");
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyAccessKeyAsync_ExpiredChallenge_ReturnsFailure()
    {
        // Arrange (A4)
        _tokenServiceMock
            .Setup(t => t.ValidateChallengeToken("expired-challenge"))
            .Returns((ClaimsPrincipal?)null);

        var dto = new VerifyAccessKeyDto
        {
            ChallengeToken = "expired-challenge",
            AccessKey = "ADMIN-SECURE-KEY-2026"
        };

        // Act
        var (success, message, data) = await _adminService.VerifyAccessKeyAsync(dto, "127.0.0.1");

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Invalid or expired pre-authentication challenge");
        data.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAccessKeyAsync_5FailedAttempts_LocksOutAccountAndIP()
    {
        // Arrange (A5)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "target_admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        var challengeToken = "challenge-token-for-bruteforce";

        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim("purpose", "admin_access_key_challenge")
            })
        );

        _tokenServiceMock
            .Setup(t => t.ValidateChallengeToken(challengeToken))
            .Returns(claimsPrincipal);

        var dto = new VerifyAccessKeyDto
        {
            ChallengeToken = challengeToken,
            AccessKey = "WRONG-KEY"
        };

        // Act - Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            await _adminService.VerifyAccessKeyAsync(dto, "10.0.0.5");
        }

        // 6th attempt should return lockout message
        var (success, message, data) = await _adminService.VerifyAccessKeyAsync(dto, "10.0.0.5");

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("locked out for 15 minutes");
        data.Should().BeNull();

        var lockoutAudit = await _dbContext.AdminActivityLogs.FirstOrDefaultAsync(a => a.Action == "ACCESS_KEY_LOCKOUT");
        lockoutAudit.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMonitoringOverviewAsync_DownstreamServiceUnavailable_DegradesGracefully()
    {
        // Arrange (A10)
        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "donor1@test.com",
            Role = UserRole.DONOR,
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Throws(new HttpRequestException("Connection refused"));

        // Act
        var (success, message, data) = await _adminService.GetMonitoringOverviewAsync("127.0.0.1");

        // Assert
        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.TotalUsers.Should().Be(1);
        data.ActiveDonors.Should().Be(1);
        data.DonationSummary.Should().NotBeNull();
        data.RequestSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActivityLogsAsync_SanitizesSensitiveInfo()
    {
        // Arrange (A11)
        _dbContext.AdminActivityLogs.Add(new AdminActivityLog
        {
            Id = Guid.NewGuid(),
            Action = "ADMIN_LOGIN_SUCCESS",
            PerformedByEmail = "admin@rescueplate.org",
            ClientIp = "127.0.0.1",
            Details = "Step 1 admin credential authentication succeeded.",
            Timestamp = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var (success, message, data) = await _adminService.GetActivityLogsAsync(1, 10);

        // Assert
        success.Should().BeTrue();
        data.Should().HaveCount(1);
        var log = data.First();
        log.Details.Should().NotContain("Password");
        log.Details.Should().NotContain("ADMIN-SECURE-KEY");
        log.Details.Should().NotContain("Bearer");
    }
}
