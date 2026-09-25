using System.ComponentModel.DataAnnotations;

namespace RequestWorkflowService.DTOs;

public class CreateOrgNeedRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = "General";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity needed must be at least 1.")]
    public int QuantityNeeded { get; set; }

    [MaxLength(50)]
    public string Unit { get; set; } = "portions";

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime NeededByDate { get; set; }
}
