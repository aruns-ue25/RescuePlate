namespace RequestWorkflowService.Events;

public class DonationRequestAcceptedEvent : RequestBaseEvent
{
    public DonationRequestAcceptedEvent()
    {
        EventType = "DonationRequestAccepted";
    }

    public int RequestId { get; set; }
    public int DonationId { get; set; }
    public string DonationTitle { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public int AcceptedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
}
