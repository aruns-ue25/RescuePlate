using System.ComponentModel.DataAnnotations;

namespace RequestWorkflowService.DTOs;

public class CreateDonorOfferDto
{
    [Required]
    [MaxLength(150)]
    public string FoodType { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Offered quantity must be at least 1.")]
    public int OfferedQuantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
