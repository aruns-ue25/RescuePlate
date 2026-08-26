namespace UserService.DTOs;

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime MemberSince { get; set; }

    // Donor & Org Profile fields
    public string BusinessOrOrgName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BioOrDescription { get; set; } = string.Empty;

    // Donor specific
    public string? DonorType { get; set; }

    // Organization specific
    public string? RegistrationNumber { get; set; }
    public List<string>? AcceptedFoodCategories { get; set; }
}

public class UpdateProfileDto
{
    public string? BusinessOrOrgName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? BioOrDescription { get; set; }
    public string? DonorType { get; set; }
    public List<string>? AcceptedFoodCategories { get; set; }
}

public class DeleteAccountDto
{
    public string Password { get; set; } = string.Empty;
}
