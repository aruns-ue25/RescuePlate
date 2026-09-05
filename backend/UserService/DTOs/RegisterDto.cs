using System.ComponentModel.DataAnnotations;
using UserService.Models;

namespace UserService.DTOs;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.DONOR;

    [Required]
    public string BusinessOrOrgName { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    // Role specific
    public string DonorType { get; set; } = "Restaurant";

    public List<string> AcceptedFoodTypes { get; set; } = new();
}
