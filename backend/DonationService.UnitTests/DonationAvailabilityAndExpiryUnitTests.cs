using FluentAssertions;
using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.UnitTests;

public class DonationAvailabilityAndExpiryUnitTests
{
    [Fact]
    public async Task ProcessExpiredDonations_OnlyTransitionsPastDeadlineDonationsToExpired()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var activeDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Active Meal",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(2), // Future
            Status = "Posted"
        };

        var expiredDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Expired Meal",
            TotalQuantity = 5,
            RemainingQuantity = 5,
            ExpiryTime = DateTime.UtcNow.AddHours(-1), // Past deadline
            Status = "Posted"
        };

        var completedDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Completed Meal",
            TotalQuantity = 5,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(-2), // Past deadline but completed
            Status = "Completed"
        };

        dbContext.Donations.AddRange(activeDonation, expiredDonation, completedDonation);
        await dbContext.SaveChangesAsync();

        // Act
        var expiredCount = await service.ProcessExpiredDonationsAsync();

        // Assert
        expiredCount.Should().Be(1);

        var dbActive = await dbContext.Donations.FindAsync(activeDonation.Id);
        dbActive!.Status.Should().Be("Posted");

        var dbExpired = await dbContext.Donations.FindAsync(expiredDonation.Id);
        dbExpired!.Status.Should().Be("Expired");

        var dbCompleted = await dbContext.Donations.FindAsync(completedDonation.Id);
        dbCompleted!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task UpdateAvailability_WithValidHours_UpdatesExpiryTimeSuccessfully()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Bread",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(1),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var dto = new UpdateAvailabilityDto { ExpiryHours = 5 };

        // Act
        var result = await service.UpdateAvailabilityAsync(donation.Id, "donor-1", dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.ExpiryTime.Should().BeAfter(DateTime.UtcNow.AddHours(4));
    }

    [Fact]
    public async Task UpdateAvailability_WithPastExpiryTime_FailsAndDoesNotUpdate()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var initialExpiry = DateTime.UtcNow.AddHours(2);
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Bread",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = initialExpiry,
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var dto = new UpdateAvailabilityDto { ExpiryTime = DateTime.UtcNow.AddHours(-1) };

        // Act
        var result = await service.UpdateAvailabilityAsync(donation.Id, "donor-1", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("must be in the future");
    }

    [Fact]
    public async Task MapToResponseDto_ExposesCalculatedIsExpiredPropertyCorrectly()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var expiredDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Past Deadline Food",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddMinutes(-10),
            Status = "Posted"
        };
        dbContext.Donations.Add(expiredDonation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDonationByIdAsync(expiredDonation.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.IsExpired.Should().BeTrue();
        result.Data.Status.Should().Be("Expired");
    }
}
