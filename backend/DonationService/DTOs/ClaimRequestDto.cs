using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class ClaimRequestDto
{
    [Range(1, 100000, ErrorMessage = "Requested quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
