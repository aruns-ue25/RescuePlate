using System.ComponentModel.DataAnnotations;

namespace RequestWorkflowService.DTOs;

public class UpdateOrgNeedRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = "General";

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime NeededByDate { get; set; }
}
