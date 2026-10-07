using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWorkflowService.Models;

[Table("DeliveryNotifications")]
public class DeliveryNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // COLLECTION_ARRANGED, DONATION_COLLECTED, RECEIPT_CONFIRMED, DONATION_COMPLETED

    public bool IsRead { get; set; } = false;

    [Required]
    public int RelatedId { get; set; } // DeliveryArrangement.Id (Non-null for strict DB unique index constraint)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
