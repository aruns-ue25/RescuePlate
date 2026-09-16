using FluentAssertions;
using DonationService.Models;

namespace DonationService.UnitTests;

public class ViewOwnDonationsUnitTests
{
    [Fact]
    public async Task GetMyDonations_ReturnsOnlyDonationsBelongingToAuthenticatedDonor()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        // Seed donations for Donor A and Donor B
        dbContext.Donations.AddRange(
            new Donation
            {
                DonorId = "donor-A",
                DonorName = "Donor A Business",
                FoodTitle = "Sandwiches A1",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(4),
                Status = "Posted"
            },
            new Donation
            {
                DonorId = "donor-A",
                DonorName = "Donor A Business",
                FoodTitle = "Salad A2",
                Category = "Fresh Produce",
                TotalQuantity = 15,
                RemainingQuantity = 15,
                ExpiryTime = DateTime.UtcNow.AddHours(5),
                Status = "Posted"
            },
            new Donation
            {
                DonorId = "donor-B",
                DonorName = "Donor B Business",
                FoodTitle = "Soup B1",
                Category = "Cooked Meals",
                TotalQuantity = 20,
                RemainingQuantity = 20,
                ExpiryTime = DateTime.UtcNow.AddHours(2),
                Status = "Posted"
            }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetMyDonationsAsync("donor-A");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);
        result.Data.Should().OnlyContain(d => d.DonorId == "donor-A");
        result.Data.Select(d => d.FoodTitle).Should().Contain(new[] { "Sandwiches A1", "Salad A2" });
        result.Data.Select(d => d.FoodTitle).Should().NotContain("Soup B1");
    }

    [Fact]
    public async Task GetMyDonations_WhenDonorHasNoDonations_ReturnsEmptyListSuccessfully()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        dbContext.Donations.Add(new Donation
        {
            DonorId = "other-donor",
            DonorName = "Other Business",
            FoodTitle = "Other Food",
            Category = "Bakery",
            TotalQuantity = 5,
            RemainingQuantity = 5,
            ExpiryTime = DateTime.UtcNow.AddHours(2),
            Status = "Posted"
        });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetMyDonationsAsync("donor-with-no-donations");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDonations_WithStatusAndSearchFilter_FiltersCorrectly()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        dbContext.Donations.AddRange(
            new Donation
            {
                DonorId = "donor-1",
                DonorName = "Donor 1",
                FoodTitle = "Vegan Pizza Slice",
                Category = "Cooked Meals",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                ExpiryTime = DateTime.UtcNow.AddHours(3),
                Status = "Posted"
            },
            new Donation
            {
                DonorId = "donor-1",
                DonorName = "Donor 1",
                FoodTitle = "Old Pastry",
                Category = "Bakery",
                TotalQuantity = 5,
                RemainingQuantity = 5,
                ExpiryTime = DateTime.UtcNow.AddHours(-1), // Past expiry
                Status = "Expired"
            }
        );
        await dbContext.SaveChangesAsync();

        // Act - Filter active + search "pizza"
        var result = await service.GetMyDonationsAsync("donor-1", status: "active", search: "pizza");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().FoodTitle.Should().Be("Vegan Pizza Slice");
    }
}
