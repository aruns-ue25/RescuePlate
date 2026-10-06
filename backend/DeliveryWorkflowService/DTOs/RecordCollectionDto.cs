using System.ComponentModel.DataAnnotations;

namespace DeliveryWorkflowService.DTOs;

public class RecordCollectionDto
{
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
