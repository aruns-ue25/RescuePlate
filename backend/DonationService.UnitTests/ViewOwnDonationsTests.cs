using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DonationService.Models;

namespace DonationService.UnitTests;

public class ViewOwnDonationsTests : TestBase
{
    private void SeedDonations(Data.DonationDbContext context, string donorId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            context.Donations.Add(new Donation
            {
                DonorId = donorId,
                DonorName = "John Doe",
                FoodTitle = $"Test Food {i}",
                Category = "Bakery",
                TotalQuantity = 10,
                RemainingQuantity = 10,
                Unit = "portions",
                ExpiryTime = DateTime.UtcNow.AddHours(24),
                CollectionMode = "Organization Pickup",
                Location = "Location",
                Status = "Posted"
            });
        }
        context.SaveChanges();
    }

    [Fact]
    public async Task DS13_GetMyDonations_RetrievesOwnDonationsSuccessfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        SeedDonations(context, "donor-1", 3);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyDonationsAsync("donor-1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Count);
    }

    [Fact]
    public async Task DS14_GetMyDonations_ContainsCorrectStatusAndQuantity()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        SeedDonations(context, "donor-2", 1);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyDonationsAsync("donor-2");

        // Assert
        Assert.True(result.Success);
        var returnedDonation = result.Data!.First();
        // A newly created/seeded donation with no claims should map to Available
        Assert.Equal("Available", returnedDonation.Status);
        // The RemainingQuantity should match the TotalQuantity (10 from SeedDonations)
        Assert.Equal(10, returnedDonation.RemainingQuantity);
    }

    [Fact]
    public async Task DS15_GetMyDonations_CannotRetrieveOtherDonorsDonations()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        SeedDonations(context, "donor-a", 2);
        SeedDonations(context, "donor-b", 5);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyDonationsAsync("donor-a");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count); // Should only see their own 2 donations, not the other 5
        Assert.All(result.Data!, d => Assert.Equal("donor-a", d.DonorId));
    }

    [Fact]
    public async Task DS16_GetMyDonations_VerifiesDataIsRetrievedFromStore()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = GetInMemoryDbContext(dbName);
        
        var d1 = new Donation { DonorId = "d-1", DonorName = "D1", FoodTitle = "F1", ExpiryTime = DateTime.UtcNow.AddHours(2), Unit="kg", Location="L1" };
        context.Donations.Add(d1);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyDonationsAsync("d-1");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal("F1", result.Data![0].FoodTitle);
    }
}
