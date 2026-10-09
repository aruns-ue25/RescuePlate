namespace DeliveryWorkflowService.DTOs;

public class DeliveryStatusHistoryDto
{
    public int Id { get; set; }
    public int DeliveryArrangementId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByRole { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
}
