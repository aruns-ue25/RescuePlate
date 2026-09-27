namespace RequestWorkflowService.Events;

public class FoodOfferStatusChangedEvent : RequestBaseEvent
{
    public FoodOfferStatusChangedEvent()
    {
        EventType = "FoodOfferStatusChanged";
    }

    public int OfferId { get; set; }
    public int NeedRequestId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
