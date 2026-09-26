using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Models;

namespace RequestWorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly RequestDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        RequestDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<RequestsController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Story 1: Submits a new donation request for an available donation (Organization only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateFoodRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail("Validation failed.", errors));
        }

        var (orgId, orgName) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(orgId))
        {
            return Unauthorized(ApiResponse<FoodRequestResponseDto>.Fail("Invalid or missing Organization authentication claims."));
        }

        // 1. Validation - Quantity check (Scenario 3)
        if (dto.RequestedQuantity <= 0)
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail("Requested quantity must be greater than zero."));
        }

        // 2. Fetch donation info from DonationService
        var donation = await FetchDonationFromDonationServiceAsync(dto.DonationId);
        if (donation == null)
        {
            return NotFound(ApiResponse<FoodRequestResponseDto>.Fail($"Donation with ID {dto.DonationId} was not found."));
        }

        // 3. Validation - Expired check (Scenario 5)
        if (donation.ExpiryTime.HasValue && DateTime.UtcNow > donation.ExpiryTime.Value)
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail("This donation listing has expired and is no longer available for requests."));
        }

        // 4. Validation - Availability check (Scenario 6)
        var status = donation.Status ?? "Available";
        if (status.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("FullyClaimed", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail($"This donation is no longer available for requests (Status: {status})."));
        }

        // 5. Validation - Quantity availability check (Scenario 4)
        if (dto.RequestedQuantity > donation.RemainingQuantity)
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail(
                $"Requested quantity ({dto.RequestedQuantity}) exceeds available quantity ({donation.RemainingQuantity})."));
        }

        // 6. Create FoodRequest (Scenario 1 & 2)
        var foodRequest = new FoodRequest
        {
            DonationId = donation.Id,
            DonationTitle = string.IsNullOrWhiteSpace(donation.FoodTitle) ? "Surplus Food Donation" : donation.FoodTitle,
            OrganizationId = orgId,
            OrganizationName = string.IsNullOrWhiteSpace(orgName) ? "Verified Organization" : orgName,
            DonorId = donation.DonorId,
            RequestedQuantity = dto.RequestedQuantity,
            AcceptedQuantity = null,
            Unit = string.IsNullOrWhiteSpace(donation.Unit) ? "portions" : donation.Unit,
            Status = "PENDING",
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Requests.Add(foodRequest);

        // Send Notification to Donor
        var notif = new Notification
        {
            UserId = donation.DonorId,
            Title = "New Food Request",
            Message = $"{foodRequest.OrganizationName} requested {foodRequest.RequestedQuantity} {foodRequest.Unit} of '{foodRequest.DonationTitle}'.",
            Type = "DONATION_REQUEST",
            RelatedId = foodRequest.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} successfully created FoodRequest {RequestId} for Donation {DonationId} ({Quantity} {Unit})",
            orgId, foodRequest.Id, donation.Id, foodRequest.RequestedQuantity, foodRequest.Unit);

        return CreatedAtAction(nameof(GetRequestById), new { id = foodRequest.Id }, ApiResponse<FoodRequestResponseDto>.Ok(MapToResponseDto(foodRequest), "Donation request submitted successfully."));
    }

    /// <summary>
    /// Story 2: Retrieves submitted requests for the authenticated Organization.
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<List<FoodRequestResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRequests([FromQuery] string? status = null)
    {
        var (orgId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(orgId))
        {
            return Unauthorized(ApiResponse<List<FoodRequestResponseDto>>.Fail("Invalid or missing Organization authentication claims."));
        }

        var query = _context.Requests.Where(r => r.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status.ToUpper() == status.Trim().ToUpper());
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var dtos = requests.Select(MapToResponseDto).ToList();

        return Ok(ApiResponse<List<FoodRequestResponseDto>>.Ok(dtos));
    }

    /// <summary>
    /// Story 3: Retrieves received requests for the authenticated Donor.
    /// </summary>
    [HttpGet("received")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<List<FoodRequestResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReceivedRequests(
        [FromQuery] string? status = null,
        [FromQuery] int? donationId = null)
    {
        var (donorId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<List<FoodRequestResponseDto>>.Fail("Invalid or missing Donor authentication claims."));
        }

        var query = _context.Requests.Where(r => r.DonorId == donorId);

        if (donationId.HasValue && donationId.Value > 0)
        {
            query = query.Where(r => r.DonationId == donationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status.ToUpper() == status.Trim().ToUpper());
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var dtos = requests.Select(MapToResponseDto).ToList();

        return Ok(ApiResponse<List<FoodRequestResponseDto>>.Ok(dtos));
    }

    /// <summary>
    /// Retrieves a single request by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestById(int id)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<FoodRequestResponseDto>.Fail($"Request with ID {id} not found."));
        }

        return Ok(ApiResponse<FoodRequestResponseDto>.Ok(MapToResponseDto(request)));
    }

    /// <summary>
    /// Story 4 & Story 6: Donor accepts a pending food request.
    /// Deducts requested quantity from available donation stock.
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        var (donorId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<FoodRequestResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<FoodRequestResponseDto>.Fail($"Request with ID {id} not found."));
        }

        // Ownership check (Story 3 Scenario 5)
        if (request.DonorId != donorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<FoodRequestResponseDto>.Fail("You do not have permission to manage this request."));
        }

        // Already decided check (Story 4 Scenario 5)
        if (request.Status != "PENDING")
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail($"Request {id} has already been {request.Status.ToLower()} and cannot be modified."));
        }

        // Deduct quantity on DonationService
        var deductionSuccess = await DeductQuantityOnDonationServiceAsync(request.DonationId, request.RequestedQuantity);
        if (!deductionSuccess.Success)
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail($"Failed to accept request: {deductionSuccess.ErrorMessage}"));
        }

        // Update Request status (Story 4 Scenario 1)
        request.Status = "ACCEPTED";
        request.AcceptedQuantity = request.RequestedQuantity;
        request.UpdatedAt = DateTime.UtcNow;

        var (_, donorNameAccept) = GetCallerIdentity();
        var acceptNotif = new Notification
        {
            UserId = request.OrganizationId,
            Title = "Request Accepted",
            Message = $"{donorNameAccept} accepted your request for {request.AcceptedQuantity} {request.Unit} of '{request.DonationTitle}'.",
            Type = "REQUEST_ACCEPTED",
            RelatedId = request.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(acceptNotif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Donor {DonorId} accepted FoodRequest {RequestId} for {Quantity} {Unit}",
            donorId, request.Id, request.AcceptedQuantity, request.Unit);

        return Ok(ApiResponse<FoodRequestResponseDto>.Ok(MapToResponseDto(request), "Donation request accepted successfully."));
    }

    /// <summary>
    /// Story 5: Donor rejects a pending food request.
    /// Preserves donation available quantity.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FoodRequestResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectRequestDto? dto = null)
    {
        var (donorId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<FoodRequestResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<FoodRequestResponseDto>.Fail($"Request with ID {id} not found."));
        }

        // Ownership check
        if (request.DonorId != donorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<FoodRequestResponseDto>.Fail("You do not have permission to manage this request."));
        }

        // Already decided check (Story 5 Scenario 3)
        if (request.Status != "PENDING")
        {
            return BadRequest(ApiResponse<FoodRequestResponseDto>.Fail($"Request {id} has already been {request.Status.ToLower()} and cannot be modified."));
        }

        // Update Request status (Story 5 Scenario 1)
        request.Status = "REJECTED";
        request.RejectionReason = dto?.Reason?.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        var (_, donorNameReject) = GetCallerIdentity();
        var rejectNotif = new Notification
        {
            UserId = request.OrganizationId,
            Title = "Request Declined",
            Message = $"{donorNameReject} declined your request for '{request.DonationTitle}'." + (!string.IsNullOrWhiteSpace(request.RejectionReason) ? $" Reason: {request.RejectionReason}" : ""),
            Type = "REQUEST_REJECTED",
            RelatedId = request.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(rejectNotif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Donor {DonorId} rejected FoodRequest {RequestId}", donorId, request.Id);

        return Ok(ApiResponse<FoodRequestResponseDto>.Ok(MapToResponseDto(request), "Donation request rejected successfully."));
    }

    private (string id, string name) GetCallerIdentity()
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
            ?? User.FindFirst("name")?.Value
            ?? "RescuePlate User";

        return (idClaim, name);
    }

    private async Task<DonationInfoInternal?> FetchDonationFromDonationServiceAsync(int donationId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DonationService");
            var response = await client.GetAsync($"/api/donations/{donationId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("FetchDonationFromDonationServiceAsync failed with status {StatusCode} for ID {DonationId}", response.StatusCode, donationId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
            {
                var id = dataEl.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : donationId;
                var foodTitle = dataEl.TryGetProperty("foodTitle", out var ftProp) ? ftProp.GetString() ?? "" : "";
                var donorId = dataEl.TryGetProperty("donorId", out var dIdProp) ? dIdProp.GetString() ?? "" : "";
                var totalQuantity = dataEl.TryGetProperty("totalQuantity", out var tqProp) ? tqProp.GetInt32() : 0;
                var remainingQuantity = dataEl.TryGetProperty("remainingQuantity", out var rqProp) ? rqProp.GetInt32() : 0;
                var unit = dataEl.TryGetProperty("unit", out var uProp) ? uProp.GetString() ?? "portions" : "portions";
                var status = dataEl.TryGetProperty("status", out var sProp) ? sProp.GetString() ?? "Available" : "Available";
                
                DateTime? expiry = null;
                if (dataEl.TryGetProperty("expiryTime", out var expProp) && expProp.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(expProp.GetString(), out var parsedExp))
                    {
                        expiry = parsedExp.ToUniversalTime();
                    }
                }

                return new DonationInfoInternal
                {
                    Id = id,
                    FoodTitle = foodTitle,
                    DonorId = donorId,
                    TotalQuantity = totalQuantity,
                    RemainingQuantity = remainingQuantity,
                    Unit = unit,
                    Status = status,
                    ExpiryTime = expiry
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching donation details for ID {DonationId}", donationId);
        }
        return null;
    }

    private async Task<(bool Success, string ErrorMessage)> DeductQuantityOnDonationServiceAsync(int donationId, int quantity)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DonationService");
            var body = new { Quantity = quantity };
            var response = await client.PostAsJsonAsync($"/api/donations/{donationId}/deduct-quantity", body);

            if (response.IsSuccessStatusCode)
            {
                return (true, string.Empty);
            }

            var content = await response.Content.ReadAsStringAsync();
            string errorMsg = "Unable to update donation quantity.";
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    errorMsg = msgProp.GetString() ?? errorMsg;
                }
            }
            catch { }

            return (false, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DonationService deduct-quantity for ID {DonationId}", donationId);
            return (false, ex.Message);
        }
    }

    private static FoodRequestResponseDto MapToResponseDto(FoodRequest r)
    {
        return new FoodRequestResponseDto
        {
            Id = r.Id,
            DonationId = r.DonationId,
            DonationTitle = r.DonationTitle,
            OrganizationId = r.OrganizationId,
            OrganizationName = r.OrganizationName,
            DonorId = r.DonorId,
            RequestedQuantity = r.RequestedQuantity,
            AcceptedQuantity = r.AcceptedQuantity,
            Unit = r.Unit,
            Status = r.Status,
            Notes = r.Notes,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }

    private class DonationInfoInternal
    {
        public int Id { get; set; }
        public string FoodTitle { get; set; } = string.Empty;
        public string DonorId { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public string Unit { get; set; } = "portions";
        public string Status { get; set; } = "Available";
        public DateTime? ExpiryTime { get; set; }
    }
}
