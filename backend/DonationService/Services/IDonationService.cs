using DonationService.DTOs;

namespace DonationService.Services;

public interface IDonationService
{
    Task<ApiResponse<DonationResponseDto>> CreateDonationAsync(string donorId, string donorName, string donorEmail, CreateDonationDto dto);
    Task<ApiResponse<List<DonationResponseDto>>> GetMyDonationsAsync(string donorId);
    Task<ApiResponse<DonationResponseDto>> GetDonationByIdAsync(int id);
    Task<ApiResponse<List<DonationResponseDto>>> GetAllAvailableDonationsAsync(string? category = null, string? search = null);
}
