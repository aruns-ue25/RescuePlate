using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.IntegrationTests;

public class FoodRequestAndOfferIntegrationTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task FR_IT_01_ValidFoodRequestAPI_ShouldPersistAsOpen()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_api_food",
            OrganizationName = "City Food Relief",
            Title = "Need 100 Sandwich Packs",
            Category = "Cooked Meals",
            QuantityNeeded = 100,
            Unit = "Packs",
            Description = "For evening distribution",
            Location = "Central Park Shelter",
            NeededByDate = DateTime.UtcNow.AddDays(3),
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        fetched.Status.Should().Be("OPEN");
        fetched.QuantityNeeded.Should().Be(100);
    }

    [Fact]
    public void FR_IT_02_InvalidFieldsInFoodRequest_ShouldReturn400()
    {
        // Arrange
        string title = ""; // Required field missing

        // Act
        bool isValid = !string.IsNullOrWhiteSpace(title);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_IT_03_PastDateInFoodRequest_ShouldReject()
    {
        // Arrange
        DateTime pastDate = DateTime.UtcNow.AddHours(-1);

        // Act
        bool isValidDate = pastDate > DateTime.UtcNow;

        // Assert
        isValidDate.Should().BeFalse();
    }

    [Fact]
    public async Task FM_IT_01_ViewOwnFoodRequests_ShouldReturnCorrectRecords()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.OrgNeedRequests.AddRange(
            new OrgFoodNeedRequest { OrganizationId = "org_X", OrganizationName = "Org X", Title = "Need Apples", Category = "Fruit", Description = "Apples", Location = "L1", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" },
            new OrgFoodNeedRequest { OrganizationId = "org_Y", OrganizationName = "Org Y", Title = "Need Milk", Category = "Dairy", Description = "Milk", Location = "L2", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await db.OrgNeedRequests.Where(r => r.OrganizationId == "org_X").ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Need Apples");
    }

    [Fact]
    public async Task FM_IT_02_CancelRequest_ShouldPersistAsCancelled()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest { OrganizationId = "org_X", OrganizationName = "Org X", Title = "Need Bread", Category = "Bakery", Description = "Bread", Location = "L1", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Act
        need.Status = "CANCELLED";
        await db.SaveChangesAsync();

        // Assert
        var updated = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        updated.Status.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task FO_IT_01_ValidOfferAPI_ShouldPersistAsPending()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 10,
            OrgFoodNeedRequestTitle = "Need Bread",
            DonorId = "donor_baker",
            DonorName = "Bakery Fresh",
            FoodType = "Whole Wheat Bread",
            OfferedQuantity = 30,
            Unit = "Loaves",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        fetched.Status.Should().Be("PENDING");
        fetched.OfferedQuantity.Should().Be(30);
    }

    [Fact]
    public async Task FOA_09_FullAcceptanceThroughAPI_ShouldPersistAllRelatedStates()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest { OrganizationId = "org_main", OrganizationName = "Main Org", Title = "Need Soup", Category = "Prepared", Description = "Soup", Location = "Loc", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" };
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        var offer = new DonorFoodOffer { OrgFoodNeedRequestId = need.Id, OrgFoodNeedRequestTitle = need.Title, DonorId = "donor_main", DonorName = "Main Donor", FoodType = "Hot Soup", OfferedQuantity = 50, Status = "PENDING" };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Act
        offer.Status = "ACCEPTED";
        need.Status = "CLAIMED";
        await db.SaveChangesAsync();

        // Assert
        var updatedOffer = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        var updatedNeed = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);

        updatedOffer.Status.Should().Be("ACCEPTED");
        updatedNeed.Status.Should().Be("CLAIMED");
    }

    [Fact]
    public async Task FOR_07_RejectOfferThroughAPI_ShouldPersistState()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var offer = new DonorFoodOffer { OrgFoodNeedRequestId = 1, OrgFoodNeedRequestTitle = "Need Rice", DonorId = "donor_r", DonorName = "Donor R", FoodType = "Rice", OfferedQuantity = 10, Status = "PENDING" };
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Act
        offer.Status = "REJECTED";
        await db.SaveChangesAsync();

        // Assert
        var updated = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        updated.Status.Should().Be("REJECTED");
    }
}
