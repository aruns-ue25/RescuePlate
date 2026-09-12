using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class UpdateAvailabilityDto
{
    [Range(1, 168, ErrorMessage = "Expiry hours must be between 1 and 168.")]
    public int? ExpiryHours { get; set; }

    public DateTime? ExpiryTime { get; set; }
}
