using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWorkflowService.Models;

[Table("DeliveryArrangements")]
public class DeliveryArrangement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int RequestId { get; set; }

    [Required]
    public int DonationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string DonationTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DonorId { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OrganizationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledCollectionTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "CollectionArranged"; // CollectionArranged, Collected, Received, Completed

    public DateTime ArrangedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CollectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<DeliveryStatusHistory> History { get; set; } = new();
}
