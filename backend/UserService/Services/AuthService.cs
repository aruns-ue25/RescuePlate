using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto);
    Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto);
    Task<(bool Success, string Message)> DeleteAccountAsync(Guid userId, string password);
}

public class AuthService : IAuthService
{
    private readonly RescuePlateDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(RescuePlateDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
    {
        // 1. Unique Email Validation (SRS Acceptance Criteria)
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            return (false, "An account with this email address already exists. Please sign in or use a different email.", null);
        }

        // 2. Secure Password Storage (SRS NFR3)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        // 3. Create User Entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        // 4. Create Role-specific Profile (Sprint 1 Stories)
        string businessName = dto.BusinessOrOrgName.Trim();
        string contactName = string.IsNullOrWhiteSpace(dto.ContactName) ? businessName : dto.ContactName.Trim();

        if (dto.Role == UserRole.DONOR)
        {
            var donorProfile = new DonorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BusinessName = businessName,
                ContactPerson = contactName,
                DonorType = string.IsNullOrWhiteSpace(dto.DonorType) ? "Restaurant" : dto.DonorType,
                Address = dto.Location.Trim(),
                Phone = dto.Phone?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.DonorProfiles.Add(donorProfile);
        }
        else if (dto.Role == UserRole.ORGANIZATION)
        {
            var acceptedTypes = dto.AcceptedFoodTypes != null && dto.AcceptedFoodTypes.Count > 0
                ? string.Join(";", dto.AcceptedFoodTypes)
                : "Cooked Meals;Bakery";

            var orgProfile = new OrganizationProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationName = businessName,
                ContactPerson = contactName,
                Address = dto.Location.Trim(),
                Phone = dto.Phone?.Trim() ?? string.Empty,
                AcceptedFoodCategories = acceptedTypes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.OrganizationProfiles.Add(orgProfile);
        }

        await _db.SaveChangesAsync();

        // 5. Generate JWT Token
        var token = _tokenService.GenerateToken(user, businessName, contactName);

        var responseData = new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            Name = contactName,
            BusinessName = businessName,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        return (true, "Registration successful!", responseData);
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.OrganizationProfile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            return (false, "Invalid email address or password.", null);
        }

        // Verify password hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return (false, "Invalid email address or password.", null);
        }

        // Check account activation (SRS FR4 & NFR4)
        if (!user.IsActive)
        {
            return (false, "Your account has been deactivated by the system administrator.", null);
        }

        string businessName = user.Role switch
        {
            UserRole.DONOR => user.DonorProfile?.BusinessName ?? "Donor Business",
            UserRole.ORGANIZATION => user.OrganizationProfile?.OrganizationName ?? "Charity Organization",
            _ => "RescuePlate Admin"
        };

        string contactName = user.Role switch
        {
            UserRole.DONOR => user.DonorProfile?.ContactPerson ?? businessName,
            UserRole.ORGANIZATION => user.OrganizationProfile?.ContactPerson ?? businessName,
            _ => "Administrator"
        };

        var token = _tokenService.GenerateToken(user, businessName, contactName);

        var responseData = new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            Name = contactName,
            BusinessName = businessName,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        return (true, "Signed in successfully!", responseData);
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(Guid userId, string password)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User account not found.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return (false, "Incorrect password. Account deletion aborted.");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return (true, "Account has been permanently deleted.");
    }
}
