using FluentAssertions;
using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.UnitTests;

public class EditDonationUnitTests
{
    [Fact]
    public async Task UpdateDonation_OwnDonationWithValidMultiFieldData_SucceedsAndUpdatesDatabase()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Original Title",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            Unit = "portions",
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted",
            Notes = "Original notes"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto
        {
            FoodTitle = "Updated Title",
            Category = "Cooked Meals",
            TotalQuantity = 25,
            Unit = "meal boxes",
            Notes = "Updated notes",
            DietaryTags = "Halal"
        };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("updated successfully");
        result.Data!.FoodTitle.Should().Be("Updated Title");
        result.Data.Category.Should().Be("Cooked Meals");
        result.Data.TotalQuantity.Should().Be(25);
        result.Data.RemainingQuantity.Should().Be(25);
        result.Data.Unit.Should().Be("meal boxes");
        result.Data.Notes.Should().Be("Updated notes");
        result.Data.DietaryTags.Should().Be("Halal");

        // Verify entity state in database
        var updatedInDb = await dbContext.Donations.FindAsync(donation.Id);
        updatedInDb!.FoodTitle.Should().Be("Updated Title");
        updatedInDb.TotalQuantity.Should().Be(25);
    }

    [Fact]
    public async Task UpdateDonation_WhenDonorIsNotOwner_FailsWithPermissionErrorAndDoesNotModifyData()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-owner",
            DonorName = "Owner",
            FoodTitle = "Original Title",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { FoodTitle = "Hacked Title" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-attacker", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");

        var dbEntity = await dbContext.Donations.FindAsync(donation.Id);
        dbEntity!.FoodTitle.Should().Be("Original Title");
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public async Task UpdateDonation_WhenInRestrictedStatus_FailsAndPreservesOriginalData(string restrictedStatus)
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Original Title",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = restrictedStatus
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { FoodTitle = "New Title" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot be modified");

        var dbEntity = await dbContext.Donations.FindAsync(donation.Id);
        dbEntity!.FoodTitle.Should().Be("Original Title");
    }

    [Fact]
    public async Task UpdateDonation_WhenDonationIsExpired_FailsAndPreservesOriginalData()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Expired Bread",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(-2), // Expiry in past
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { FoodTitle = "New Title" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Expired donations cannot be modified");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdateDonation_WithZeroOrNegativeQuantity_FailsAndPreservesOriginalData(int invalidQty)
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Donor One",
            FoodTitle = "Original Title",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            Status = "Posted"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var updateDto = new UpdateDonationDto { TotalQuantity = invalidQty };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("greater than 0");

        var dbEntity = await dbContext.Donations.FindAsync(donation.Id);
        dbEntity!.TotalQuantity.Should().Be(10);
    }

    [Fact]
    public async Task UpdateDonation_WithNonExistentId_ReturnsNotFoundError()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();
        var updateDto = new UpdateDonationDto { FoodTitle = "Test" };

        // Act
        var result = await service.UpdateDonationAsync(99999, "donor-1", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }
}
