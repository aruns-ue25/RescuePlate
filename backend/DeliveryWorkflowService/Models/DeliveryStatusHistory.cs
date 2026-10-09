using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWorkflowService.Models;

[Table("DeliveryStatusHistories")]
public class DeliveryStatusHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DeliveryArrangementId { get; set; }

    [ForeignKey(nameof(DeliveryArrangementId))]
    public DeliveryArrangement? DeliveryArrangement { get; set; }

    [Required]
    [MaxLength(50)]
    public string PreviousStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NewStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ChangedByUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ChangedByRole { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
