using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DonationService.DTOs;

namespace DonationService.UnitTests;

public class DonationCreationUnitTests
{
    [Fact]
    public async Task CreateDonation_WithValidInformation_SucceedsAndPersistsCorrectData()
    {
        // Arrange
        var (service, dbContext, eventProducerMock) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Freshly Baked Bread Rolls",
            Category = "Bakery",
            TotalQuantity = 50,
            Unit = "meal boxes",
            ExpiryHours = 6.0,
            Notes = "Contains gluten and wheat",
            DietaryTags = "Vegetarian"
        };

        // Act
        var result = await service.CreateDonationAsync("donor-101", "City Bakery", "bakery@example.com", dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("posted successfully");
        result.Data.Should().NotBeNull();

        // Verify DTO response properties
        var response = result.Data!;
        response.Id.Should().BeGreaterThan(0);
        response.DonorId.Should().Be("donor-101");
        response.DonorName.Should().Be("City Bakery");
        response.DonorEmail.Should().Be("bakery@example.com");
        response.FoodTitle.Should().Be("Freshly Baked Bread Rolls");
        response.Category.Should().Be("Bakery");
        response.TotalQuantity.Should().Be(50);
        response.RemainingQuantity.Should().Be(50);
        response.Unit.Should().Be("meal boxes");
        response.Status.Should().Be("Available");
        response.Location.Should().Be("123 Test Street, Colombo");
        response.Notes.Should().Be("Contains gluten and wheat");
        response.DietaryTags.Should().Be("Vegetarian");

        // Verify database persistence
        var persistedEntity = await dbContext.Donations.FindAsync(response.Id);
        persistedEntity.Should().NotBeNull();
        persistedEntity!.DonorId.Should().Be("donor-101");
        persistedEntity.TotalQuantity.Should().Be(50);
        persistedEntity.RemainingQuantity.Should().Be(50);
        persistedEntity.Status.Should().Be("Posted");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateDonation_WithZeroOrNegativeQuantity_FailsAndDoesNotCreateRecord(int invalidQuantity)
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Rice & Curry",
            Category = "Cooked Meals",
            TotalQuantity = invalidQuantity,
            ExpiryHours = 4.0
        };

        // Act
        var result = await service.CreateDonationAsync("donor-101", "City Kitchen", "kitchen@example.com", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Quantity must be a positive number greater than 0");
        (await dbContext.Donations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateDonation_WithPastExpiryTime_FailsAndDoesNotCreateRecord()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Sandwiches",
            Category = "Bakery",
            TotalQuantity = 10,
            ExpiryTime = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = await service.CreateDonationAsync("donor-101", "City Cafe", "cafe@example.com", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("must be in the future");
        (await dbContext.Donations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateDonation_WithMissingFoodTitle_FailsAndDoesNotCreateRecord()
    {
        // Arrange
        var (service, dbContext, _) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "   ",
            Category = "Cooked Meals",
            TotalQuantity = 20,
            ExpiryHours = 2.0
        };

        // Act
        var result = await service.CreateDonationAsync("donor-101", "City Cafe", "cafe@example.com", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("title is required");
        (await dbContext.Donations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateDonation_WithUnresolvableLocation_FailsAndDoesNotCreateRecord()
    {
        // Arrange
        var mockHttpFactory = TestHelpers.CreateMockHttpClientFactory(address: null, statusCode: System.Net.HttpStatusCode.NotFound);
        var (service, dbContext, _) = TestHelpers.CreateDonationService(httpClientFactory: mockHttpFactory);

        var dto = new CreateDonationDto
        {
            FoodTitle = "Packaged Biscuits",
            Category = "Packaged Dry",
            TotalQuantity = 10,
            ExpiryHours = 24.0
        };

        // Act
        var result = await service.CreateDonationAsync("donor-no-address", "Unknown Business", "email@example.com", dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("location could not be found");
        (await dbContext.Donations.CountAsync()).Should().Be(0);
    }
}
