namespace RequestWorkflowService.Models;

public class FoodRequest
{
    public int Id { get; set; }
    public int DonationId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
