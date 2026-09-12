using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class CreateDonationDto
{
    [Required(ErrorMessage = "Food title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Food title must be between 3 and 200 characters.")]
    public string FoodTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Food category is required.")]
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    public string Category { get; set; } = "Cooked Meals";

    [Required(ErrorMessage = "Total quantity is required.")]
    [Range(1, 100000, ErrorMessage = "Quantity must be a positive number greater than 0.")]
    public int TotalQuantity { get; set; }

    [Required(ErrorMessage = "Unit of measurement is required.")]
    [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters.")]
    public string Unit { get; set; } = "portions";

    /// <summary>
    /// Relative hours until expiry (e.g. 2.5 hours, 4 hours, 24 hours).
    /// </summary>
    [Range(0.25, 168.0, ErrorMessage = "Availability window must be between 15 minutes (0.25h) and 7 days (168h).")]
    public double? ExpiryHours { get; set; }

    /// <summary>
    /// Explicit future timestamp for expiry (optional alternative to ExpiryHours).
    /// </summary>
    public DateTime? ExpiryTime { get; set; }

    [Required(ErrorMessage = "Handover collection mode is required.")]
    [StringLength(50, ErrorMessage = "Collection mode cannot exceed 50 characters.")]
    public string CollectionMode { get; set; } = "Organization Pickup";

    [Required(ErrorMessage = "Pickup or delivery location address is required.")]
    [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters.")]
    public string Location { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [StringLength(255, ErrorMessage = "Dietary tags cannot exceed 255 characters.")]
    public string? DietaryTags { get; set; }
}
