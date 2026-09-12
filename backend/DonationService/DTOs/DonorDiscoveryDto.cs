namespace DonationService.DTOs;

public class DonorDiscoveryDto
{
    public string DonorId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string DonorType { get; set; } = "Food Business";
    public string Location { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int ActiveDonationsCount { get; set; }
    public int CompletedDonationsCount { get; set; }
    public int TotalPortionsContributed { get; set; }
    public DateTime? MemberSince { get; set; }
    public List<DonationResponseDto>? ActiveListings { get; set; }
}
