namespace RequestWorkflowService.DTOs;

public class FoodRequestResponseDto
{
    public int Id { get; set; }
    public int DonationId { get; set; }
    public string DonationTitle { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int? AcceptedQuantity { get; set; }
    public string Unit { get; set; } = "portions";
    public string Status { get; set; } = "PENDING";
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
