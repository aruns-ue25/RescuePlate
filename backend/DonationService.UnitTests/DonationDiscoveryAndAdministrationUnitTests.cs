using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DonationService.DTOs;
using DonationService.Models;
using Xunit;

namespace DonationService.UnitTests;

public class DonationDiscoveryAndAdministrationUnitTests
{
    [Fact]
    public async Task GetParticipatingDonorsAsync_WithActiveDonations_ReturnsAggregatedDonorStats()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var donation1 = new Donation
        {
            DonorId = "donor-bistro",
            DonorName = "Green Leaf Bistro",
            DonorEmail = "bistro@test.com",
            FoodTitle = "Daily Soup",
            Category = "Cooked Meals",
            TotalQuantity = 20,
            RemainingQuantity = 15,
            ClaimedQuantity = 5,
            Unit = "bowls",
            Status = "PartiallyClaimed",
            Location = "Downtown",
            Notes = "Freshly prepared",
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var donation2 = new Donation
        {
            DonorId = "donor-bakery",
            DonorName = "Bread Factory",
            DonorEmail = "bread@test.com",
            FoodTitle = "Baguettes",
            Category = "Bakery",
            TotalQuantity = 30,
            RemainingQuantity = 30,
            ClaimedQuantity = 0,
            Unit = "pieces",
            Status = "Available",
            Location = "Uptown",
            Notes = "Artisan bread",
            ExpiryTime = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        dbContext.Donations.AddRange(donation1, donation2);
        await dbContext.SaveChangesAsync();

        // Act - Query all donors
        var result = await service.GetParticipatingDonorsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);

        var bistro = result.Data.First(d => d.DonorId == "donor-bistro");
        bistro.BusinessName.Should().Be("Green Leaf Bistro");
        bistro.DonorType.Should().Be("Restaurant");
        bistro.TotalPortionsContributed.Should().Be(20);
        bistro.ActiveDonationsCount.Should().Be(1);

        var bakery = result.Data.First(d => d.DonorId == "donor-bakery");
        bakery.DonorType.Should().Be("Bakery");
        bakery.TotalPortionsContributed.Should().Be(30);

        // Act 2 - Filter by specific donorType
        var bakeryFilterResult = await service.GetParticipatingDonorsAsync(donorType: "Bakery");
        bakeryFilterResult.Success.Should().BeTrue();
        bakeryFilterResult.Data!.Should().HaveCount(1);
        bakeryFilterResult.Data!.First().DonorId.Should().Be("donor-bakery");
    }

    [Fact]
    public async Task GetDonorProfileDetailsAsync_ExistingDonor_ReturnsProfileWithActiveListings()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var donation = new Donation
        {
            DonorId = "donor-market",
            DonorName = "Central Market",
            DonorEmail = "market@test.com",
            FoodTitle = "Organic Apples",
            Category = "Fresh Produce",
            TotalQuantity = 50,
            RemainingQuantity = 50,
            ClaimedQuantity = 0,
            Unit = "kg",
            Status = "Available",
            Location = "Market Square",
            Notes = "Crunchy apples",
            ExpiryTime = DateTime.UtcNow.AddHours(12),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDonorProfileDetailsAsync("donor-market");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.DonorId.Should().Be("donor-market");
        result.Data.DonorType.Should().NotBeNullOrWhiteSpace();
        result.Data.TotalPortionsContributed.Should().Be(50);
        result.Data.ActiveListings.Should().HaveCount(1);
        result.Data.ActiveListings.First().FoodTitle.Should().Be("Organic Apples");
    }

