using System.ComponentModel.DataAnnotations;

namespace DeliveryWorkflowService.DTOs;

public class ArrangeCollectionDto
{
    [Required(ErrorMessage = "RequestId is required.")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "PickupAddress is required.")]
    [StringLength(255, ErrorMessage = "PickupAddress cannot exceed 255 characters.")]
    public string PickupAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "DeliveryAddress is required.")]
    [StringLength(255, ErrorMessage = "DeliveryAddress cannot exceed 255 characters.")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "ContactName is required.")]
    [StringLength(150, ErrorMessage = "ContactName cannot exceed 150 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ContactPhone is required.")]
    [StringLength(50, ErrorMessage = "ContactPhone cannot exceed 50 characters.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScheduledCollectionTime is required.")]
    public DateTime ScheduledCollectionTime { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
