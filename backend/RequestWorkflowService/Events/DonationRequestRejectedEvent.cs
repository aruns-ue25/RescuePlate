namespace RequestWorkflowService.Events;

public class DonationRequestRejectedEvent : RequestBaseEvent
{
    public DonationRequestRejectedEvent()
    {
        EventType = "DonationRequestRejected";
    }

    public int RequestId { get; set; }
    public int DonationId { get; set; }
    public string DonationTitle { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
}
