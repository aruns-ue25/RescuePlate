using FluentAssertions;
using DonationService.Models;

namespace DonationService.UnitTests;

public class DonationDetailsUnitTests
{
    [Fact]
    public async Task GetDonationById_ExistingDonation_ReturnsCompleteAndCorrectDetails()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Grand Hotel",
            DonorEmail = "hotel@example.com",
            FoodTitle = "Buffet Surplus Trays",
            Category = "Cooked Meals",
            TotalQuantity = 40,
            RemainingQuantity = 40,
            Unit = "kg",
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            Status = "Posted",
            Location = "Main Galle Road, Colombo",
            Notes = "Keep refrigerated",
            DietaryTags = "Halal, Non-Veg"
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDonationByIdAsync(donation.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var dto = result.Data!;
        dto.Id.Should().Be(donation.Id);
        dto.DonorId.Should().Be("donor-1");
        dto.DonorName.Should().Be("Grand Hotel");
        dto.DonorEmail.Should().Be("hotel@example.com");
        dto.FoodTitle.Should().Be("Buffet Surplus Trays");
        dto.Category.Should().Be("Cooked Meals");
        dto.TotalQuantity.Should().Be(40);
        dto.RemainingQuantity.Should().Be(40);
        dto.Unit.Should().Be("kg");
        dto.Status.Should().Be("Available");
        dto.Location.Should().Be("Main Galle Road, Colombo");
        dto.Notes.Should().Be("Keep refrigerated");
        dto.DietaryTags.Should().Be("Halal, Non-Veg");
        dto.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GetDonationById_ExpiredDonation_IdentifiesAsExpiredInStatusAndFlag()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var expiredDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Grand Hotel",
            FoodTitle = "Past Deadline Salad",
            Category = "Fresh Produce",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(-1),
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

    [Fact]
    public async Task GetDonationById_CancelledDonation_ReturnsCancelledStatusAndZeroRemainingQuantity()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var cancelledDonation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Grand Hotel",
            FoodTitle = "Cancelled Dessert",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(3),
            Status = "Cancelled"
        };
        dbContext.Donations.Add(cancelledDonation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDonationByIdAsync(cancelledDonation.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Cancelled");
        result.Data.RemainingQuantity.Should().Be(0);
    }

    [Fact]
    public async Task GetDonationById_NonExistentId_ReturnsNotFoundError()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act
        var result = await service.GetDonationByIdAsync(9999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }
}
