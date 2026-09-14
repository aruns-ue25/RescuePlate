using System;
using System.Threading.Tasks;
using Xunit;
using DonationService.Models;

namespace DonationService.UnitTests;

public class CancelDonationTests : TestBase
{
    private Donation SeedDonation(Data.DonationDbContext context, string status = "Available", string donorId = "donor-1")
    {
        var donation = new Donation
        {
            DonorId = donorId,
            DonorName = "John Doe",
            FoodTitle = "Original Food",
            Category = "Bakery",
            TotalQuantity = 10,
            ClaimedQuantity = 0,
            RemainingQuantity = 10,
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
    public async Task DS23_CancelDonation_Successfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1", "No longer available");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task DS24_CancelDonation_SetsStatusToCancelled()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "donor-1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Cancelled", result.Data!.Status);
        Assert.Equal(0, result.Data!.RemainingQuantity); // Usually logic clears remaining
        
        var saved = context.Donations.Find(donation.Id);
        Assert.Equal("Cancelled", saved.Status);
    }

    [Fact]
    public async Task DS25_CancelDonation_PreventCancellingAnotherDonorsDonation()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, donorId: "donor-original");
        var service = CreateService(context);

        // Act
        var result = await service.CancelDonationAsync(donation.Id, "hacker-123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.Message.ToLower());
    }

    [Fact]
    public async Task DS26_CancelDonation_ExcludedFromAvailability()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, status: "Available"); // It is available initially
        var service = CreateService(context);

        // Act 1: Cancel
        await service.CancelDonationAsync(donation.Id, "donor-1");

        // Act 2: Fetch available donations
        var availableResult = await service.GetAllAvailableDonationsAsync();

        // Assert
        Assert.True(availableResult.Success);
        Assert.DoesNotContain(availableResult.Data!, d => d.Id == donation.Id);
    }
}
