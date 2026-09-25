using System.ComponentModel.DataAnnotations;

namespace RequestWorkflowService.DTOs;

public class RejectRequestDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
