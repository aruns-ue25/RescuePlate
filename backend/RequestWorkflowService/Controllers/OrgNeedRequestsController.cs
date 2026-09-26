using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.DTOs;
using RequestWorkflowService.Models;

namespace RequestWorkflowService.Controllers;

[ApiController]
[Route("api/need-requests")]
public class OrgNeedRequestsController : ControllerBase
{
    private readonly RequestDbContext _context;
    private readonly ILogger<OrgNeedRequestsController> _logger;

    public OrgNeedRequestsController(RequestDbContext context, ILogger<OrgNeedRequestsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Story 1: Organization raises a food need request.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateNeedRequest([FromBody] CreateOrgNeedRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail("Validation failed.", errors));
        }

        var (orgId, orgName) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(orgId))
        {
            return Unauthorized(ApiResponse<OrgNeedRequestResponseDto>.Fail("Invalid or missing Organization authentication claims."));
        }

        // Scenario 3: Date validation
        var neededByUtc = dto.NeededByDate.ToUniversalTime();
        if (neededByUtc <= DateTime.UtcNow)
        {
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail("Needed-by date must be in the future."));
        }

        // Scenario 2: Quantity check
        if (dto.QuantityNeeded <= 0)
        {
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail("Quantity needed must be greater than 0."));
        }

        // Scenario 1 & 4 & 5: Initial Status = OPEN, associated with Organization and selected Location
        var needRequest = new OrgFoodNeedRequest
        {
            OrganizationId = orgId,
            OrganizationName = string.IsNullOrWhiteSpace(orgName) ? "Verified Organization" : orgName,
            Title = dto.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
            QuantityNeeded = dto.QuantityNeeded,
            FulfilledQuantity = 0,
            RemainingNeeded = dto.QuantityNeeded,
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "portions" : dto.Unit.Trim(),
            Description = dto.Description.Trim(),
            Location = dto.Location.Trim(),
            NeededByDate = neededByUtc,
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        _context.OrgNeedRequests.Add(needRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} successfully created food need request {Id} ({Title})",
            orgId, needRequest.Id, needRequest.Title);

        return CreatedAtAction(nameof(GetNeedRequestById), new { id = needRequest.Id },
            ApiResponse<OrgNeedRequestResponseDto>.Ok(MapToResponseDto(needRequest), "Food need request created successfully."));
    }

    /// <summary>
    /// Story 2: Organization views their submitted food need requests.
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<List<OrgNeedRequestResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNeedRequests([FromQuery] string? status = null)
    {
        var (orgId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(orgId))
        {
            return Unauthorized(ApiResponse<List<OrgNeedRequestResponseDto>>.Fail("Invalid or missing Organization authentication claims."));
        }

        var query = _context.OrgNeedRequests.Where(r => r.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status.ToUpper() == status.Trim().ToUpper());
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        // Process dynamic expiration & offer counts
        var responseList = new List<OrgNeedRequestResponseDto>();
        foreach (var r in requests)
        {
            ResolveExpiration(r);
            var offers = await _context.DonorOffers.Where(o => o.OrgFoodNeedRequestId == r.Id).ToListAsync();
            var dto = MapToResponseDto(r);
            dto.PendingOffersCount = offers.Count(o => o.Status == "PENDING");
            dto.TotalOffersCount = offers.Count;
            responseList.Add(dto);
        }

        await _context.SaveChangesAsync(); // Save any status transitions to EXPIRED
        return Ok(ApiResponse<List<OrgNeedRequestResponseDto>>.Ok(responseList));
    }

    /// <summary>
    /// Story 2 Scenario 3: Organization edits valid information on an open food need request.
    /// Preserves QuantityNeeded to protect accepted/pending offer consistency.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNeedRequest(int id, [FromBody] UpdateOrgNeedRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail("Validation failed.", errors));
        }

        var (orgId, _) = GetCallerIdentity();
        var request = await _context.OrgNeedRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<OrgNeedRequestResponseDto>.Fail($"Food need request {id} not found."));
        }

        // Scenario 5: Ownership check
        if (request.OrganizationId != orgId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<OrgNeedRequestResponseDto>.Fail("You do not have permission to edit this food request."));
        }

        ResolveExpiration(request);
        if (request.Status == "CANCELLED" || request.Status == "EXPIRED" || request.Status == "CLAIMED")
        {
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail($"Cannot edit request {id} because its status is {request.Status}."));
        }

        var neededByUtc = dto.NeededByDate.ToUniversalTime();
        if (neededByUtc <= DateTime.UtcNow)
        {
            return BadRequest(ApiResponse<OrgNeedRequestResponseDto>.Fail("Needed-by date must be in the future."));
        }

        request.Title = dto.Title.Trim();
        request.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim();
        request.Description = dto.Description.Trim();
        request.Location = dto.Location.Trim();
        request.NeededByDate = neededByUtc;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} updated food need request {Id}", orgId, request.Id);

        return Ok(ApiResponse<OrgNeedRequestResponseDto>.Ok(MapToResponseDto(request), "Food need request updated successfully."));
    }

    /// <summary>
    /// Story 2 Scenario 4: Organization cancels an open food need request.
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelNeedRequest(int id)
    {
        var (orgId, _) = GetCallerIdentity();
        var request = await _context.OrgNeedRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<OrgNeedRequestResponseDto>.Fail($"Food need request {id} not found."));
        }

        if (request.OrganizationId != orgId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<OrgNeedRequestResponseDto>.Fail("You do not have permission to cancel this food request."));
        }

        request.Status = "CANCELLED";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} cancelled food need request {Id}", orgId, request.Id);

        return Ok(ApiResponse<OrgNeedRequestResponseDto>.Ok(MapToResponseDto(request), "Food need request cancelled successfully."));
    }

    /// <summary>
    /// Story 3: Donors view recent & soon-to-expire open food requests.
    /// Excludes expired & cancelled requests. Surfaced by deadline urgency.
    /// </summary>
    [HttpGet("active")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<OrgNeedRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveNeedRequests(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.OrgNeedRequests
            .Where(r => r.NeededByDate > now && (r.Status == "OPEN" || r.Status == "PARTIALLY_FULFILLED"));

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Category.ToUpper() == category.Trim().ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(s) || r.Description.ToLower().Contains(s) || r.Location.ToLower().Contains(s) || r.OrganizationName.ToLower().Contains(s));
        }

        // Scenario 1 & 2: Surface recent and soon-to-expire requests (order by NeededByDate ascending)
        var requests = await query.OrderBy(r => r.NeededByDate).ToListAsync();
        var dtos = requests.Select(MapToResponseDto).ToList();

        return Ok(ApiResponse<List<OrgNeedRequestResponseDto>>.Ok(dtos));
    }

    /// <summary>
    /// Story 4: Donors or Organizations view detailed info about a specific food request.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrgNeedRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNeedRequestById(int id)
    {
        var request = await _context.OrgNeedRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<OrgNeedRequestResponseDto>.Fail($"Food need request {id} not found."));
        }

        ResolveExpiration(request);
        await _context.SaveChangesAsync();

        var offers = await _context.DonorOffers.Where(o => o.OrgFoodNeedRequestId == id).ToListAsync();
        var dto = MapToResponseDto(request);
        dto.PendingOffersCount = offers.Count(o => o.Status == "PENDING");
        dto.TotalOffersCount = offers.Count;

        return Ok(ApiResponse<OrgNeedRequestResponseDto>.Ok(dto));
    }

    /// <summary>
    /// Story 5: Donor submits a food offer against an Organization's active need request.
    /// </summary>
    [HttpPost("{id:int}/offers")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDonorOffer(int id, [FromBody] CreateDonorOfferDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail("Validation failed.", errors));
        }

        var (donorId, donorName) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<DonorOfferResponseDto>.Fail("Invalid or missing Donor authentication claims."));
        }

        var request = await _context.OrgNeedRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Food need request {id} not found."));
        }

        ResolveExpiration(request);

        // Story 5 Scenario 4: Expired request
        if (request.Status == "EXPIRED" || request.NeededByDate <= DateTime.UtcNow)
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail("This food request has expired and is no longer accepting offers."));
        }

        if (request.Status == "CANCELLED" || request.Status == "CLAIMED")
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail($"This food request is no longer open for offers (Status: {request.Status})."));
        }

        // Story 5 Scenario 3: Invalid quantity
        if (dto.OfferedQuantity <= 0)
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail("Offered quantity must be greater than 0."));
        }

        if (dto.OfferedQuantity > request.RemainingNeeded)
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail(
                $"Offered quantity ({dto.OfferedQuantity}) exceeds remaining needed quantity ({request.RemainingNeeded} {request.Unit})."));
        }

        // Story 5 Scenario 1: Create food offer with PENDING status
        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = request.Id,
            OrgFoodNeedRequestTitle = request.Title,
            DonorId = donorId,
            DonorName = string.IsNullOrWhiteSpace(donorName) ? "Verified Food Donor" : donorName,
            FoodType = dto.FoodType.Trim(),
            OfferedQuantity = dto.OfferedQuantity,
            AcceptedQuantity = null,
            Unit = request.Unit,
            Notes = dto.Notes?.Trim(),
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.DonorOffers.Add(offer);

        var notif = new Notification
        {
            UserId = request.OrganizationId,
            Title = "New Food Offer",
            Message = $"{offer.DonorName} offered {offer.OfferedQuantity} {offer.Unit} of {offer.FoodType} for '{request.Title}'.",
            Type = "FOOD_OFFER",
            RelatedId = request.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Donor {DonorId} submitted offer {OfferId} ({Quantity} {Unit} of {FoodType}) for Need Request {RequestId}",
            donorId, offer.Id, offer.OfferedQuantity, offer.Unit, offer.FoodType, request.Id);

        return CreatedAtAction(nameof(GetOfferById), new { offerId = offer.Id },
            ApiResponse<DonorOfferResponseDto>.Ok(MapOfferToDto(offer), "Food offer submitted successfully. Pending Organization review."));
    }

    /// <summary>
    /// Story 6 Scenario 1: Organization views received food offers for a specific need request.
    /// </summary>
    [HttpGet("{id:int}/offers")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<List<DonorOfferResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffersForNeedRequest(int id)
    {
        var (orgId, _) = GetCallerIdentity();
        var request = await _context.OrgNeedRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(ApiResponse<List<DonorOfferResponseDto>>.Fail($"Food need request {id} not found."));
        }

        // Story 6 Scenario 6: Ownership check
        if (request.OrganizationId != orgId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<List<DonorOfferResponseDto>>.Fail("You do not have permission to view offers for another organization's request."));
        }

        var offers = await _context.DonorOffers
            .Where(o => o.OrgFoodNeedRequestId == id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<DonorOfferResponseDto>>.Ok(offers.Select(MapOfferToDto).ToList()));
    }

    /// <summary>
    /// Donors view all food offers submitted by them across organization requests.
    /// </summary>
    [HttpGet("/api/offers/my-offers")]
    [Authorize(Roles = "DONOR")]
    [ProducesResponseType(typeof(ApiResponse<List<DonorOfferResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubmittedOffers([FromQuery] string? status = null)
    {
        var (donorId, _) = GetCallerIdentity();
        if (string.IsNullOrWhiteSpace(donorId))
        {
            return Unauthorized(ApiResponse<List<DonorOfferResponseDto>>.Fail("Invalid or missing Donor authentication claims."));
        }

        var query = _context.DonorOffers.Where(o => o.DonorId == donorId);
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.Status.ToUpper() == status.Trim().ToUpper());
        }

        var offers = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(ApiResponse<List<DonorOfferResponseDto>>.Ok(offers.Select(MapOfferToDto).ToList()));
    }

    /// <summary>
    /// Retrieves a single offer by ID.
    /// </summary>
    [HttpGet("/api/offers/{offerId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOfferById(int offerId)
    {
        var offer = await _context.DonorOffers.FindAsync(offerId);
        if (offer == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Offer with ID {offerId} not found."));
        }

        return Ok(ApiResponse<DonorOfferResponseDto>.Ok(MapOfferToDto(offer)));
    }

    /// <summary>
    /// Story 6 Scenario 2: Organization accepts a donor's pending food offer.
    /// Supports partial fulfillment & updates request status to PARTIALLY_FULFILLED or CLAIMED.
    /// </summary>
    [HttpPost("/api/offers/{offerId:int}/accept")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptDonorOffer(int offerId)
    {
        var (orgId, _) = GetCallerIdentity();
        var offer = await _context.DonorOffers.FindAsync(offerId);
        if (offer == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Offer {offerId} not found."));
        }

        var request = await _context.OrgNeedRequests.FindAsync(offer.OrgFoodNeedRequestId);
        if (request == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Associated food need request {offer.OrgFoodNeedRequestId} not found."));
        }

        // Story 6 Scenario 6: Ownership check
        if (request.OrganizationId != orgId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<DonorOfferResponseDto>.Fail("You do not have permission to manage offers for this request."));
        }

        // Story 6 Scenario 4: Already decided check
        if (offer.Status != "PENDING")
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail($"Offer {offerId} has already been {offer.Status.ToLower()} and cannot be changed."));
        }

        // Story 6 Scenario 5: Check if request is already fully claimed
        if (request.Status == "CLAIMED" || request.RemainingNeeded <= 0)
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail("This food request has already been fully claimed and fulfilled."));
        }

        // Calculate accepted quantity & update request fulfillment
        int acceptedQty = Math.Min(offer.OfferedQuantity, request.RemainingNeeded);

        offer.Status = "ACCEPTED";
        offer.AcceptedQuantity = acceptedQty;
        offer.UpdatedAt = DateTime.UtcNow;

        request.FulfilledQuantity += acceptedQty;
        request.RemainingNeeded = Math.Max(0, request.QuantityNeeded - request.FulfilledQuantity);

        if (request.RemainingNeeded <= 0)
        {
            request.RemainingNeeded = 0;
            request.Status = "CLAIMED";
        }
        else
        {
            request.Status = "PARTIALLY_FULFILLED";
        }

        request.UpdatedAt = DateTime.UtcNow;

        var (_, orgNameAccept) = GetCallerIdentity();
        var acceptNotif = new Notification
        {
            UserId = offer.DonorId,
            Title = "Offer Accepted",
            Message = $"{orgNameAccept} accepted your offer of {offer.OfferedQuantity} {offer.Unit} of {offer.FoodType} for '{request.Title}'.",
            Type = "OFFER_ACCEPTED",
            RelatedId = request.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(acceptNotif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} accepted offer {OfferId} ({AcceptedQty} {Unit}). Need Request {RequestId} status: {Status}",
            orgId, offer.Id, acceptedQty, offer.Unit, request.Id, request.Status);

        return Ok(ApiResponse<DonorOfferResponseDto>.Ok(MapOfferToDto(offer), "Food offer accepted successfully! The request has been updated."));
    }

    /// <summary>
    /// Story 6 Scenario 3: Organization rejects a donor's pending food offer.
    /// Preserves remaining needed quantity for other offers.
    /// </summary>
    [HttpPost("/api/offers/{offerId:int}/reject")]
    [Authorize(Roles = "ORGANIZATION")]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DonorOfferResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectDonorOffer(int offerId)
    {
        var (orgId, _) = GetCallerIdentity();
        var offer = await _context.DonorOffers.FindAsync(offerId);
        if (offer == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Offer {offerId} not found."));
        }

        var request = await _context.OrgNeedRequests.FindAsync(offer.OrgFoodNeedRequestId);
        if (request == null)
        {
            return NotFound(ApiResponse<DonorOfferResponseDto>.Fail($"Associated food need request {offer.OrgFoodNeedRequestId} not found."));
        }

        // Ownership check
        if (request.OrganizationId != orgId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<DonorOfferResponseDto>.Fail("You do not have permission to manage offers for this request."));
        }

        // Already decided check
        if (offer.Status != "PENDING")
        {
            return BadRequest(ApiResponse<DonorOfferResponseDto>.Fail($"Offer {offerId} has already been {offer.Status.ToLower()} and cannot be changed."));
        }

        offer.Status = "REJECTED";
        offer.UpdatedAt = DateTime.UtcNow;

        var (_, orgNameReject) = GetCallerIdentity();
        var rejectNotif = new Notification
        {
            UserId = offer.DonorId,
            Title = "Offer Declined",
            Message = $"{orgNameReject} declined your offer for '{request.Title}'.",
            Type = "OFFER_REJECTED",
            RelatedId = request.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(rejectNotif);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} rejected offer {OfferId}", orgId, offer.Id);

        return Ok(ApiResponse<DonorOfferResponseDto>.Ok(MapOfferToDto(offer), "Food offer rejected successfully."));
    }

    private static void ResolveExpiration(OrgFoodNeedRequest r)
    {
        if (r.Status != "CANCELLED" && r.Status != "CLAIMED")
        {
            if (DateTime.UtcNow > r.NeededByDate)
            {
                r.Status = "EXPIRED";
            }
        }
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

    private static OrgNeedRequestResponseDto MapToResponseDto(OrgFoodNeedRequest r)
    {
        return new OrgNeedRequestResponseDto
        {
            Id = r.Id,
            OrganizationId = r.OrganizationId,
            OrganizationName = r.OrganizationName,
            Title = r.Title,
            Category = r.Category,
            QuantityNeeded = r.QuantityNeeded,
            FulfilledQuantity = r.FulfilledQuantity,
            RemainingNeeded = r.RemainingNeeded,
            Unit = r.Unit,
            Description = r.Description,
            Location = r.Location,
            NeededByDate = r.NeededByDate,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }

    private static DonorOfferResponseDto MapOfferToDto(DonorFoodOffer o)
    {
        return new DonorOfferResponseDto
        {
            Id = o.Id,
            OrgFoodNeedRequestId = o.OrgFoodNeedRequestId,
            OrgFoodNeedRequestTitle = o.OrgFoodNeedRequestTitle,
            DonorId = o.DonorId,
            DonorName = o.DonorName,
            FoodType = o.FoodType,
            OfferedQuantity = o.OfferedQuantity,
            AcceptedQuantity = o.AcceptedQuantity,
            Unit = o.Unit,
            Notes = o.Notes,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };
    }
}
