using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DonationService.IntegrationTests.Helpers;

public static class AuthHelper
{
    public const string DefaultSecretKey = "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
    public const string DefaultIssuer = "RescuePlate.UserService";
    public const string DefaultAudience = "RescuePlate.Client";

    public static string GenerateJwtToken(
        string userId,
        string role = "DONOR",
        string businessName = "Test Donor Business",
        string email = "donor@test.com",
        string? secretKey = null,
        string? issuer = null,
        string? audience = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? DefaultSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new("id", userId),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Email, email),
            new("email", email),
            new("businessName", businessName),
            new("BusinessName", businessName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer ?? DefaultIssuer,
            audience: audience ?? DefaultAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
