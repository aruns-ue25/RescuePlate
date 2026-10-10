using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;

namespace UserService.Services;

public interface ITokenService
{
    string GenerateToken(User user, string businessOrOrgName, string contactName);
    string GeneratePreAuthChallengeToken(User user, int expiryMinutes = 5);
    string GenerateVerifiedAdminToken(User user, string businessOrOrgName, string contactName);
    ClaimsPrincipal? ValidateChallengeToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user, string businessOrOrgName, string contactName)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
        var issuer = _config["Jwt:Issuer"] ?? "RescuePlate.UserService";
        var audience = _config["Jwt:Audience"] ?? "RescuePlate.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("BusinessName", businessOrOrgName),
            new("ContactName", contactName),
            new("IsActive", user.IsActive.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GeneratePreAuthChallengeToken(User user, int expiryMinutes = 5)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
        var issuer = _config["Jwt:Issuer"] ?? "RescuePlate.UserService";
        var audience = _config["Jwt:Audience"] ?? "RescuePlate.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("purpose", "admin_access_key_challenge")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateVerifiedAdminToken(User user, string businessOrOrgName, string contactName)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
        var issuer = _config["Jwt:Issuer"] ?? "RescuePlate.UserService";
        var audience = _config["Jwt:Audience"] ?? "RescuePlate.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, UserRole.ADMIN.ToString()),
            new("admin_verified", "true"),
            new("BusinessName", string.IsNullOrWhiteSpace(businessOrOrgName) ? "RescuePlate System Admin" : businessOrOrgName),
            new("ContactName", string.IsNullOrWhiteSpace(contactName) ? "System Administrator" : contactName),
            new("IsActive", user.IsActive.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateChallengeToken(string token)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood";
        var issuer = _config["Jwt:Issuer"] ?? "RescuePlate.UserService";
        var audience = _config["Jwt:Audience"] ?? "RescuePlate.Client";

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != "admin_access_key_challenge")
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
