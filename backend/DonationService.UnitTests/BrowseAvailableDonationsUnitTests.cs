using FluentAssertions;
using DonationService.Models;

namespace DonationService.UnitTests;

public class BrowseAvailableDonationsUnitTests
{
    [Fact]
    public async Task GetAllAvailableDonations_ExcludesCancelledAndExpiredDonations()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var validAvailable = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Fresh Bakery",
            FoodTitle = "Warm Buns",
            Category = "Bakery",
            TotalQuantity = 15,
            RemainingQuantity = 15,
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            Status = "Posted"
        };

        var cancelled = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Fresh Bakery",
            FoodTitle = "Cancelled Cake",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 0,
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            Status = "Cancelled"
        };

        var expired = new Donation
        {
            DonorId = "donor-2",
            DonorName = "City Cafe",
            FoodTitle = "Expired Soup",
            Category = "Cooked Meals",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(-1),
            Status = "Posted"
        };

        dbContext.Donations.AddRange(validAvailable, cancelled, expired);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetAllAvailableDonationsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().FoodTitle.Should().Be("Warm Buns");
    }

    [Fact]
    public async Task GetAllAvailableDonations_WithCategoryAndSearchFilter_FiltersCorrectly()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var bakery1 = new Donation
        {
            DonorId = "donor-1",
            DonorName = "Bakery One",
            FoodTitle = "Whole Wheat Loaves",
            Category = "Bakery",
            TotalQuantity = 20,
            RemainingQuantity = 20,
            ExpiryTime = DateTime.UtcNow.AddHours(6),
            Status = "Posted"
        };

        var meals1 = new Donation
        {
            DonorId = "donor-2",
            DonorName = "Restaurant Two",
            FoodTitle = "Vegetable Curry Rice",
            Category = "Cooked Meals",
            TotalQuantity = 30,
            RemainingQuantity = 30,
            ExpiryTime = DateTime.UtcNow.AddHours(6),
            Status = "Posted"
        };

        dbContext.Donations.AddRange(bakery1, meals1);
        await dbContext.SaveChangesAsync();

        // Act - Category Bakery
        var categoryResult = await service.GetAllAvailableDonationsAsync(category: "Bakery");

        // Assert Category Filter
        categoryResult.Success.Should().BeTrue();
        categoryResult.Data.Should().HaveCount(1);
        categoryResult.Data!.First().FoodTitle.Should().Be("Whole Wheat Loaves");

        // Act - Search "Curry"
        var searchResult = await service.GetAllAvailableDonationsAsync(search: "curry");

        // Assert Search Filter
        searchResult.Success.Should().BeTrue();
        searchResult.Data.Should().HaveCount(1);
        searchResult.Data!.First().FoodTitle.Should().Be("Vegetable Curry Rice");
    }

    [Fact]
    public async Task GetAllAvailableDonations_WhenNoMatchingAvailableDonationsExist_ReturnsEmptyList()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act
        var result = await service.GetAllAvailableDonationsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
