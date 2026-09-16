using FluentAssertions;
using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.UnitTests;

public class OwnershipAndAuthorizationUnitTests
{
    [Fact]
    public async Task UpdateDonation_OtherDonor_IsRejectedWithPermissionError()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "legitimate-owner-id",
            DonorName = "Legitimate Owner",
            FoodTitle = "Original Meal",
            Category = "Cooked Meals",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { FoodTitle = "Unauthorized Modify Attempt" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "unauthorized-donor-id", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task CancelDonation_OtherDonor_IsRejectedWithPermissionError()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "legitimate-owner-id",
            DonorName = "Legitimate Owner",
            FoodTitle = "Original Meal",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "unauthorized-donor-id", "Unauthorized cancel");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task UpdateAvailability_OtherDonor_IsRejectedWithPermissionError()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "legitimate-owner-id",
            DonorName = "Legitimate Owner",
            FoodTitle = "Original Meal",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var dto = new UpdateAvailabilityDto { ExpiryHours = 12 };

        // Act
        var result = await service.UpdateAvailabilityAsync(donation.Id, "unauthorized-donor-id", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }
}
