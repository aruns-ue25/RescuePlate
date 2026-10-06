namespace DeliveryWorkflowService.DTOs;

public class DeliveryTrackingResponseDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int DonationId { get; set; }
    public string DonationTitle { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;

    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime ScheduledCollectionTime { get; set; }
    public string? Notes { get; set; }

    public string Status { get; set; } = string.Empty; // CollectionArranged, Collected, Received, Completed

    public DateTime ArrangedAt { get; set; }
    public DateTime? CollectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<DeliveryStatusHistoryDto> StatusHistory { get; set; } = new();
}
