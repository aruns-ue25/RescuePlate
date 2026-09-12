using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationService.Models;

[Table("Donations")]
public class Donation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string DonorId { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string DonorEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FoodTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "Cooked Meals"; // Cooked Meals, Bakery, Fresh Produce, Dairy & Chilled, Packaged Dry

    [Required]
    public int TotalQuantity { get; set; }

    public int ClaimedQuantity { get; set; } = 0;

    public int RemainingQuantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = "portions"; // portions, meal boxes, kg, chilled units

    [Required]
    public DateTime ExpiryTime { get; set; }

    [Required]
    [MaxLength(50)]
    public string CollectionMode { get; set; } = "Organization Pickup"; // Organization Pickup, Donor Delivery

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available"; // Available, PartiallyClaimed, FullyClaimed, Completed, Expired, Cancelled

    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(255)]
    public string DietaryTags { get; set; } = string.Empty; // e.g. Vegetarian, Halal, Dairy-Free

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
