namespace RequestWorkflowService.Models;

public class OrgFoodNeedRequest
{
    public int Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // Cooked Meals, Bakery, Fresh Produce, Dairy & Chilled, Packaged Dry, General
    public int QuantityNeeded { get; set; }
    public int FulfilledQuantity { get; set; } = 0;
    public int RemainingNeeded { get; set; }
    public string Unit { get; set; } = "portions";
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime NeededByDate { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, PARTIALLY_FULFILLED, CLAIMED, CANCELLED, EXPIRED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
