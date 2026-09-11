namespace DonationService.DTOs;

public class DonationResponseDto
{
    public int Id { get; set; }
    public string DonorId { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string FoodTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int ClaimedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime ExpiryTime { get; set; }
    public string CollectionMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? DietaryTags { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiryTime;
    public double RemainingHours => Math.Max(0, (ExpiryTime - DateTime.UtcNow).TotalHours);
}
