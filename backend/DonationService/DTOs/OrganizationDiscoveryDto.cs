namespace DonationService.DTOs;

public class OrganizationDiscoveryDto
{
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = "Community Food Bank";
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AcceptedFoodTypes { get; set; } = new();
    public string? ProfilePictureUrl { get; set; }
    public int ClaimedDonationsCount { get; set; }
    public int TotalPortionsReceived { get; set; }
    public DateTime? MemberSince { get; set; }
}
