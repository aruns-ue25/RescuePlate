using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration _config;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood"},
            {"Jwt:Issuer", "RescuePlate.UserService"},
            {"Jwt:Audience", "RescuePlate.Client"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_config);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsValidJwtWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor@restaurant.com",
            Role = UserRole.DONOR,
            IsActive = true
        };
        string businessName = "Green Leaf Bistro";
        string contactName = "Chef John";

        // Act
        var tokenString = _tokenService.GenerateToken(user, businessName, contactName);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("RescuePlate.UserService");
        jwtToken.Audiences.Should().Contain("RescuePlate.Client");
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow.AddDays(6));

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "DONOR");
        claims.Should().Contain(c => c.Type == "BusinessName" && c.Value == businessName);
        claims.Should().Contain(c => c.Type == "ContactName" && c.Value == contactName);
        claims.Should().Contain(c => c.Type == "IsActive" && c.Value == "True");
    }

    [Theory]
    [InlineData(UserRole.ORGANIZATION, "Hope Shelter", "Sarah Connor")]
    [InlineData(UserRole.ADMIN, "RescuePlate Admin", "Administrator")]
    public void GenerateToken_DifferentRoles_GeneratesCorrectRoleAndNameClaims(UserRole role, string businessName, string contactName)
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{role.ToString().ToLower()}@rescueplate.org",
            Role = role,
            IsActive = true
        };

        // Act
        var tokenString = _tokenService.GenerateToken(user, businessName, contactName);

        // Assert
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "BusinessName" && c.Value == businessName);
        jwtToken.Claims.Should().Contain(c => c.Type == "ContactName" && c.Value == contactName);
    }

    [Fact]
    public void GeneratePreAuthChallengeToken_ValidAdmin_GeneratesShortLivedTokenWithPurpose()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };

        // Act
        var tokenString = _tokenService.GeneratePreAuthChallengeToken(user, 5);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("RescuePlate.UserService");
        jwtToken.Audiences.Should().Contain("RescuePlate.Client");
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(4));
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(6));

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "ADMIN");
        claims.Should().Contain(c => c.Type == "purpose" && c.Value == "admin_access_key_challenge");
    }

    [Fact]
    public void GenerateVerifiedAdminToken_ValidUser_GeneratesTokenWithAdminVerifiedClaimAndFallbacks()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };

        // Act - with empty business and contact names to verify fallbacks
        var tokenString = _tokenService.GenerateVerifiedAdminToken(user, "", "  ");

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("RescuePlate.UserService");
        jwtToken.Audiences.Should().Contain("RescuePlate.Client");
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow.AddDays(6));

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "ADMIN");
        claims.Should().Contain(c => c.Type == "admin_verified" && c.Value == "true");
        claims.Should().Contain(c => c.Type == "BusinessName" && c.Value == "RescuePlate System Admin");
        claims.Should().Contain(c => c.Type == "ContactName" && c.Value == "System Administrator");
        claims.Should().Contain(c => c.Type == "IsActive" && c.Value == "True");
    }

    [Fact]
    public void GenerateVerifiedAdminToken_CustomNames_UsesCustomNames()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "superadmin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };

        // Act
        var tokenString = _tokenService.GenerateVerifiedAdminToken(user, "Headquarters", "Lead Admin");

        // Assert
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        jwtToken.Claims.Should().Contain(c => c.Type == "BusinessName" && c.Value == "Headquarters");
        jwtToken.Claims.Should().Contain(c => c.Type == "ContactName" && c.Value == "Lead Admin");
    }

    [Fact]
    public void ValidateChallengeToken_ValidChallengeToken_ReturnsPrincipalWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };
        var token = _tokenService.GeneratePreAuthChallengeToken(user, 10);

        // Act
        var principal = _tokenService.ValidateChallengeToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
        principal.FindFirst("purpose")?.Value.Should().Be("admin_access_key_challenge");
    }

    [Fact]
    public void ValidateChallengeToken_RegularTokenWithoutChallengePurpose_ReturnsNull()
    {
        // Arrange: Generate standard token without 'purpose' claim
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };
        var regularToken = _tokenService.GenerateToken(user, "Admin Org", "Admin User");

        // Act
        var principal = _tokenService.ValidateChallengeToken(regularToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateChallengeToken_TamperedOrMalformedToken_ReturnsNull()
    {
        // Arrange
        var malformedToken = "invalid.token.signature";

        // Act
        var principal = _tokenService.ValidateChallengeToken(malformedToken);

        // Assert
        principal.Should().BeNull();
    }
}
