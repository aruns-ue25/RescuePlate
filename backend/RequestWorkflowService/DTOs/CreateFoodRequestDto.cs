using System.ComponentModel.DataAnnotations;

namespace RequestWorkflowService.DTOs;

public class CreateFoodRequestDto
{
    [Required]
    public int DonationId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Requested quantity must be at least 1.")]
    public int RequestedQuantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
