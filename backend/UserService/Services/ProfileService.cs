using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId);
    Task<(bool Success, string Message, UserProfileDto? Data)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task<(bool Success, string Message, string? ProfilePictureUrl)> UploadProfilePictureAsync(Guid userId, IFormFile? file, string webRootPath);
    Task<(bool Success, string Message)> RemoveProfilePictureAsync(Guid userId, string webRootPath);
}

public class ProfileService : IProfileService
{
    private readonly RescuePlateDbContext _db;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

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
            MemberSince = user.CreatedAt,
            ProfilePictureUrl = user.ProfilePictureUrl
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

    public async Task<(bool Success, string Message, string? ProfilePictureUrl)> UploadProfilePictureAsync(
        Guid userId, 
        IFormFile? file, 
        string webRootPath)
    {
        // 1. Scenario 5: Validate file presence & size
        if (file == null || file.Length == 0)
        {
            return (false, "Please select an image file to upload.", null);
        }

        const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxSizeBytes)
        {
            return (false, "Image file size exceeds the 5MB limit. Please choose a smaller image.", null);
        }

        // 2. Scenario 4: Validate extension & MIME type
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (false, "Unsupported image format. Allowed formats are JPG, PNG, and WEBP.", null);
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            return (false, "Invalid image MIME type. Allowed formats are JPG, PNG, and WEBP.", null);
        }

        // 3. Scenario 5: Validate image binary magic bytes (prevent disguised malicious files)
        using (var stream = file.OpenReadStream())
        {
            if (!IsValidImageHeader(stream, extension))
            {
                return (false, "The uploaded file is not a valid or supported image.", null);
            }
        }

        // 4. Scenario 6: Find user in database
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User account not found.", null);
        }

        // 5. Scenario 2: If user already has a profile picture, delete the old physical file
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            DeletePhysicalFile(webRootPath, user.ProfilePictureUrl);
        }

        // 6. Save new physical file to wwwroot/uploads/profiles/
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"profile_{userId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(outputStream);
        }

        // 7. Update User ProfilePictureUrl in PostgreSQL
        var relativeUrl = $"/uploads/profiles/{uniqueFileName}";
        user.ProfilePictureUrl = relativeUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, "Profile picture updated successfully!", relativeUrl);
    }

    public async Task<(bool Success, string Message)> RemoveProfilePictureAsync(Guid userId, string webRootPath)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User account not found.");
        }

        // Scenario 3: Delete physical file if exists and reset DB column
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            DeletePhysicalFile(webRootPath, user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return (true, "Profile picture removed successfully.");
    }

    private static bool IsValidImageHeader(Stream stream, string extension)
    {
        try
        {
            stream.Position = 0;
            var header = new byte[12];
            var bytesRead = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            if (bytesRead < 4) return false;

            var ext = extension.ToLowerInvariant();

            // JPEG magic bytes: FF D8 FF
            if (ext is ".jpg" or ".jpeg")
            {
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            }

            // PNG magic bytes: 89 50 4E 47 (0x89, 'P', 'N', 'G')
            if (ext is ".png")
            {
                return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            }

            // WEBP magic bytes: RIFF (bytes 0..3) and WEBP (bytes 8..11)
            if (ext is ".webp")
            {
                return header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                       bytesRead >= 12 &&
                       header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void DeletePhysicalFile(string webRootPath, string relativeUrl)
    {
        try
        {
            var cleanPath = relativeUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRootPath, cleanPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch
        {
            // Ignore file deletion errors to prevent blocking DB operations
        }
    }
}
