using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IAdminService
{
    Task<(bool Success, string Message, AdminPreAuthResponseDto? Data)> AdminLoginAsync(LoginDto dto, string clientIp);
    Task<(bool Success, string Message, AuthResponseDto? Data)> VerifyAccessKeyAsync(VerifyAccessKeyDto dto, string clientIp);
    Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, bool isActive, Guid adminUserId, string adminEmail, string clientIp);
    Task<(bool Success, string Message, MonitoringOverviewDto? Data)> GetMonitoringOverviewAsync(string clientIp);
    Task<(bool Success, string Message, List<ActivityLogDto> Data)> GetActivityLogsAsync(int page = 1, int pageSize = 50);
}

public class AdminService : IAdminService
{
    private readonly RescuePlateDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        RescuePlateDbContext db,
        ITokenService tokenService,
        IConfiguration config,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AdminPreAuthResponseDto? Data)> AdminLoginAsync(LoginDto dto, string clientIp)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            await LogActivityAsync("ADMIN_LOGIN_FAILED", null, normalizedEmail, null, clientIp, "Attempted login with non-existent email.");
            return (false, "Invalid administrator credentials.", null);
        }

        // Check Password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            await LogActivityAsync("ADMIN_LOGIN_FAILED", user.Id, user.Email, null, clientIp, "Invalid password provided.");
            return (false, "Invalid administrator credentials.", null);
        }

        // Check Role Claim (Scenario 4)
        if (user.Role != UserRole.ADMIN)
        {
            await LogActivityAsync("ADMIN_LOGIN_FAILED", user.Id, user.Email, null, clientIp, $"User with role '{user.Role}' attempted admin login.");
            return (false, "Access denied: Account is not an administrator.", null);
        }

        // Check Active Account Status (Scenario 3)
        if (!user.IsActive)
        {
            await LogActivityAsync("ADMIN_LOGIN_FAILED", user.Id, user.Email, null, clientIp, "Inactive admin account attempted login.");
            return (false, "Administrator account is inactive.", null);
        }

        // Issue short-lived Pre-Auth Challenge Token (5 min expiry)
        int challengeExpiryMinutes = int.TryParse(_config["AdminSettings:ChallengeTokenExpiryMinutes"], out var exp) ? exp : 5;
        var challengeToken = _tokenService.GeneratePreAuthChallengeToken(user, challengeExpiryMinutes);

        await LogActivityAsync("ADMIN_LOGIN_SUCCESS", user.Id, user.Email, null, clientIp, "Step 1 admin credential authentication succeeded.");

        var responseData = new AdminPreAuthResponseDto
        {
            Email = user.Email,
            ChallengeToken = challengeToken,
            RequiresAccessKey = true,
            ExpiresInSeconds = challengeExpiryMinutes * 60
        };

        return (true, "Administrator credentials verified. Access Key required.", responseData);
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Data)> VerifyAccessKeyAsync(VerifyAccessKeyDto dto, string clientIp)
    {
        // 1. Validate Challenge Token
        var principal = _tokenService.ValidateChallengeToken(dto.ChallengeToken);
        if (principal == null)
        {
            return (false, "Invalid or expired pre-authentication challenge. Please sign in again.", null);
        }

        var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return (false, "Invalid challenge payload.", null);
        }

        var user = await _db.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.OrganizationProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Role != UserRole.ADMIN)
        {
            await LogActivityAsync("ACCESS_KEY_FAILED", userId, principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown", null, clientIp, "Non-admin attempted access key verification.");
            return (false, "Administrator access verification denied.", null);
        }

        if (!user.IsActive)
        {
            await LogActivityAsync("ACCESS_KEY_FAILED", user.Id, user.Email, null, clientIp, "Inactive admin account attempted access key verification.");
            return (false, "Administrator account is inactive.", null);
        }

        // 2. Global Rate-Limiting Across Account & IP
        string emailCacheKey = $"RateLimit_AdminAccessKey_{user.Email.ToLower()}";
        string ipCacheKey = $"RateLimit_AdminAccessKey_IP_{clientIp}";

        int failedEmailCount = _cache.Get<int?>(emailCacheKey) ?? 0;
        int failedIpCount = _cache.Get<int?>(ipCacheKey) ?? 0;

        int maxAllowedAttempts = int.TryParse(_config["AdminSettings:MaxFailedAttempts"], out var maxAtt) ? maxAtt : 5;
        int lockoutMinutes = int.TryParse(_config["AdminSettings:LockoutMinutes"], out var lockMin) ? lockMin : 15;

        if (failedEmailCount >= maxAllowedAttempts || failedIpCount >= maxAllowedAttempts)
        {
            await LogActivityAsync("ACCESS_KEY_LOCKOUT", user.Id, user.Email, null, clientIp, $"Access Key verification locked out due to {maxAllowedAttempts} failed attempts.");
            return (false, $"Too many failed access key attempts. Account locked out for {lockoutMinutes} minutes.", null);
        }

        // 3. Verify Access Key
        string expectedAccessKey = _config["AdminSettings:AccessKey"] ?? "ADMIN-SECURE-KEY-2026";
        if (dto.AccessKey != expectedAccessKey)
        {
            // Increment failed attempt counters
            _cache.Set(emailCacheKey, failedEmailCount + 1, TimeSpan.FromMinutes(lockoutMinutes));
            _cache.Set(ipCacheKey, failedIpCount + 1, TimeSpan.FromMinutes(lockoutMinutes));

            await LogActivityAsync("ACCESS_KEY_FAILED", user.Id, user.Email, null, clientIp, $"Incorrect access key attempt ({failedEmailCount + 1}/{maxAllowedAttempts}).");
            return (false, "Invalid Administrator Access Key.", null);
        }

        // 4. Verification Successful -> Clear lockout counter & log success
        _cache.Remove(emailCacheKey);
        _cache.Remove(ipCacheKey);

        await LogActivityAsync("ACCESS_KEY_SUCCESS", user.Id, user.Email, null, clientIp, "Administrator access key verified successfully.");

        // Generate final Verified Admin Token
        var token = _tokenService.GenerateVerifiedAdminToken(user, "RescuePlate System Admin", "System Administrator");

        var responseData = new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            Name = "System Administrator",
            BusinessName = "RescuePlate System Admin",
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        return (true, "Administrator access verified successfully!", responseData);
    }

    public async Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, bool isActive, Guid adminUserId, string adminEmail, string clientIp)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        // Prevent admin self-deactivation
        if (user.Id == adminUserId && !isActive)
        {
            return (false, "You cannot deactivate your own administrator account.");
        }

        // Prevent deactivating the only active admin account
        if (user.Role == UserRole.ADMIN && !isActive)
        {
            var activeAdminCount = await _db.Users.CountAsync(u => u.Role == UserRole.ADMIN && u.IsActive);
            if (activeAdminCount <= 1)
            {
                return (false, "Cannot deactivate the only active system administrator account.");
            }
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        string action = isActive ? "USER_REACTIVATED" : "USER_DEACTIVATED";
        string details = $"User account '{user.Email}' status updated to {(isActive ? "ACTIVE" : "INACTIVE")}.";
        await LogActivityAsync(action, adminUserId, adminEmail, user.Id, clientIp, details);

        return (true, $"User account has been {(isActive ? "activated" : "deactivated")}.");
    }

    public async Task<(bool Success, string Message, MonitoringOverviewDto? Data)> GetMonitoringOverviewAsync(string clientIp)
    {
        var totalUsers = await _db.Users.CountAsync();
        var activeDonors = await _db.Users.CountAsync(u => u.Role == UserRole.DONOR && u.IsActive);
        var activeOrgs = await _db.Users.CountAsync(u => u.Role == UserRole.ORGANIZATION && u.IsActive);

        var recentLogs = await _db.AdminActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(15)
            .Select(l => new ActivityLogDto
            {
                Id = l.Id,
                Action = l.Action,
                PerformedByEmail = l.PerformedByEmail,
                ClientIp = l.ClientIp,
                Details = l.Details,
                Timestamp = l.Timestamp
            })
            .ToListAsync();

        string? authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        object? donationData = null;
        try
        {
            var donationClient = _httpClientFactory.CreateClient("DonationService");
            if (!string.IsNullOrEmpty(authHeader))
            {
                donationClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
            }
            var response = await donationClient.GetAsync("/api/donations/admin/summary");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                donationData = json;
            }
            else
            {
                donationData = new { status = "UNAVAILABLE", statusCode = (int)response.StatusCode };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch donation statistics from DonationService");
            donationData = new { status = "UNAVAILABLE", error = ex.Message };
        }

        object? requestData = null;
        try
        {
            var requestClient = _httpClientFactory.CreateClient("RequestService");
            if (!string.IsNullOrEmpty(authHeader))
            {
                requestClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
            }
            var response = await requestClient.GetAsync("/api/requests/admin/summary");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                requestData = json;
            }
            else
            {
                requestData = new { status = "UNAVAILABLE", statusCode = (int)response.StatusCode };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch request statistics from RequestWorkflowService");
            requestData = new { status = "UNAVAILABLE", error = ex.Message };
        }

        var overview = new MonitoringOverviewDto
        {
            TotalUsers = totalUsers,
            ActiveDonors = activeDonors,
            ActiveOrganizations = activeOrgs,
            DonationSummary = donationData,
            RequestSummary = requestData,
            RecentActivities = recentLogs
        };

        return (true, "Platform monitoring overview retrieved successfully.", overview);
    }

    public async Task<(bool Success, string Message, List<ActivityLogDto> Data)> GetActivityLogsAsync(int page = 1, int pageSize = 50)
    {
        var logs = await _db.AdminActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ActivityLogDto
            {
                Id = l.Id,
                Action = l.Action,
                PerformedByEmail = l.PerformedByEmail,
                ClientIp = l.ClientIp,
                Details = l.Details,
                Timestamp = l.Timestamp
            })
            .ToListAsync();

        return (true, "Activity logs retrieved successfully.", logs);
    }

    private async Task LogActivityAsync(string action, Guid? performedByUserId, string performedByEmail, Guid? targetUserId, string clientIp, string details)
    {
        try
        {
            var log = new AdminActivityLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                PerformedByUserId = performedByUserId,
                PerformedByEmail = performedByEmail,
                TargetUserId = targetUserId,
                ClientIp = clientIp,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _db.AdminActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write AdminActivityLog to database.");
        }
    }
}
