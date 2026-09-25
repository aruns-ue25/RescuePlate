namespace RequestWorkflowService.DTOs;

public class DonorOfferResponseDto
{
    public int Id { get; set; }
    public int OrgFoodNeedRequestId { get; set; }
    public string OrgFoodNeedRequestTitle { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public int OfferedQuantity { get; set; }
    public int? AcceptedQuantity { get; set; }
    public string Unit { get; set; } = "portions";
    public string? Notes { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
