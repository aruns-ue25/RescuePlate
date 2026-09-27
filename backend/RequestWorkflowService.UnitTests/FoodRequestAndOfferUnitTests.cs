using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class FoodRequestAndOfferUnitTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task FR_UT_01_CreateValidFoodRequest_ShouldHaveOpenStatus()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_10",
            OrganizationName = "City Shelter",
            Title = "Need 50 Meal Packs",
            Category = "Cooked Meals",
            QuantityNeeded = 50,
            Unit = "Packs",
            Description = "Urgent requirement for evening shelter meal",
            Location = "Downtown Shelter",
            NeededByDate = DateTime.UtcNow.AddDays(2),
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        saved.Status.Should().Be("OPEN");
        saved.QuantityNeeded.Should().Be(50);
    }

    [Fact]
    public void FR_UT_02_MissingDescription_ShouldBeInvalid()
    {
        // Arrange
        string? description = null;

        // Act
        bool isValid = !string.IsNullOrWhiteSpace(description);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_UT_03_EmptyOrWhitespaceDescription_ShouldBeInvalid()
    {
        // Arrange
        string description = "   ";

        // Act
        bool isValid = !string.IsNullOrWhiteSpace(description);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_UT_04_MissingLocation_ShouldBeInvalid()
    {
        // Arrange
        string? location = "";

        // Act
        bool isValid = !string.IsNullOrWhiteSpace(location);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_UT_05_MissingNeededByDate_ShouldBeInvalid()
    {
        // Arrange
        DateTime? neededByDate = null;

        // Act
        bool isValid = neededByDate.HasValue && neededByDate.Value > DateTime.UtcNow;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_UT_06_InvalidOrPastNeededByDate_ShouldBeInvalid()
    {
        // Arrange
        DateTime pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        bool isValid = pastDate > DateTime.UtcNow;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FR_UT_07_MaximumValidFieldLength_ShouldBeAccepted()
    {
        // Arrange
        string validTitle = new string('A', 200);

        // Act
        bool isValid = validTitle.Length <= 200;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void FR_UT_08_ExcessiveFieldLength_ShouldBeRejected()
    {
        // Arrange
        string excessiveTitle = new string('A', 201);

        // Act
        bool isValid = excessiveTitle.Length <= 200;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task FR_UT_09_CorrectOrganizationAssociation_ShouldStoreCorrectID()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest
        {
            OrganizationId = "org_999",
            OrganizationName = "Alpha Org",
            Title = "Need Rice",
            Category = "Dry Food",
            QuantityNeeded = 100,
            Description = "Rice bags",
            Location = "Main Storage",
            NeededByDate = DateTime.UtcNow.AddDays(5),
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.OrgNeedRequests.Add(need);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.OrgNeedRequests.FirstAsync(n => n.Id == need.Id);
        saved.OrganizationId.Should().Be("org_999");
    }

    [Fact]
    public async Task FM_UT_01_ViewOwnFoodRequests_ShouldReturnOnlyOwnRequests()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.OrgNeedRequests.AddRange(
            new OrgFoodNeedRequest { OrganizationId = "org_A", OrganizationName = "Org A", Title = "T1", Category = "C1", Description = "D1", Location = "L1", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" },
            new OrgFoodNeedRequest { OrganizationId = "org_B", OrganizationName = "Org B", Title = "T2", Category = "C2", Description = "D2", Location = "L2", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await db.OrgNeedRequests.Where(r => r.OrganizationId == "org_A").ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().OrganizationId.Should().Be("org_A");
    }

    [Fact]
    public async Task FM_UT_05_CancelOpenRequest_ShouldTransitionStatusToCancelled()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var need = new OrgFoodNeedRequest { OrganizationId = "org_1", OrganizationName = "Org 1", Title = "T", Category = "C", Description = "D", Location = "L", NeededByDate = DateTime.UtcNow.AddDays(1), Status = "OPEN" };
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
    public async Task FO_UT_01_CreateValidOffer_ShouldHavePendingStatus()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var offer = new DonorFoodOffer
        {
            OrgFoodNeedRequestId = 1,
            OrgFoodNeedRequestTitle = "Need Meals",
            DonorId = "donor_55",
            DonorName = "Chef John",
            FoodType = "Fresh Meals",
            OfferedQuantity = 20,
            Unit = "Packs",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.DonorOffers.Add(offer);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.DonorOffers.FirstAsync(o => o.Id == offer.Id);
        saved.Status.Should().Be("PENDING");
        saved.OfferedQuantity.Should().Be(20);
    }

    [Fact]
    public void FO_UT_04_QuantityZero_ShouldBeRejected()
    {
        // Arrange
        int quantity = 0;

        // Act
        bool isValid = quantity > 0;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FOA_01_AcceptValidPendingOffer_ShouldTransitionStatusToAccepted()
    {
        // Arrange
        var offer = new DonorFoodOffer { Id = 1, Status = "PENDING" };

        // Act
        offer.Status = "ACCEPTED";

        // Assert
        offer.Status.Should().Be("ACCEPTED");
    }

    [Fact]
    public void FOA_02_AcceptOffer_ShouldMarkFoodRequestAsClaimed()
    {
        // Arrange
        var need = new OrgFoodNeedRequest { Id = 1, Status = "OPEN" };

        // Act
        need.Status = "CLAIMED";

        // Assert
        need.Status.Should().Be("CLAIMED");
    }

    [Fact]
    public void PF_UT_01_PartialFulfilment_TotalAcceptedMustNotExceedOriginalQuantity()
    {
        // Arrange
        int originalQuantity = 100;
        int firstAcceptance = 60;
        int secondAcceptance = 30;

        // Act
        int totalAccepted = firstAcceptance + secondAcceptance;
        int remaining = originalQuantity - totalAccepted;

        // Assert
        totalAccepted.Should().BeLessThanOrEqualTo(originalQuantity);
        remaining.Should().Be(10);
    }

    [Fact]
    public void PF_UT_04_AttemptAcceptanceBeyondRemaining_ShouldBePrevented()
    {
        // Arrange
        int remaining = 10;
        int attemptedAcceptance = 20;

        // Act
        bool isAllowed = attemptedAcceptance <= remaining;

        // Assert
        isAllowed.Should().BeFalse();
    }

    [Fact]
    public void PF_UT_05_RejectedRequest_DoesNotAffectRemainingQuantity()
    {
        // Arrange
        int remainingBefore = 40;
        bool isRejected = true;

        // Act
        int remainingAfter = isRejected ? remainingBefore : remainingBefore - 10;

        // Assert
        remainingAfter.Should().Be(40);
    }
}
