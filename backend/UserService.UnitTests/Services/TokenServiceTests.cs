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
}
