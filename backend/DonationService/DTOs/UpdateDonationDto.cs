using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class UpdateDonationDto
{
    [MaxLength(200, ErrorMessage = "Food item title cannot exceed 200 characters.")]
    public string? FoodTitle { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    public int? TotalQuantity { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public DateTime? ExpiryTime { get; set; }

    [Range(1, 168, ErrorMessage = "Expiry hours must be between 1 and 168.")]
    public int? ExpiryHours { get; set; }

    [MaxLength(50)]
    public string? CollectionMode { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(255)]
    public string? DietaryTags { get; set; }
}
