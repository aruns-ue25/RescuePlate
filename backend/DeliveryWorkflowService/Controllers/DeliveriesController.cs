using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryWorkflowService.Data;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.Models;

namespace DeliveryWorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryDbContext _context;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(DeliveryDbContext context, ILogger<DeliveriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Story 1: Arrange collection for an accepted donation.
    /// Authorized for Donor (DonorId) or Organization (OrganizationId) associated with the request.
    /// </summary>
    [HttpPost("arrange")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArrangeCollection([FromBody] ArrangeCollectionDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail("Validation failed.", errors));
        }

        var (userId, userRole) = GetCallerIdentity();

        // 1. Authoritative verification of Request acceptance from PostgreSQL database
        var requestInfo = await VerifyRequestAcceptanceAsync(dto.RequestId);
        if (requestInfo == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Donation request with ID {dto.RequestId} was not found."));
        }

        if (requestInfo.Status != "ACCEPTED")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"Collection arrangement rejected. Donation request {dto.RequestId} is in status '{requestInfo.Status}', but must be 'ACCEPTED'."));
        }

        // 2. Strict Authorization Check (Story 1: Donor or Organization ONLY)
        if (userId != requestInfo.DonorId && userId != requestInfo.OrganizationId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("You are not authorized to arrange collection for this donation request."));
        }

        // 3. Duplicate arrangement check (Concurrency & Unique Index guard)
        var existingArrangement = await _context.DeliveryArrangements
            .AnyAsync(d => d.RequestId == dto.RequestId);
        if (existingArrangement)
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"A collection arrangement has already been created for donation request {dto.RequestId}."));
        }

        // 4. Create DeliveryArrangement & History entry in a single atomic database transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var arrangement = new DeliveryArrangement
            {
                RequestId = dto.RequestId,
                DonationId = requestInfo.DonationId,
                DonationTitle = string.IsNullOrWhiteSpace(requestInfo.DonationTitle) ? "Surplus Food Donation" : requestInfo.DonationTitle,
                DonorId = requestInfo.DonorId,
                DonorName = "Donor User",
                OrganizationId = requestInfo.OrganizationId,
                OrganizationName = string.IsNullOrWhiteSpace(requestInfo.OrganizationName) ? "Organization User" : requestInfo.OrganizationName,
                PickupAddress = dto.PickupAddress.Trim(),
                DeliveryAddress = dto.DeliveryAddress.Trim(),
                ContactName = dto.ContactName.Trim(),
                ContactPhone = dto.ContactPhone.Trim(),
                ScheduledCollectionTime = dto.ScheduledCollectionTime.ToUniversalTime(),
                Notes = dto.Notes?.Trim(),
                Status = "CollectionArranged",
                ArrangedAt = now,
                CreatedAt = now
            };

            var history = new DeliveryStatusHistory
            {
                PreviousStatus = "Accepted",
                NewStatus = "CollectionArranged",
                ChangedByUserId = userId,
                ChangedByRole = userRole,
                Notes = dto.Notes?.Trim(),
                Timestamp = now
            };

            arrangement.History.Add(history);
            _context.DeliveryArrangements.Add(arrangement);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} successfully arranged collection for Request {RequestId}, Delivery {DeliveryId}",
                userId, dto.RequestId, arrangement.Id);

            return CreatedAtAction(nameof(GetTrackingById), new { id = arrangement.Id },
                ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(arrangement), "Collection arrangement created successfully."));
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Duplicate arrangement prevented by database constraint for request {RequestId}", dto.RequestId);
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"A collection arrangement has already been created for donation request {dto.RequestId}."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating collection arrangement for request {RequestId}", dto.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("An internal error occurred while creating collection arrangement."));
        }
    }

    /// <summary>
    /// Story 2: Record that a donation has been collected.
    /// Authorized for Donor (DonorId) or Organization (OrganizationId) associated with the donation.
    /// </summary>
    [HttpPost("{id:int}/collect")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordCollection(int id, [FromBody] RecordCollectionDto? dto = null)
    {
        var (userId, userRole) = GetCallerIdentity();

        var arrangement = await _context.DeliveryArrangements
            .FirstOrDefaultAsync(d => d.Id == id);

        if (arrangement == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Delivery arrangement with ID {id} not found."));
        }

        // Strict Authorization Check (Story 2: Donor or Organization ONLY)
        if (userId != arrangement.DonorId && userId != arrangement.OrganizationId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("You are not authorized to record collection for this donation."));
        }

        // Duplicate Check using exact stored string
        if (arrangement.Status == "Collected")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail("Collection has already been recorded for this donation."));
        }

        // Invalid Transition Check using exact stored string
        if (arrangement.Status != "CollectionArranged")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"Cannot record collection. Donation is currently in status '{arrangement.Status}', but must be 'CollectionArranged'."));
        }

        // Atomic Concurrency Guard & History Entry inside a single Database Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var previousStatus = arrangement.Status;

            // Conditional update at database execution level checking exact status "CollectionArranged"
            var updatedRows = await _context.DeliveryArrangements
                .Where(d => d.Id == id && d.Status == "CollectionArranged")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, "Collected")
                    .SetProperty(d => d.CollectedAt, now)
                    .SetProperty(d => d.UpdatedAt, now));

            if (updatedRows == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                    "Action rejected. The donation status has already been updated or modified concurrently."));
            }

            // Insert status history entry in the exact same transaction
            var history = new DeliveryStatusHistory
            {
                DeliveryArrangementId = arrangement.Id,
                PreviousStatus = previousStatus,
                NewStatus = "Collected",
                ChangedByUserId = userId,
                ChangedByRole = userRole,
                Notes = dto?.Notes?.Trim(),
                Timestamp = now
            };

            _context.DeliveryStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} recorded collection for Delivery {DeliveryId}", userId, arrangement.Id);

            var updatedArrangement = await _context.DeliveryArrangements
                .Include(d => d.History)
                .FirstOrDefaultAsync(d => d.Id == id);

            return Ok(ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(updatedArrangement!), "Donation collection recorded successfully."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error recording collection for delivery {DeliveryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("An internal error occurred while recording collection."));
        }
    }

    /// <summary>
    /// Story 3: Intended receiving organization confirms receipt of a collected donation.
    /// Authorized STRICTLY for the receiving Organization (OrganizationId ONLY).
    /// </summary>
    [HttpPost("{id:int}/receive")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmReceipt(int id, [FromBody] ConfirmReceiptDto? dto = null)
    {
        var (userId, userRole) = GetCallerIdentity();

        var arrangement = await _context.DeliveryArrangements
            .FirstOrDefaultAsync(d => d.Id == id);

        if (arrangement == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Delivery arrangement with ID {id} not found."));
        }

        // Strict Recipient Authorization Check (Story 3 Scenario 3: Receiving Organization ONLY)
        if (userId != arrangement.OrganizationId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("Only the designated receiving organization is authorized to confirm receipt of this donation."));
        }

        // Duplicate Check using exact stored strings
        if (arrangement.Status == "Received" || arrangement.Status == "Completed")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail("Receipt has already been confirmed for this donation."));
        }

        // Invalid Transition Check using exact stored string
        if (arrangement.Status != "Collected")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"Cannot confirm receipt. Donation is currently in status '{arrangement.Status}', but must be 'Collected'."));
        }

        // Atomic Concurrency Guard & History Entry inside a single Database Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var previousStatus = arrangement.Status;

            // Conditional update at database execution level checking exact status "Collected"
            var updatedRows = await _context.DeliveryArrangements
                .Where(d => d.Id == id && d.Status == "Collected")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, "Received")
                    .SetProperty(d => d.ReceivedAt, now)
                    .SetProperty(d => d.UpdatedAt, now));

            if (updatedRows == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                    "Action rejected. The donation status has already been updated or modified concurrently."));
            }

            // Insert status history entry in the exact same transaction
            var history = new DeliveryStatusHistory
            {
                DeliveryArrangementId = arrangement.Id,
                PreviousStatus = previousStatus,
                NewStatus = "Received",
                ChangedByUserId = userId,
                ChangedByRole = userRole,
                Notes = dto?.Notes?.Trim(),
                Timestamp = now
            };

            _context.DeliveryStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Organization {UserId} confirmed receipt for Delivery {DeliveryId}", userId, arrangement.Id);

            var updatedArrangement = await _context.DeliveryArrangements
                .Include(d => d.History)
                .FirstOrDefaultAsync(d => d.Id == id);

            return Ok(ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(updatedArrangement!), "Donation receipt confirmed successfully."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error confirming receipt for delivery {DeliveryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("An internal error occurred while confirming receipt."));
        }
    }

    /// <summary>
    /// Story 4: Retrieve current tracking status and complete timestamped status history by delivery ID.
    /// Authorized for Donor (DonorId), Organization (OrganizationId), or ADMIN.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrackingById(int id)
    {
        var (userId, userRole) = GetCallerIdentity();

        var arrangement = await _context.DeliveryArrangements
            .Include(d => d.History)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (arrangement == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Delivery tracking with ID {id} not found."));
        }

        // Authorization Check (Story 4: Donor, Organization, or ADMIN)
        if (userId != arrangement.DonorId && userId != arrangement.OrganizationId && !userRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("You are not authorized to view tracking information for this donation."));
        }

        return Ok(ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(arrangement)));
    }

    /// <summary>
    /// Story 4: Retrieve current tracking status and complete timestamped status history by food request ID.
    /// Authorized for Donor (DonorId), Organization (OrganizationId), or ADMIN.
    /// </summary>
    [HttpGet("request/{requestId:int}")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrackingByRequestId(int requestId)
    {
        var (userId, userRole) = GetCallerIdentity();

        var arrangement = await _context.DeliveryArrangements
            .Include(d => d.History)
            .FirstOrDefaultAsync(d => d.RequestId == requestId);

        if (arrangement == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Tracking information for food request {requestId} was not found."));
        }

        // Authorization Check (Story 4: Donor, Organization, or ADMIN)
        if (userId != arrangement.DonorId && userId != arrangement.OrganizationId && !userRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("You are not authorized to view tracking information for this donation."));
        }

        return Ok(ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(arrangement)));
    }

    /// <summary>
    /// Story 5: Mark a received donation as completed.
    /// Authorized for Donor (DonorId) or Organization (OrganizationId) associated with the donation.
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryTrackingResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteDonation(int id, [FromBody] CompleteDonationDto? dto = null)
    {
        var (userId, userRole) = GetCallerIdentity();

        var arrangement = await _context.DeliveryArrangements
            .FirstOrDefaultAsync(d => d.Id == id);

        if (arrangement == null)
        {
            return NotFound(ApiResponse<DeliveryTrackingResponseDto>.Fail($"Delivery arrangement with ID {id} not found."));
        }

        // Strict Authorization Check (Story 5: Donor or Organization ONLY)
        if (userId != arrangement.DonorId && userId != arrangement.OrganizationId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("You are not authorized to mark this donation as complete."));
        }

        // Duplicate Check using exact stored string
        if (arrangement.Status == "Completed")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail("Donation lifecycle has already been completed."));
        }

        // Invalid Transition Check using exact stored string
        if (arrangement.Status != "Received")
        {
            return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                $"Cannot complete donation. Current status is '{arrangement.Status}', but must be 'Received'."));
        }

        // Atomic Concurrency Guard & History Entry inside a single Database Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var previousStatus = arrangement.Status;

            // Conditional update at database execution level checking exact status "Received"
            var updatedRows = await _context.DeliveryArrangements
                .Where(d => d.Id == id && d.Status == "Received")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, "Completed")
                    .SetProperty(d => d.CompletedAt, now)
                    .SetProperty(d => d.UpdatedAt, now));

            if (updatedRows == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(ApiResponse<DeliveryTrackingResponseDto>.Fail(
                    "Action rejected. The donation status has already been updated or modified concurrently."));
            }

            // Insert status history entry in the exact same transaction
            var history = new DeliveryStatusHistory
            {
                DeliveryArrangementId = arrangement.Id,
                PreviousStatus = previousStatus,
                NewStatus = "Completed",
                ChangedByUserId = userId,
                ChangedByRole = userRole,
                Notes = dto?.Notes?.Trim(),
                Timestamp = now
            };

            _context.DeliveryStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} completed donation lifecycle for Delivery {DeliveryId}", userId, arrangement.Id);

            var updatedArrangement = await _context.DeliveryArrangements
                .Include(d => d.History)
                .FirstOrDefaultAsync(d => d.Id == id);

            return Ok(ApiResponse<DeliveryTrackingResponseDto>.Ok(MapToDto(updatedArrangement!), "Donation lifecycle completed successfully."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing donation lifecycle for delivery {DeliveryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<DeliveryTrackingResponseDto>.Fail("An internal error occurred while completing donation."));
        }
    }

    private (string userId, string role) GetCallerIdentity()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? string.Empty;

        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value
            ?? "USER";

        return (userId, role);
    }

    private async Task<VerifiedRequestInfo?> VerifyRequestAcceptanceAsync(int requestId)
    {
        try
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""DonationId"", ""DonationTitle"", ""OrganizationId"", ""OrganizationName"", ""DonorId"", ""Status""
                FROM ""Requests""
                WHERE ""Id"" = @reqId";

            var param = command.CreateParameter();
            param.ParameterName = "@reqId";
            param.Value = requestId;
            command.Parameters.Add(param);

            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await _context.Database.OpenConnectionAsync();
            }

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new VerifiedRequestInfo
                {
                    Id = reader.GetInt32(0),
                    DonationId = reader.GetInt32(1),
                    DonationTitle = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    OrganizationId = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    OrganizationName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    DonorId = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Status = reader.IsDBNull(6) ? "PENDING" : reader.GetString(6)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying acceptance for food request {RequestId}", requestId);
        }
        return null;
    }

    private static DeliveryTrackingResponseDto MapToDto(DeliveryArrangement d)
    {
        return new DeliveryTrackingResponseDto
        {
            Id = d.Id,
            RequestId = d.RequestId,
            DonationId = d.DonationId,
            DonationTitle = d.DonationTitle,
            DonorId = d.DonorId,
            DonorName = d.DonorName,
            OrganizationId = d.OrganizationId,
            OrganizationName = d.OrganizationName,
            PickupAddress = d.PickupAddress,
            DeliveryAddress = d.DeliveryAddress,
            ContactName = d.ContactName,
            ContactPhone = d.ContactPhone,
            ScheduledCollectionTime = d.ScheduledCollectionTime,
            Notes = d.Notes,
            Status = d.Status,
            ArrangedAt = d.ArrangedAt,
            CollectedAt = d.CollectedAt,
            ReceivedAt = d.ReceivedAt,
            CompletedAt = d.CompletedAt,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            StatusHistory = d.History
                .OrderBy(h => h.Timestamp)
                .Select(h => new DeliveryStatusHistoryDto
                {
                    Id = h.Id,
                    DeliveryArrangementId = h.DeliveryArrangementId,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    ChangedByUserId = h.ChangedByUserId,
                    ChangedByRole = h.ChangedByRole,
                    Notes = h.Notes,
                    Timestamp = h.Timestamp
                }).ToList()
        };
    }

    private class VerifiedRequestInfo
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public string DonationTitle { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string DonorId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
