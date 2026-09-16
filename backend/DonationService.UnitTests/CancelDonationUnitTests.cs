using FluentAssertions;
using DonationService.Models;

namespace DonationService.UnitTests;

public class CancelDonationUnitTests
{
    [Fact]
    public async Task CancelDonation_EligibleDonation_SucceedsAndUpdatesStatusAndRemainingQuantity()
    {
        // Arrange
        var (service, dbContext, eventProducerMock) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Surplus Muffins",
            Category = "Bakery",
            TotalQuantity = 20,
            RemainingQuantity = 20,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted",
            Notes = "Store pickup"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1", reason: "Store closing early");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("cancelled successfully");
        result.Data!.Status.Should().Be("Cancelled");
        result.Data.RemainingQuantity.Should().Be(0);
        result.Data.Notes.Should().Contain("[Cancelled by donor: Store closing early]");

        // Verify database persistence
        var dbEntity = await dbContext.Donations.FindAsync(donation.Id);
        dbEntity!.Status.Should().Be("Cancelled");
        dbEntity.RemainingQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CancelDonation_WhenAlreadyCancelled_FailsWithDuplicateCancellationMessage()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Already Cancelled Food",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Cancelled"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already cancelled");
    }

    [Fact]
    public async Task CancelDonation_WhenDonationIsCompleted_FailsWithWorkflowRestrictionMessage()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Completed Donation",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Completed"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Completed donations cannot be cancelled");
    }

    [Fact]
    public async Task CancelDonation_WhenCalledByNonOwnerDonor_FailsWithPermissionError()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-owner",
            DonorName = "Owner",
            FoodTitle = "Protected Food",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-other");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");

        var dbEntity = await dbContext.Donations.FindAsync(donation.Id);
        dbEntity!.Status.Should().Be("Posted");
    }

    [Fact]
    public async Task CancelDonation_WithNonExistentId_ReturnsNotFoundError()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act
        var result = await service.CancelDonationAsync(99999, "donor-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }
}
