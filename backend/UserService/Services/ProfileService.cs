using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId);
    Task<(bool Success, string Message, UserProfileDto? Data)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}

public class ProfileService : IProfileService
{
    private readonly RescuePlateDbContext _db;

    public ProfileService(RescuePlateDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.OrganizationProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var profile = new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            MemberSince = user.CreatedAt
        };

        if (user.Role == UserRole.DONOR && user.DonorProfile != null)
        {
            profile.BusinessOrOrgName = user.DonorProfile.BusinessName;
            profile.ContactName = user.DonorProfile.ContactPerson;
            profile.Phone = user.DonorProfile.Phone;
            profile.Address = user.DonorProfile.Address;
            profile.BioOrDescription = user.DonorProfile.Bio;
            profile.DonorType = user.DonorProfile.DonorType;
        }
        else if (user.Role == UserRole.ORGANIZATION && user.OrganizationProfile != null)
        {
            profile.BusinessOrOrgName = user.OrganizationProfile.OrganizationName;
            profile.ContactName = user.OrganizationProfile.ContactPerson;
            profile.Phone = user.OrganizationProfile.Phone;
            profile.Address = user.OrganizationProfile.Address;
            profile.BioOrDescription = user.OrganizationProfile.Description;
            profile.RegistrationNumber = user.OrganizationProfile.RegistrationNumber;
            profile.AcceptedFoodCategories = user.OrganizationProfile.AcceptedFoodCategories
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return profile;
    }

    public async Task<(bool Success, string Message, UserProfileDto? Data)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _db.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.OrganizationProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (false, "User not found.", null);
        }

        if (user.Role == UserRole.DONOR)
        {
            if (user.DonorProfile == null)
            {
                user.DonorProfile = new DonorProfile { UserId = user.Id };
                _db.DonorProfiles.Add(user.DonorProfile);
            }

            if (!string.IsNullOrWhiteSpace(dto.BusinessOrOrgName)) user.DonorProfile.BusinessName = dto.BusinessOrOrgName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ContactName)) user.DonorProfile.ContactPerson = dto.ContactName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.DonorProfile.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Address)) user.DonorProfile.Address = dto.Address.Trim();
            if (dto.BioOrDescription != null) user.DonorProfile.Bio = dto.BioOrDescription.Trim();
            if (!string.IsNullOrWhiteSpace(dto.DonorType)) user.DonorProfile.DonorType = dto.DonorType.Trim();
            user.DonorProfile.UpdatedAt = DateTime.UtcNow;
        }
        else if (user.Role == UserRole.ORGANIZATION)
        {
            if (user.OrganizationProfile == null)
            {
                user.OrganizationProfile = new OrganizationProfile { UserId = user.Id };
                _db.OrganizationProfiles.Add(user.OrganizationProfile);
            }

            if (!string.IsNullOrWhiteSpace(dto.BusinessOrOrgName)) user.OrganizationProfile.OrganizationName = dto.BusinessOrOrgName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ContactName)) user.OrganizationProfile.ContactPerson = dto.ContactName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.OrganizationProfile.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Address)) user.OrganizationProfile.Address = dto.Address.Trim();
            if (dto.BioOrDescription != null) user.OrganizationProfile.Description = dto.BioOrDescription.Trim();
            if (dto.AcceptedFoodCategories != null)
            {
                user.OrganizationProfile.AcceptedFoodCategories = string.Join(";", dto.AcceptedFoodCategories);
            }
            user.OrganizationProfile.UpdatedAt = DateTime.UtcNow;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var updatedProfile = await GetProfileByUserIdAsync(userId);
        return (true, "Profile updated successfully!", updatedProfile);
    }
}
