namespace RequestWorkflowService.Events;

public class FoodNeedRequestCreatedEvent : RequestBaseEvent
{
    public FoodNeedRequestCreatedEvent()
    {
        EventType = "FoodNeedRequestCreated";
    }

    public int NeedRequestId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