    [Fact]
    public async Task GetDonorProfileDetailsAsync_NonExistentDonor_ReturnsFail()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act
        var result = await service.GetDonorProfileDetailsAsync("non-existent-donor-id");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found or has no public activity");
    }

    [Fact]
    public async Task GetParticipatingOrganizationsAsync_WithFilters_FiltersCorrectly()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act 1: Get all organizations
        var allResult = await service.GetParticipatingOrganizationsAsync();

        // Assert
        allResult.Success.Should().BeTrue();
        allResult.Data.Should().NotBeNull();
        allResult.Data!.Count.Should().BeGreaterThanOrEqualTo(3);

        // Act 2: Filter by search keyword
        var searchResult = await service.GetParticipatingOrganizationsAsync(search: "Colombo");
        searchResult.Success.Should().BeTrue();
        searchResult.Data!.Should().NotBeEmpty();
        searchResult.Data!.All(o => o.OrganizationName.Contains("Colombo", StringComparison.OrdinalIgnoreCase) || o.Location.Contains("Colombo", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();

        // Act 3: Filter by food category
        var categoryResult = await service.GetParticipatingOrganizationsAsync(foodCategory: "Bakery");
        categoryResult.Success.Should().BeTrue();
        categoryResult.Data!.Should().NotBeEmpty();
        categoryResult.Data!.All(o => o.AcceptedFoodTypes.Contains("Bakery")).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizationProfileDetailsAsync_ExistingAndNonExistent_HandledCorrectly()
    {
        // Arrange
        var (service, _, _) = TestHelpers.CreateDonationService();

        // Act 1: Existing organization
        var existingResult = await service.GetOrganizationProfileDetailsAsync("org-colombo-foodbank");

        // Assert
        existingResult.Success.Should().BeTrue();
        existingResult.Data.Should().NotBeNull();
        existingResult.Data!.OrganizationId.Should().Be("org-colombo-foodbank");

        // Act 2: Non-existent organization
        var nonExistentResult = await service.GetOrganizationProfileDetailsAsync("non-existent-org-999");
        nonExistentResult.Success.Should().BeFalse();
        nonExistentResult.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeductDonationQuantityAsync_ValidAndFullyClaimedTransition_Succeeds()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        var donation = new Donation
        {
            DonorId = "donor-10",
            DonorName = "Dine In",
            DonorEmail = "dine@test.com",
            FoodTitle = "Box Lunches",
            Category = "Cooked Meals",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            ClaimedQuantity = 0,
            Unit = "boxes",
            Status = "Available",
            Location = "Downtown",
            ExpiryTime = DateTime.UtcNow.AddHours(5),
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act 1: Partial deduction
        var partialResult = await service.DeductQuantityAsync(donation.Id, 4);

        // Assert partial
        partialResult.Success.Should().BeTrue();
        partialResult.Data!.RemainingQuantity.Should().Be(6);
        partialResult.Data.ClaimedQuantity.Should().Be(4);
        partialResult.Data.Status.Should().Be("PartiallyClaimed");

        // Act 2: Deduct remaining (fully claimed)
        var fullResult = await service.DeductQuantityAsync(donation.Id, 6);

        // Assert full
        fullResult.Success.Should().BeTrue();
        fullResult.Data!.RemainingQuantity.Should().Be(0);
        fullResult.Data.ClaimedQuantity.Should().Be(10);
        fullResult.Data.Status.Should().Be("FullyClaimed");

        // Act 3: Deduct more than available -> should fail
        var overResult = await service.DeductQuantityAsync(donation.Id, 1);
        overResult.Success.Should().BeFalse();
        overResult.Message.Should().Contain("cannot be requested/claimed");

        // Act 4: Non-existent donation
        var nonExistentResult = await service.DeductQuantityAsync(9999, 5);
        nonExistentResult.Success.Should().BeFalse();
        nonExistentResult.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetAdminDonationSummaryAsync_ReturnsAggregatedMetrics()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();

        dbContext.Donations.AddRange(
            new Donation
            {
                DonorId = "d1",
                DonorName = "D1",
                DonorEmail = "d1@test.com",
                FoodTitle = "Bread",
                Category = "Bakery",
                TotalQuantity = 20,
                RemainingQuantity = 10,
                ClaimedQuantity = 10,
                Unit = "pcs",
                Status = "PartiallyClaimed",
                Location = "City",
                ExpiryTime = DateTime.UtcNow.AddHours(3),
                CreatedAt = DateTime.UtcNow
            },
            new Donation
            {
                DonorId = "d2",
                DonorName = "D2",
                DonorEmail = "d2@test.com",
                FoodTitle = "Salad",
                Category = "Cooked Meals",
                TotalQuantity = 15,
                RemainingQuantity = 0,
                ClaimedQuantity = 0,
                Unit = "boxes",
                Status = "Cancelled",
                Location = "City",
                ExpiryTime = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetAdminDonationSummaryAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAvailableDonationsAsync_WhenDonorIsInactive_ExcludesDonationFromResults()
    {
        // Arrange
        var mockHttp = TestHelpers.CreateMockHttpClientFactory(inactiveUserIds: new List<string> { "inactive-donor-1" });
        var (service, dbContext, _) = TestHelpers.CreateDonationService(httpClientFactory: mockHttp);

        dbContext.Donations.AddRange(
            new Donation
            {
                DonorId = "inactive-donor-1",
                DonorName = "Inactive Bakery",
                FoodTitle = "Inactive Pastries",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                Status = "Available",
                ExpiryTime = DateTime.UtcNow.AddHours(5)
            },
            new Donation
            {
                DonorId = "active-donor-2",
                DonorName = "Active Diner",
                FoodTitle = "Active Meals",
                Category = "Cooked Meals",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                Status = "Available",
                ExpiryTime = DateTime.UtcNow.AddHours(5)
            }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetAllAvailableDonationsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().ContainSingle(d => d.DonorId == "active-donor-2");
        result.Data!.Should().NotContain(d => d.DonorId == "inactive-donor-1");
    }

    [Fact]
    public async Task GetDonationByIdAsync_WhenDonorIsInactive_ReturnsNotFound()
    {
        // Arrange
        var mockHttp = TestHelpers.CreateMockHttpClientFactory(inactiveUserIds: new List<string> { "inactive-donor-1" });
        var (service, dbContext, _) = TestHelpers.CreateDonationService(httpClientFactory: mockHttp);

        var donation = new Donation
        {
            DonorId = "inactive-donor-1",
            DonorName = "Inactive Bakery",
            FoodTitle = "Inactive Pastries",
            Category = "Bakery",
            TotalQuantity = 10,
            RemainingQuantity = 10,
            Status = "Available",
            ExpiryTime = DateTime.UtcNow.AddHours(5)
        };
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDonationByIdAsync(donation.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }
}
