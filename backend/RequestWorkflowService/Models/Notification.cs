namespace RequestWorkflowService.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // DONATION_REQUEST, REQUEST_ACCEPTED, REQUEST_REJECTED, FOOD_OFFER, OFFER_ACCEPTED, OFFER_REJECTED
    public bool IsRead { get; set; } = false;
    public int? RelatedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
