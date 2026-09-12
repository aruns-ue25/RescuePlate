using Microsoft.EntityFrameworkCore;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.Services;

public class DonationServiceImpl : IDonationService
{
    private readonly DonationDbContext _context;
    private readonly ILogger<DonationServiceImpl> _logger;

    public DonationServiceImpl(DonationDbContext context, ILogger<DonationServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<DonationResponseDto>> CreateDonationAsync(
        string donorId, 
        string donorName, 
        string donorEmail, 
        CreateDonationDto dto)
    {
        try
        {
            // 1. Validation - Quantity check (Scenario 3)
            if (dto.TotalQuantity <= 0)
            {
                return ApiResponse<DonationResponseDto>.Fail("Quantity must be a positive number greater than 0.");
            }

            // 2. Validation - Expiry check (Scenario 4)
            DateTime calculatedExpiry;
            if (dto.ExpiryHours.HasValue && dto.ExpiryHours.Value > 0)
            {
                calculatedExpiry = DateTime.UtcNow.AddHours(dto.ExpiryHours.Value);
            }
            else if (dto.ExpiryTime.HasValue)
            {
                calculatedExpiry = dto.ExpiryTime.Value.ToUniversalTime();
            }
            else
            {
                calculatedExpiry = DateTime.UtcNow.AddHours(4); // Default 4-hour safety window
            }

            if (calculatedExpiry <= DateTime.UtcNow)
            {
                return ApiResponse<DonationResponseDto>.Fail("Availability period / expiry time must be in the future.");
            }

            // 3. Validation - Required fields (Scenario 2)
            if (string.IsNullOrWhiteSpace(dto.FoodTitle))
            {
                return ApiResponse<DonationResponseDto>.Fail("Food item title is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Location))
            {
                return ApiResponse<DonationResponseDto>.Fail("Pickup or delivery location is required.");
            }

            // 4. Create and persist entity (Scenario 1 & 5)
            var donation = new Donation
            {
                DonorId = donorId,
                DonorName = string.IsNullOrWhiteSpace(donorName) ? "Verified Donor" : donorName,
                DonorEmail = donorEmail ?? string.Empty,
                FoodTitle = dto.FoodTitle.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Cooked Meals" : dto.Category.Trim(),
                TotalQuantity = dto.TotalQuantity,
                ClaimedQuantity = 0,
                RemainingQuantity = dto.TotalQuantity,
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "portions" : dto.Unit.Trim(),
                ExpiryTime = calculatedExpiry,
                CollectionMode = string.IsNullOrWhiteSpace(dto.CollectionMode) ? "Organization Pickup" : dto.CollectionMode.Trim(),
                Status = "Posted", // Initial Status: Posted
                Location = dto.Location.Trim(),
                Notes = dto.Notes?.Trim() ?? string.Empty,
                DietaryTags = dto.DietaryTags?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Donations.AddAsync(donation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Donation #{Id} ('{Title}') posted by Donor #{DonorId} successfully.", 
                donation.Id, donation.FoodTitle, donation.DonorId);

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), "Surplus food donation posted successfully!");
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException != null ? $"{ex.Message} --> Inner: {ex.InnerException.Message}" : ex.Message;
            _logger.LogError(ex, "Error creating donation for Donor #{DonorId}: {Detail}", donorId, detail);
            return ApiResponse<DonationResponseDto>.Fail($"An error occurred while creating the donation: {detail}");
        }
    }

    public async Task<ApiResponse<List<DonationResponseDto>>> GetMyDonationsAsync(
        string donorId, 
        string? status = null, 
        string? search = null)
    {
        try
        {
            var query = _context.Donations
                .Where(d => d.DonorId == donorId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d => 
                    d.FoodTitle.ToLower().Contains(s) || 
                    d.Location.ToLower().Contains(s) || 
                    d.Category.ToLower().Contains(s) ||
                    d.Notes.ToLower().Contains(s));
            }

            var donations = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var dtos = donations.Select(MapToResponseDto).ToList();

            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
            {
                var sFilter = status.Trim().ToLower();
                if (sFilter == "active" || sFilter == "available")
                {
                    dtos = dtos.Where(d => (d.Status == "Posted" || d.Status == "Available" || d.Status == "Partially Claimed") && !d.IsExpired).ToList();
                }
                else if (sFilter == "expired")
                {
                    dtos = dtos.Where(d => d.IsExpired || d.Status == "Expired").ToList();
                }
                else
                {
                    dtos = dtos.Where(d => d.Status.ToLower() == sFilter).ToList();
                }
            }

            return ApiResponse<List<DonationResponseDto>>.Ok(dtos, "My surplus listings retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching donations for donor #{DonorId}", donorId);
            return ApiResponse<List<DonationResponseDto>>.Fail($"Failed to retrieve donations: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DonationResponseDto>> GetDonationByIdAsync(int id)
    {
        try
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return ApiResponse<DonationResponseDto>.Fail("Donation not found.");
            }

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), "Donation retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching donation #{Id}", id);
            return ApiResponse<DonationResponseDto>.Fail($"Failed to retrieve donation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DonationResponseDto>>> GetAllAvailableDonationsAsync(string? category = null, string? search = null)
    {
        try
        {
            var query = _context.Donations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category.ToUpper() != "ALL")
            {
                query = query.Where(d => d.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(d => 
                    d.FoodTitle.ToLower().Contains(s) || 
                    d.DonorName.ToLower().Contains(s) || 
                    d.Location.ToLower().Contains(s) ||
                    d.Notes.ToLower().Contains(s));
            }

            var donations = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var dtos = donations.Select(MapToResponseDto).ToList();
            return ApiResponse<List<DonationResponseDto>>.Ok(dtos, "Available food listings retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available donations.");
            return ApiResponse<List<DonationResponseDto>>.Fail($"Failed to retrieve available listings: {ex.Message}");
        }
    }

    private static DonationResponseDto MapToResponseDto(Donation d)
    {
        var calculatedRemaining = Math.Max(0, d.TotalQuantity - d.ClaimedQuantity);
        var effectiveStatus = d.Status;

        if (effectiveStatus != "Completed" && effectiveStatus != "Cancelled")
        {
            if (DateTime.UtcNow > d.ExpiryTime)
            {
                effectiveStatus = "Expired";
            }
            else if (calculatedRemaining == 0 && d.TotalQuantity > 0)
            {
                effectiveStatus = "Fully Claimed";
            }
            else if (d.ClaimedQuantity > 0 && calculatedRemaining > 0)
            {
                effectiveStatus = "Partially Claimed";
            }
            else if (string.IsNullOrWhiteSpace(effectiveStatus) || effectiveStatus == "Posted")
            {
                effectiveStatus = "Available";
            }
        }

        return new DonationResponseDto
        {
            Id = d.Id,
            DonorId = d.DonorId,
            DonorName = d.DonorName,
            DonorEmail = d.DonorEmail,
            FoodTitle = d.FoodTitle,
            Category = d.Category,
            TotalQuantity = d.TotalQuantity,
            ClaimedQuantity = d.ClaimedQuantity,
            RemainingQuantity = calculatedRemaining,
            Unit = d.Unit,
            ExpiryTime = d.ExpiryTime,
            CollectionMode = d.CollectionMode,
            Status = effectiveStatus,
            Location = d.Location,
            Notes = d.Notes,
            DietaryTags = d.DietaryTags,
            CreatedAt = d.CreatedAt
        };
    }
}
