using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DonationService.DTOs;
using DonationService.Services;

namespace DonationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonationsController : ControllerBase
{
    private readonly IDonationService _donationService;
    private readonly ILogger<DonationsController> _logger;

    public DonationsController(IDonationService donationService, ILogger<DonationsController> logger)
    {
        _donationService = donationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new surplus food donation listing (Donor only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDonation([FromBody] CreateDonationDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<DonationResponseDto>.Fail("Validation failed.", errors));
        }

        var (donorId, donorName, donorEmail) = GetCurrentDonorIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<DonationResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var result = await _donationService.CreateDonationAsync(donorId, donorName, donorEmail, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetDonationById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Updates an existing surplus food donation listing (Owner Donor only).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDonation(int id, [FromBody] UpdateDonationDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<DonationResponseDto>.Fail("Validation failed.", errors));
        }

        var (donorId, _, _) = GetCurrentDonorIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<DonationResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var result = await _donationService.UpdateDonationAsync(id, donorId, dto);
        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancels an existing surplus food donation listing (Owner Donor only).
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelDonation(int id, [FromBody] CancelDonationDto? dto = null)
    {
        var (donorId, _, _) = GetCurrentDonorIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<DonationResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var result = await _donationService.CancelDonationAsync(id, donorId, dto?.Reason);
        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieves all donations posted by the authenticated Donor (with optional status & search filtering).
    /// </summary>
    [HttpGet("my-donations")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<List<DonationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyDonations(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var (donorId, _, _) = GetCurrentDonorIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<List<DonationResponseDto>>.Fail("Invalid or missing Donor authentication claims."));
        }

        var result = await _donationService.GetMyDonationsAsync(donorId, status, search);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single donation listing by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDonationById(int id)
    {
        var result = await _donationService.GetDonationByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Public browse endpoint for available donations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DonationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAvailableDonations(
        [FromQuery] string? category = null, 
        [FromQuery] string? search = null)
    {
        var result = await _donationService.GetAllAvailableDonationsAsync(category, search);
        return Ok(result);
    }

    /// <summary>
    /// Updates the availability period / expiry deadline for a donation.
    /// Scenario 1: Set Availability Period.
    /// </summary>
    [HttpPatch("{id:int}/availability")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability(int id, [FromBody] UpdateAvailabilityDto dto)
    {
        var (donorId, _, _) = GetCurrentDonorIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<DonationResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var result = await _donationService.UpdateAvailabilityAsync(id, donorId, dto);
        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Claims/requests portions of an available donation for an Organization.
    /// Scenario 2 & 4: Prevents requests on expired or unavailable donations.
    /// </summary>
    [HttpPost("{id:int}/request")]
    [Authorize(Roles = "ORGANIZATION,DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestDonation(int id, [FromBody] ClaimRequestDto dto)
    {
        var orgId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? string.Empty;

        var orgName = User.FindFirst("OrganizationName")?.Value
            ?? User.FindFirst("organizationName")?.Value
            ?? User.FindFirst("BusinessName")?.Value
            ?? User.FindFirst("businessName")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? "Partner Organization";

        if (string.IsNullOrWhiteSpace(orgId))
        {
            return Unauthorized(ApiResponse<DonationResponseDto>.Fail("Invalid or missing user authentication claims."));
        }

        var result = await _donationService.RequestDonationAsync(id, orgId, orgName, dto);
        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Scenario 1, 2, 3: Dedicated discovery endpoint for participating Donors.
    /// Returns public profile information (name, type, location, bio, stats).
    /// </summary>
    [HttpGet("donors")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<DonorDiscoveryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParticipatingDonors(
        [FromQuery] string? search = null,
        [FromQuery] string? donorType = null)
    {
        var result = await _donationService.GetParticipatingDonorsAsync(search, donorType);
        return Ok(result);
    }

    /// <summary>
    /// Scenario 4: Select and view a Donor's detailed profile and active listings.
    /// </summary>
    [HttpGet("donors/{donorId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DonorDiscoveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDonorProfileDetails(string donorId)
    {
        var result = await _donationService.GetDonorProfileDetailsAsync(donorId);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    private (string id, string name, string email) GetCurrentDonorIdentity()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? string.Empty;

        var name = User.FindFirst("BusinessName")?.Value
            ?? User.FindFirst("businessName")?.Value
            ?? User.FindFirst("ContactName")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? "Verified Food Donor";

        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
            ?? string.Empty;

        return (idClaim, name, email);
    }
}
