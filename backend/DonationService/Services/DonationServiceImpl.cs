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

    public async Task<ApiResponse<DonationResponseDto>> UpdateDonationAsync(int id, string donorId, UpdateDonationDto dto)
    {
        try
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return ApiResponse<DonationResponseDto>.Fail("Donation not found.");
            }

            // Scenario 6: Donation Ownership Check
            if (donation.DonorId != donorId)
            {
                return ApiResponse<DonationResponseDto>.Fail("You do not have permission to modify this donation.");
            }

            // Scenario 7: Workflow State Restrictions
            if (donation.Status == "Completed" || donation.Status == "Cancelled")
            {
                return ApiResponse<DonationResponseDto>.Fail($"Donations in '{donation.Status}' state cannot be modified.");
            }

            if (DateTime.UtcNow > donation.ExpiryTime)
            {
                return ApiResponse<DonationResponseDto>.Fail("Expired donations cannot be modified.");
            }

            if (donation.RemainingQuantity == 0 && donation.TotalQuantity > 0)
            {
                return ApiResponse<DonationResponseDto>.Fail("Fully claimed donations cannot be modified.");
            }

            // Scenario 3: Validate updated information (non-empty if provided)
            if (dto.FoodTitle != null && string.IsNullOrWhiteSpace(dto.FoodTitle))
            {
                return ApiResponse<DonationResponseDto>.Fail("Food item title cannot be empty.");
            }

            if (dto.Location != null && string.IsNullOrWhiteSpace(dto.Location))
            {
                return ApiResponse<DonationResponseDto>.Fail("Pickup or delivery location cannot be empty.");
            }

            // Scenario 4: Validate Quantity
            if (dto.TotalQuantity.HasValue)
            {
                if (dto.TotalQuantity.Value <= 0)
                {
                    return ApiResponse<DonationResponseDto>.Fail("Quantity must be a positive number greater than 0.");
                }

                if (dto.TotalQuantity.Value < donation.ClaimedQuantity)
                {
                    return ApiResponse<DonationResponseDto>.Fail($"Total quantity cannot be reduced below the already claimed portions ({donation.ClaimedQuantity} {donation.Unit}).");
                }
            }

            // Scenario 5: Validate Availability Period
            DateTime? calculatedExpiry = null;
            if (dto.ExpiryHours.HasValue)
            {
                if (dto.ExpiryHours.Value <= 0)
                {
                    return ApiResponse<DonationResponseDto>.Fail("Availability period / expiry hours must be greater than 0.");
                }
                calculatedExpiry = DateTime.UtcNow.AddHours(dto.ExpiryHours.Value);
            }
            else if (dto.ExpiryTime.HasValue)
            {
                calculatedExpiry = dto.ExpiryTime.Value.ToUniversalTime();
            }

            if (calculatedExpiry.HasValue && calculatedExpiry.Value <= DateTime.UtcNow)
            {
                return ApiResponse<DonationResponseDto>.Fail("Availability period / expiry time must be in the future.");
            }

            // Scenario 1 & 2: Apply valid changes to fields and preserve unchanged
            if (!string.IsNullOrWhiteSpace(dto.FoodTitle))
            {
                donation.FoodTitle = dto.FoodTitle.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                donation.Category = dto.Category.Trim();
            }

            if (dto.TotalQuantity.HasValue)
            {
                donation.TotalQuantity = dto.TotalQuantity.Value;
                donation.RemainingQuantity = Math.Max(0, donation.TotalQuantity - donation.ClaimedQuantity);
            }

            if (!string.IsNullOrWhiteSpace(dto.Unit))
            {
                donation.Unit = dto.Unit.Trim();
            }

            if (calculatedExpiry.HasValue)
            {
                donation.ExpiryTime = calculatedExpiry.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.CollectionMode))
            {
                donation.CollectionMode = dto.CollectionMode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Location))
            {
                donation.Location = dto.Location.Trim();
            }

            if (dto.Notes != null)
            {
                donation.Notes = dto.Notes.Trim();
            }

            if (dto.DietaryTags != null)
            {
                donation.DietaryTags = dto.DietaryTags.Trim();
            }

            donation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Donation #{Id} successfully updated by Donor #{DonorId}.", donation.Id, donorId);

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), "Donation updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating donation #{Id} for donor #{DonorId}", id, donorId);
            return ApiResponse<DonationResponseDto>.Fail($"Failed to update donation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DonationResponseDto>> CancelDonationAsync(int id, string donorId, string? reason = null)
    {
        try
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return ApiResponse<DonationResponseDto>.Fail("Donation not found.");
            }

            // Scenario 5: Ownership Check
            if (donation.DonorId != donorId)
            {
                return ApiResponse<DonationResponseDto>.Fail("You do not have permission to cancel this donation.");
            }

            // Scenario 3: Prevent Invalid Cancellation (Workflow State Restrictions)
            if (donation.Status == "Cancelled")
            {
                return ApiResponse<DonationResponseDto>.Fail("This donation is already cancelled.");
            }

            if (donation.Status == "Completed")
            {
                return ApiResponse<DonationResponseDto>.Fail("Completed donations cannot be cancelled as they have already been distributed.");
            }

            if (donation.ClaimedQuantity > 0)
            {
                return ApiResponse<DonationResponseDto>.Fail($"Donations with active charity claims ({donation.ClaimedQuantity} {donation.Unit} claimed) cannot be cancelled directly. Please coordinate with the charity organization.");
            }

            // Scenario 1: Cancel Available Donation
            donation.Status = "Cancelled";
            donation.RemainingQuantity = 0;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                var noteSuffix = $"[Cancelled by donor: {reason.Trim()}]";
                donation.Notes = string.IsNullOrWhiteSpace(donation.Notes) ? noteSuffix : $"{donation.Notes} {noteSuffix}";
            }
            donation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Donation #{Id} successfully cancelled by Donor #{DonorId}. Reason: '{Reason}'", id, donorId, reason ?? "None provided");

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), "Donation cancelled successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling donation #{Id} for donor #{DonorId}", id, donorId);
            return ApiResponse<DonationResponseDto>.Fail($"Failed to cancel donation: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DonationResponseDto>> UpdateAvailabilityAsync(int id, string donorId, UpdateAvailabilityDto dto)
    {
        try
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return ApiResponse<DonationResponseDto>.Fail("Donation not found.");
            }

            if (donation.DonorId != donorId)
            {
                return ApiResponse<DonationResponseDto>.Fail("You do not have permission to manage availability for this donation.");
            }

            if (donation.Status == "Cancelled")
            {
                return ApiResponse<DonationResponseDto>.Fail("Cancelled donations cannot have their availability updated.");
            }

            if (donation.Status == "Completed")
            {
                return ApiResponse<DonationResponseDto>.Fail("Completed donations cannot have their availability updated.");
            }

            DateTime? calculatedExpiry = null;
            if (dto.ExpiryHours.HasValue)
            {
                if (dto.ExpiryHours.Value <= 0)
                {
                    return ApiResponse<DonationResponseDto>.Fail("Availability period / expiry hours must be greater than 0.");
                }
                calculatedExpiry = DateTime.UtcNow.AddHours(dto.ExpiryHours.Value);
            }
            else if (dto.ExpiryTime.HasValue)
            {
                calculatedExpiry = dto.ExpiryTime.Value.ToUniversalTime();
            }
            else
            {
                return ApiResponse<DonationResponseDto>.Fail("Please specify expiry hours or an explicit availability deadline.");
            }

            if (calculatedExpiry <= DateTime.UtcNow)
            {
                return ApiResponse<DonationResponseDto>.Fail("Availability period / expiry time must be in the future.");
            }

            donation.ExpiryTime = calculatedExpiry.Value;
            if (donation.Status == "Expired" && donation.RemainingQuantity > 0)
            {
                donation.Status = donation.ClaimedQuantity > 0 ? "Partially Claimed" : "Available";
            }
            donation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Availability for Donation #{Id} updated to {ExpiryTime} by Donor #{DonorId}", id, donation.ExpiryTime, donorId);

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), "Donation availability period updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating availability for donation #{Id}", id);
            return ApiResponse<DonationResponseDto>.Fail($"Failed to update availability: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DonationResponseDto>> RequestDonationAsync(int id, string organizationId, string organizationName, ClaimRequestDto dto)
    {
        try
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return ApiResponse<DonationResponseDto>.Fail("Donation not found.");
            }

            // Scenario 3 & 4: Prevent request on expired donation
            if (DateTime.UtcNow > donation.ExpiryTime || donation.Status == "Expired")
            {
                return ApiResponse<DonationResponseDto>.Fail("This food donation has reached the end of its availability period (expired) and cannot receive new requests.");
            }

            if (donation.Status == "Cancelled")
            {
                return ApiResponse<DonationResponseDto>.Fail("This food donation has been cancelled by the donor and is no longer available.");
            }

            if (donation.Status == "Completed")
            {
                return ApiResponse<DonationResponseDto>.Fail("This food donation has already been completed and distributed.");
            }

            if (donation.RemainingQuantity <= 0)
            {
                return ApiResponse<DonationResponseDto>.Fail("All available portions for this food donation have already been claimed.");
            }

            if (dto.Quantity <= 0)
            {
                return ApiResponse<DonationResponseDto>.Fail("Requested portion quantity must be greater than 0.");
            }

            if (dto.Quantity > donation.RemainingQuantity)
            {
                return ApiResponse<DonationResponseDto>.Fail($"Requested quantity ({dto.Quantity}) exceeds remaining available portions ({donation.RemainingQuantity} {donation.Unit}).");
            }

            donation.ClaimedQuantity += dto.Quantity;
            donation.RemainingQuantity = Math.Max(0, donation.TotalQuantity - donation.ClaimedQuantity);

            if (donation.RemainingQuantity == 0)
            {
                donation.Status = "Fully Claimed";
            }
            else
            {
                donation.Status = "Partially Claimed";
            }

            donation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Organization '{OrgName}' (#{OrgId}) claimed {Quantity} portions of Donation #{DonationId}. Remaining: {Remaining}",
                organizationName, organizationId, dto.Quantity, donation.Id, donation.RemainingQuantity);

            return ApiResponse<DonationResponseDto>.Ok(MapToResponseDto(donation), $"Successfully requested {dto.Quantity} {donation.Unit} for {organizationName}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing food request for donation #{Id}", id);
            return ApiResponse<DonationResponseDto>.Fail($"Failed to process food request: {ex.Message}");
        }
    }

    public async Task<int> ProcessExpiredDonationsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredDonations = await _context.Donations
                .Where(d => d.Status != "Completed" && 
                            d.Status != "Cancelled" && 
                            d.Status != "Expired" && 
                            now > d.ExpiryTime)
                .ToListAsync();

            if (expiredDonations.Count == 0) return 0;

            foreach (var d in expiredDonations)
            {
                d.Status = "Expired";
                d.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("System workflow: {Count} past-deadline donations automatically marked as Expired.", expiredDonations.Count);
            return expiredDonations.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired donations workflow.");
            return 0;
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
            // Scenario 4: Exclude cancelled and completed donations from organization browsing
            var query = _context.Donations
                .Where(d => d.Status != "Cancelled" && d.Status != "Completed");

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

            // Exclude expired and fully claimed from available browse listings
            var dtos = donations
                .Select(MapToResponseDto)
                .Where(d => d.Status != "Cancelled" && d.Status != "Completed" && !d.IsExpired && d.RemainingQuantity > 0)
                .ToList();

            return ApiResponse<List<DonationResponseDto>>.Ok(dtos, "Available food listings retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available donations.");
            return ApiResponse<List<DonationResponseDto>>.Fail($"Failed to retrieve available listings: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DonorDiscoveryDto>>> GetParticipatingDonorsAsync(string? search = null, string? donorType = null)
    {
        try
        {
            var query = _context.Donations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d =>
                    d.DonorName.ToLower().Contains(s) ||
                    d.Location.ToLower().Contains(s) ||
                    d.Notes.ToLower().Contains(s));
            }

            var allDonations = await query.ToListAsync();
            var grouped = allDonations.GroupBy(d => d.DonorId).ToList();

            var list = new List<DonorDiscoveryDto>();
            var now = DateTime.UtcNow;

            foreach (var group in grouped)
            {
                var donorId = group.Key;
                var latestDonation = group.OrderByDescending(d => d.CreatedAt).First();
                var earliestDonation = group.OrderBy(d => d.CreatedAt).First();

                var activeCount = group.Count(d => d.Status != "Cancelled" && d.Status != "Completed" && d.Status != "Expired" && d.ExpiryTime > now && (d.TotalQuantity - d.ClaimedQuantity) > 0);
                var completedCount = group.Count(d => d.Status == "Completed" || d.Status == "Fully Claimed");
                var totalPortions = group.Sum(d => d.TotalQuantity);

                // Guess or infer donor type from category/notes or default to verified business
                var inferredType = "Food Business";
                if (group.Any(d => d.Category == "Bakery")) inferredType = "Bakery";
                else if (group.Any(d => d.Category == "Cooked Meals")) inferredType = "Restaurant";
                else if (group.Any(d => d.Category == "Fresh Produce")) inferredType = "Supermarket";

                if (!string.IsNullOrWhiteSpace(donorType) && donorType.ToUpper() != "ALL")
                {
                    if (!inferredType.Equals(donorType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                list.Add(new DonorDiscoveryDto
                {
                    DonorId = donorId,
                    BusinessName = latestDonation.DonorName,
                    DonorType = inferredType,
                    Location = latestDonation.Location,
                    Bio = !string.IsNullOrWhiteSpace(latestDonation.Notes) 
                        ? latestDonation.Notes 
                        : $"Partnered food donor contributing surplus to combat local food insecurity.",
                    ProfilePictureUrl = null,
                    ActiveDonationsCount = activeCount,
                    CompletedDonationsCount = completedCount,
                    TotalPortionsContributed = totalPortions,
                    MemberSince = earliestDonation.CreatedAt
                });
            }

            return ApiResponse<List<DonorDiscoveryDto>>.Ok(list, "Participating donors retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving participating donors.");
            return ApiResponse<List<DonorDiscoveryDto>>.Fail($"Failed to retrieve donors: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DonorDiscoveryDto>> GetDonorProfileDetailsAsync(string donorId)
    {
        try
        {
            var donorListings = await _context.Donations
                .Where(d => d.DonorId == donorId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            if (donorListings.Count == 0)
            {
                return ApiResponse<DonorDiscoveryDto>.Fail("Donor not found or has no public activity.");
            }

            var latest = donorListings.First();
            var earliest = donorListings.Last();
            var now = DateTime.UtcNow;

            var activeCount = donorListings.Count(d => d.Status != "Cancelled" && d.Status != "Completed" && d.Status != "Expired" && d.ExpiryTime > now && (d.TotalQuantity - d.ClaimedQuantity) > 0);
            var completedCount = donorListings.Count(d => d.Status == "Completed" || d.Status == "Fully Claimed");
            var totalPortions = donorListings.Sum(d => d.TotalQuantity);

            var activeDtos = donorListings
                .Where(d => d.Status != "Cancelled" && d.Status != "Completed" && d.ExpiryTime > now && (d.TotalQuantity - d.ClaimedQuantity) > 0)
                .Select(MapToResponseDto)
                .ToList();

            var inferredType = "Food Business";
            if (donorListings.Any(d => d.Category == "Bakery")) inferredType = "Bakery";
            else if (donorListings.Any(d => d.Category == "Cooked Meals")) inferredType = "Restaurant";
            else if (donorListings.Any(d => d.Category == "Fresh Produce")) inferredType = "Supermarket";

            var dto = new DonorDiscoveryDto
            {
                DonorId = donorId,
                BusinessName = latest.DonorName,
                DonorType = inferredType,
                Location = latest.Location,
                Bio = !string.IsNullOrWhiteSpace(latest.Notes) ? latest.Notes : "Verified food rescue contributor.",
                ActiveDonationsCount = activeCount,
                CompletedDonationsCount = completedCount,
                TotalPortionsContributed = totalPortions,
                MemberSince = earliest.CreatedAt,
                ActiveListings = activeDtos
            };

            return ApiResponse<DonorDiscoveryDto>.Ok(dto, "Donor profile details retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving donor profile details for {DonorId}", donorId);
            return ApiResponse<DonorDiscoveryDto>.Fail($"Failed to retrieve donor profile: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<OrganizationDiscoveryDto>>> GetParticipatingOrganizationsAsync(string? search = null, string? foodCategory = null)
    {
        try
        {
            // Preset participating charitable food relief partners registered in the platform
            var organizations = new List<OrganizationDiscoveryDto>
            {
                new()
                {
                    OrganizationId = "a44beac7-bd54-4946-b94b-9a17b0716fcb",
                    OrganizationName = "Sri Hope Community Kitchen",
                    OrganizationType = "Community Kitchen",
                    Location = "Trinco Road, Batticaloa",
                    Description = "Providing hot cooked meals, fresh bread, and essential nutritional support to vulnerable families and community shelters across the eastern province.",
                    AcceptedFoodTypes = new List<string> { "Cooked Meals", "Bakery", "Fresh Produce" },
                    ProfilePictureUrl = null,
                    ClaimedDonationsCount = 8,
                    TotalPortionsReceived = 145,
                    MemberSince = new DateTime(2026, 9, 12, 6, 8, 27, DateTimeKind.Utc)
                },
                new()
                {
                    OrganizationId = "org-colombo-foodbank-2026",
                    OrganizationName = "Colombo City Food Bank",
                    OrganizationType = "Food Bank",
                    Location = "Dharmapala Mawatha, Colombo 07",
                    Description = "Dedicated metropolitan food redistribution hub collecting bulk surplus bakery products, dairy, and packed food for orphanages and senior care centers.",
                    AcceptedFoodTypes = new List<string> { "Bakery", "Dairy & Chilled", "Packaged Dry", "Cooked Meals" },
                    ProfilePictureUrl = null,
                    ClaimedDonationsCount = 14,
                    TotalPortionsReceived = 320,
                    MemberSince = new DateTime(2026, 8, 15, 10, 30, 0, DateTimeKind.Utc)
                },
                new()
                {
                    OrganizationId = "org-kandy-relief-care",
                    OrganizationName = "Hill Country Food Relief & Shelter",
                    OrganizationType = "Homeless Shelter",
                    Location = "Peradeniya Road, Kandy",
                    Description = "Operating evening kitchens and shelter care feeding daily-wage workers and underprivileged children with high-protein wholesome meals.",
                    AcceptedFoodTypes = new List<string> { "Cooked Meals", "Bakery", "Fresh Produce" },
                    ProfilePictureUrl = null,
                    ClaimedDonationsCount = 5,
                    TotalPortionsReceived = 95,
                    MemberSince = new DateTime(2026, 8, 20, 14, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    OrganizationId = "org-galle-youth-care",
                    OrganizationName = "Southern Youth Care Foundation",
                    OrganizationType = "Charity Foundation",
                    Location = "Main Street, Galle Fort",
                    Description = "Supporting low-income coastal community programs and student nutrition drives with fresh bakery and healthy pantry staples.",
                    AcceptedFoodTypes = new List<string> { "Bakery", "Dairy & Chilled", "Packaged Dry" },
                    ProfilePictureUrl = null,
                    ClaimedDonationsCount = 9,
                    TotalPortionsReceived = 180,
                    MemberSince = new DateTime(2026, 8, 25, 9, 15, 0, DateTimeKind.Utc)
                }
            };

            var filtered = organizations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                filtered = filtered.Where(o =>
                    o.OrganizationName.ToLower().Contains(s) ||
                    o.Location.ToLower().Contains(s) ||
                    o.Description.ToLower().Contains(s) ||
                    o.OrganizationType.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(foodCategory) && foodCategory.ToUpper() != "ALL")
            {
                var cat = foodCategory.Trim();
                filtered = filtered.Where(o => o.AcceptedFoodTypes.Any(t => t.Equals(cat, StringComparison.OrdinalIgnoreCase)));
            }

            return ApiResponse<List<OrganizationDiscoveryDto>>.Ok(filtered.ToList(), "Participating organizations retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving participating organizations.");
            return ApiResponse<List<OrganizationDiscoveryDto>>.Fail($"Failed to retrieve organizations: {ex.Message}");
        }
    }

    public async Task<ApiResponse<OrganizationDiscoveryDto>> GetOrganizationProfileDetailsAsync(string organizationId)
    {
        try
        {
            var listResult = await GetParticipatingOrganizationsAsync();
            var org = listResult.Data?.FirstOrDefault(o => o.OrganizationId.Equals(organizationId, StringComparison.OrdinalIgnoreCase));

            if (org == null)
            {
                return ApiResponse<OrganizationDiscoveryDto>.Fail("Organization not found.");
            }

            return ApiResponse<OrganizationDiscoveryDto>.Ok(org, "Organization profile details retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization details for {OrgId}", organizationId);
            return ApiResponse<OrganizationDiscoveryDto>.Fail($"Failed to retrieve organization profile: {ex.Message}");
        }
    }

    private static DonationResponseDto MapToResponseDto(Donation d)
    {
        var calculatedRemaining = d.Status == "Cancelled" ? 0 : Math.Max(0, d.TotalQuantity - d.ClaimedQuantity);
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
