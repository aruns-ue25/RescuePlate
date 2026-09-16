using FluentAssertions;
using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.UnitTests;

public class DonationWorkflowStateUnitTests
{
    [Fact]
    public async Task NewlyCreatedDonation_ReceivesPostedStatus_AndMapsToAvailableDtoStatus()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Fresh Bread",
            Category = "Bakery",
            TotalQuantity = 10,
            ExpiryHours = 4.0
        };

        // Act
        var result = await service.CreateDonationAsync("donor-1", "Bakery", "b@example.com", dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Available");

        var dbItem = await dbContext.Donations.FindAsync(result.Data.Id);
        dbItem!.Status.Should().Be("Posted");
    }

    [Fact]
    public async Task ExpiryProcessing_TransitionsPostedDonationToExpiredStatus()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Bakery",
            FoodTitle = "Overnight Donuts",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddMinutes(-5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var processedCount = await service.ProcessExpiredDonationsAsync();

        // Assert
        processedCount.Should().Be(1);
        var dbItem = await dbContext.Donations.FindAsync(donation.Id);
        dbItem!.Status.Should().Be("Expired");
    }

    [Fact]
    public async Task Cancellation_TransitionsPostedDonationToCancelledStatus_AndSetsRemainingQuantityToZero()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Bakery",
            FoodTitle = "Whole Loaf",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Cancelled");
        result.Data.RemainingQuantity.Should().Be(0);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public async Task InvalidStateTransition_ModifyingTerminalStatus_IsRejected(string terminalStatus)
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Bakery",
            FoodTitle = "Terminal Item",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            Status = terminalStatus
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { FoodTitle = "Attempted Transition" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot be modified");
    }
}
