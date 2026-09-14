using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DonationService.Models;
using DonationService.DTOs;

namespace DonationService.UnitTests;

public class UpdateDonationTests : TestBase
{
    private Donation SeedDonation(Data.DonationDbContext context, string status = "Available", int total = 10, int claimed = 0, string donorId = "donor-1")
    {
        var donation = new Donation
        {
            DonorId = donorId,
            DonorName = "John Doe",
            FoodTitle = "Original Food",
            Category = "Bakery",
            TotalQuantity = total,
            ClaimedQuantity = claimed,
            RemainingQuantity = total - claimed,
            Unit = "portions",
            ExpiryTime = DateTime.UtcNow.AddHours(24),
            CollectionMode = "Organization Pickup",
            Location = "Location",
            Status = status
        };
        context.Donations.Add(donation);
        context.SaveChanges();
        return donation;
    }

    [Fact]
    public async Task DS17_UpdateDonation_Successfully_WithValidData()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        var updateDto = new UpdateDonationDto
        {
            FoodTitle = "Updated Title",
            TotalQuantity = 20
        };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated Title", result.Data!.FoodTitle);
        Assert.Equal(20, result.Data!.TotalQuantity);
    }

    [Fact]
    public async Task DS18_UpdateDonation_VerifyPersisted()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        var updateDto = new UpdateDonationDto { Location = "New Location" };

        // Act
        await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        var saved = context.Donations.Find(donation.Id);
        Assert.Equal("New Location", saved.Location);
    }

    [Fact]
    public async Task DS19_UpdateDonation_Reject_InvalidData()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        // Try updating to past expiry
        var updateDto = new UpdateDonationDto
        {
            ExpiryTime = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be in the future", result.Message);
        
        // Assert it was not persisted
        var saved = context.Donations.Find(donation.Id);
        Assert.True(saved.ExpiryTime > DateTime.UtcNow); // original is 24 hours ahead
    }

    [Fact]
    public async Task DS20_UpdateDonation_PreventEditingAnotherDonorsDonation()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, donorId: "donor-original");
        var service = CreateService(context);

        var updateDto = new UpdateDonationDto { FoodTitle = "Hacked Title" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "hacker-123", updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.Message.ToLower());
    }

    [Fact]
    public async Task DS21_UpdateDonation_PreventEditingWhenStatusNotAllowed()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, status: "Cancelled");
        var service = CreateService(context);

        var updateDto = new UpdateDonationDto { FoodTitle = "Will Not Work" };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be modified", result.Message);
    }

    [Fact]
    public async Task DS22_UpdateDonation_MaintainsQuantityIntegrity()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, total: 10, claimed: 4); // Remaining should be 6
        var service = CreateService(context);

        var updateDto = new UpdateDonationDto { TotalQuantity = 15 };

        // Act
        var result = await service.UpdateDonationAsync(donation.Id, "donor-1", updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15, result.Data!.TotalQuantity); // Updated
        Assert.Equal(4, result.Data!.ClaimedQuantity); // Same
        Assert.Equal(11, result.Data!.RemainingQuantity); // 15 - 4
        
        // Check database directly
        var saved = context.Donations.Find(donation.Id);
        Assert.Equal(11, saved.RemainingQuantity);
    }
}
