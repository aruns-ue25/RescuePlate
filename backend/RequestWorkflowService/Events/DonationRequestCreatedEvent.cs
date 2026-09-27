namespace RequestWorkflowService.Events;

public class DonationRequestCreatedEvent : RequestBaseEvent
{
    public DonationRequestCreatedEvent()
    {
        EventType = "DonationRequestCreated";
    }

    public int RequestId { get; set; }
    public int DonationId { get; set; }
    public string DonationTitle { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
