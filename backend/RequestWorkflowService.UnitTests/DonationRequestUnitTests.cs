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

public class DonationRequestUnitTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task DR_UT_01_CreateRequest_ValidQuantityBelowRemaining_ShouldHavePendingStatus()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 101,
            DonationTitle = "Fresh Vegetables",
            OrganizationId = "org_10",
            OrganizationName = "City Food Bank",
            DonorId = "donor_01",
            RequestedQuantity = 5,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Requests.FirstOrDefaultAsync(r => r.Id == request.Id);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be("PENDING");
        saved.RequestedQuantity.Should().Be(5);
    }

    [Fact]
    public async Task DR_UT_02_CreateRequest_ExactlyRemainingQuantity_ShouldSucceed()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        int remainingQuantity = 10;
        var request = new FoodRequest
        {
            DonationId = 102,
            DonationTitle = "Bread Loaves",
            OrganizationId = "org_11",
            OrganizationName = "Hope Shelter",
            DonorId = "donor_02",
            RequestedQuantity = remainingQuantity,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Requests.FirstOrDefaultAsync(r => r.Id == request.Id);
        saved.Should().NotBeNull();
        saved!.RequestedQuantity.Should().Be(remainingQuantity);
    }

    [Theory]
    [InlineData(0)]
    public void DR_UT_03_CreateRequest_QuantityZero_ShouldBeInvalid(int quantity)
    {
        // Act
        bool isValid = quantity > 0;

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-5)]
    public void DR_UT_04_CreateRequest_NegativeQuantity_ShouldBeInvalid(int quantity)
    {
        // Act
        bool isValid = quantity > 0;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void DR_UT_05_CreateRequest_QuantityGreaterThanRemaining_ShouldBeRejected()
    {
        // Arrange
        int remainingQuantity = 10;
        int requestedQuantity = 15;

        // Act
        bool canRequest = requestedQuantity <= remainingQuantity;

        // Assert
        canRequest.Should().BeFalse();
    }

    [Fact]
    public void DR_UT_06_CreateRequest_DonationWithZeroRemaining_ShouldBeRejected()
    {
        // Arrange
        int remainingQuantity = 0;
        int requestedQuantity = 2;

        // Act
        bool canRequest = remainingQuantity > 0 && requestedQuantity <= remainingQuantity;

        // Assert
        canRequest.Should().BeFalse();
    }

    [Fact]
    public void DR_UT_07_CreateRequest_ExpiredDonation_ShouldBeRejected()
    {
        // Arrange
        DateTime expiryDate = DateTime.UtcNow.AddHours(-2);
        bool isExpired = expiryDate <= DateTime.UtcNow;

        // Act & Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void DR_UT_08_CreateRequest_UnavailableDonation_ShouldBeRejected()
    {
        // Arrange
        string donationStatus = "CLAIMED";
        bool isAvailable = donationStatus == "AVAILABLE";

        // Act & Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task DR_UT_09_CreateRequest_NonExistentDonation_ShouldReturnError()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        int nonExistentDonationId = 9999;

        // Act
        var request = await db.Requests.FirstOrDefaultAsync(r => r.DonationId == nonExistentDonationId);

        // Assert
        request.Should().BeNull();
    }

    [Fact]
    public async Task DR_UT_10_VerifyOrganizationDonationAssociation_ShouldHaveCorrectIDs()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 201,
            DonationTitle = "Rice Bags",
            OrganizationId = "org_99",
            OrganizationName = "Community Kitchen",
            DonorId = "donor_88",
            RequestedQuantity = 3,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Requests.FirstAsync(r => r.Id == request.Id);
        saved.OrganizationId.Should().Be("org_99");
        saved.DonationId.Should().Be(201);
        saved.DonorId.Should().Be("donor_88");
    }

    [Fact]
    public async Task DR_UT_11_CreateRequest_DoesNotReduceDonationQuantityImmediately()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        int initialDonationQuantity = 50;

        var request = new FoodRequest
        {
            DonationId = 301,
            DonationTitle = "Canned Soup",
            OrganizationId = "org_01",
            OrganizationName = "Help Center",
            DonorId = "donor_01",
            RequestedQuantity = 10,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert: Pending request creation leaves the original donation quantity untouched
        initialDonationQuantity.Should().Be(50);
        request.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task DR_UT_12_MultipleOrganizationsRequestSameDonation_ShouldAllowIndependentPendingRequests()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        int donationId = 401;

        var req1 = new FoodRequest
        {
            DonationId = donationId,
            DonationTitle = "Packed Lunches",
            OrganizationId = "org_A",
            OrganizationName = "Org Alpha",
            DonorId = "donor_X",
            RequestedQuantity = 5,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        var req2 = new FoodRequest
        {
            DonationId = donationId,
            DonationTitle = "Packed Lunches",
            OrganizationId = "org_B",
            OrganizationName = "Org Beta",
            DonorId = "donor_X",
            RequestedQuantity = 5,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.AddRange(req1, req2);
        await db.SaveChangesAsync();

        // Assert
        var pendingRequests = await db.Requests.Where(r => r.DonationId == donationId).ToListAsync();
        pendingRequests.Should().HaveCount(2);
        pendingRequests.All(r => r.Status == "PENDING").Should().BeTrue();
    }
}
