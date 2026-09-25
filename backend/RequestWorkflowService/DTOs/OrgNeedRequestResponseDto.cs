namespace RequestWorkflowService.DTOs;

public class OrgNeedRequestResponseDto
{
    public int Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int QuantityNeeded { get; set; }
    public int FulfilledQuantity { get; set; }
    public int RemainingNeeded { get; set; }
    public string Unit { get; set; } = "portions";
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime NeededByDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int PendingOffersCount { get; set; }
    public int TotalOffersCount { get; set; }
}
