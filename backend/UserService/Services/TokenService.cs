using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;

namespace UserService.Services;

public interface ITokenService
{
    string GenerateToken(User user, string businessOrOrgName, string contactName);
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
        var secretKey = _config["Jwt:SecretKey"] ?? "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1";
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
}
