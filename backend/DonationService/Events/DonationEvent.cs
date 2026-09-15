using System;

namespace DonationService.Events;

/// <summary>
/// Domain event published to Apache Kafka ('donation-events' topic)
/// whenever a donation is created, updated, or cancelled in DonationService.
/// </summary>
public class DonationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of event: "DonationCreated", "DonationUpdated", "DonationCancelled"
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public int DonationId { get; set; }

    public string DonorId { get; set; } = string.Empty;

    public string DonorName { get; set; } = string.Empty;

    public string DonorEmail { get; set; } = string.Empty;

    public string FoodTitle { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int TotalQuantity { get; set; }

    public int RemainingQuantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ExpiryTime { get; set; }

    public string DietaryTags { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
