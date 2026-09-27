namespace RequestWorkflowService.Events;

public class FoodOfferCreatedEvent : RequestBaseEvent
{
    public FoodOfferCreatedEvent()
    {
        EventType = "FoodOfferCreated";
    }

    public int OfferId { get; set; }
    public int NeedRequestId { get; set; }
    public string DonorId { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
