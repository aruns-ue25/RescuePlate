using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public class AdminPreAuthResponseDto
{
    public string Email { get; set; } = string.Empty;
    public string ChallengeToken { get; set; } = string.Empty;
    public bool RequiresAccessKey { get; set; } = true;
    public int ExpiresInSeconds { get; set; } = 300;
}

public class VerifyAccessKeyDto
{
    [Required]
    public string ChallengeToken { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;
}

public class AdminUserSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class UserStatusUpdateDto
{
    [Required]
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedByEmail { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class MonitoringOverviewDto
{
    public int TotalUsers { get; set; }
    public int ActiveDonors { get; set; }
    public int ActiveOrganizations { get; set; }
    public object? DonationSummary { get; set; }
    public object? RequestSummary { get; set; }
    public List<ActivityLogDto> RecentActivities { get; set; } = new();
}
