using DonationService.DTOs;

namespace DonationService.Services;

public interface IDonationService
{
    Task<ApiResponse<DonationResponseDto>> CreateDonationAsync(string donorId, string donorName, string donorEmail, CreateDonationDto dto);
    Task<ApiResponse<DonationResponseDto>> UpdateDonationAsync(int id, string donorId, UpdateDonationDto dto);
    Task<ApiResponse<DonationResponseDto>> CancelDonationAsync(int id, string donorId, string? reason = null);
    Task<ApiResponse<DonationResponseDto>> UpdateAvailabilityAsync(int id, string donorId, UpdateAvailabilityDto dto);
    Task<ApiResponse<DonationResponseDto>> RequestDonationAsync(int id, string organizationId, string organizationName, ClaimRequestDto dto);
    Task<int> ProcessExpiredDonationsAsync();
    Task<ApiResponse<List<DonationResponseDto>>> GetMyDonationsAsync(string donorId, string? status = null, string? search = null);
    Task<ApiResponse<DonationResponseDto>> GetDonationByIdAsync(int id);
    Task<ApiResponse<List<DonationResponseDto>>> GetAllAvailableDonationsAsync(string? category = null, string? search = null);
    Task<ApiResponse<List<DonorDiscoveryDto>>> GetParticipatingDonorsAsync(string? search = null, string? donorType = null);
    Task<ApiResponse<DonorDiscoveryDto>> GetDonorProfileDetailsAsync(string donorId);
    Task<ApiResponse<List<OrganizationDiscoveryDto>>> GetParticipatingOrganizationsAsync(string? search = null, string? foodCategory = null);
    Task<ApiResponse<OrganizationDiscoveryDto>> GetOrganizationProfileDetailsAsync(string organizationId);
}
