using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DonationService.Models;
using DonationService.DTOs;

namespace DonationService.UnitTests;

public class AvailabilityExpiryTests : TestBase
{
    private Donation SeedDonation(Data.DonationDbContext context, string status = "Available", DateTime? expiryTime = null)
    {
        var donation = new Donation
        {
            DonorId = "donor-1",
            DonorName = "John Doe",
            FoodTitle = "Original Food",
            Category = "Bakery",
            TotalQuantity = 10,
            ClaimedQuantity = 0,
            RemainingQuantity = 10,
            Unit = "portions",
            ExpiryTime = expiryTime ?? DateTime.UtcNow.AddHours(24),
            CollectionMode = "Organization Pickup",
            Location = "Location",
            Status = status
        };
        context.Donations.Add(donation);
        context.SaveChanges();
        return donation;
    }

    [Fact]
    public async Task DS27_UpdateAvailability_Successfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        var dto = new UpdateAvailabilityDto { ExpiryHours = 48 };

        // Act
        var result = await service.UpdateAvailabilityAsync(donation.Id, "donor-1", dto);

        // Assert
        Assert.True(result.Success);
        
        var saved = context.Donations.Find(donation.Id);
        // Expiry should now be approx 48 hours from now
        Assert.True(saved.ExpiryTime > DateTime.UtcNow.AddHours(47)); 
    }

    [Fact]
    public async Task DS28_UpdateAvailability_Reject_InvalidUpdate()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context);
        var service = CreateService(context);

        // Act - Negative expiry hours or past date
        var dto = new UpdateAvailabilityDto { ExpiryHours = -5 };
        var result = await service.UpdateAvailabilityAsync(donation.Id, "donor-1", dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be greater than 0", result.Message);
        
        // Act - Past explicit date
        var dtoPast = new UpdateAvailabilityDto { ExpiryTime = DateTime.UtcNow.AddHours(-1) };
        var resultPast = await service.UpdateAvailabilityAsync(donation.Id, "donor-1", dtoPast);
        
        Assert.False(resultPast.Success);
        Assert.Contains("future", resultPast.Message);
    }

    [Fact]
    public async Task DS29_ProcessExpiredDonations_TransitionsToExpired()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        // Seed a donation with an expiry time in the past, but status still "Available"
        var donation = SeedDonation(context, "Available", DateTime.UtcNow.AddHours(-1));
        var service = CreateService(context);

        // Act
        var count = await service.ProcessExpiredDonationsAsync();

        // Assert
        Assert.Equal(1, count);
        var saved = context.Donations.Find(donation.Id);
        Assert.Equal("Expired", saved.Status);
    }

    [Fact]
    public async Task DS30_ExpiredDonation_CannotBeRequested_Or_IsExcluded()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        var donation = SeedDonation(context, "Expired", DateTime.UtcNow.AddHours(-2));
        var service = CreateService(context);

        // Act 1: Check availability list
        var availableResult = await service.GetAllAvailableDonationsAsync();
        
        // Act 2: Attempt to request it directly
        var claimDto = new ClaimRequestDto { Quantity = 2 };
        var claimResult = await service.RequestDonationAsync(donation.Id, "org-1", "Org Name", claimDto);

        // Assert
        // Should not be in available list
        Assert.True(availableResult.Success);
        Assert.DoesNotContain(availableResult.Data!, d => d.Id == donation.Id);
        
        // Cannot be claimed
        Assert.False(claimResult.Success);
        Assert.Contains("xpired", claimResult.Message.ToLower());
    }

    [Fact]
    public async Task DS31_UnavailableDonations_ExcludedFromAvailableResults()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        
        SeedDonation(context, "Available"); 
        SeedDonation(context, "Completed"); // Unavailable
        SeedDonation(context, "Cancelled"); // Unavailable
        SeedDonation(context, "Expired", DateTime.UtcNow.AddHours(-1)); // Unavailable
        
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAvailableDonationsAsync();

        // Assert
        Assert.True(result.Success);
        // Should only return the 1 "Available" donation out of the 4 seeded
        Assert.Single(result.Data!);
        Assert.Equal("Available", result.Data![0].Status);
    }
}
