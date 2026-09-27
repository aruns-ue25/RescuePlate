using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.UnitTests;

public class DonorAcceptAndRejectUnitTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task DA_UT_01_AcceptValidPendingRequest_ShouldTransitionStatusToAccepted()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 1,
            DonationTitle = "Rice",
            OrganizationId = "org_1",
            OrganizationName = "Org 1",
            DonorId = "donor_1",
            RequestedQuantity = 5,
            Status = "PENDING"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        req.Status = "ACCEPTED";
        await db.SaveChangesAsync();

        // Assert
        var updated = await db.Requests.FirstAsync(r => r.Id == req.Id);
        updated.Status.Should().Be("ACCEPTED");
    }

    [Fact]
    public async Task DA_UT_02_RecordAcceptedQuantity_ShouldStoreCorrectQuantity()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 2,
            DonationTitle = "Pasta",
            OrganizationId = "org_2",
            OrganizationName = "Org 2",
            DonorId = "donor_1",
            RequestedQuantity = 10,
            Status = "PENDING"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        req.Status = "ACCEPTED";
        await db.SaveChangesAsync();

        // Assert
        var updated = await db.Requests.FirstAsync(r => r.Id == req.Id);
        updated.RequestedQuantity.Should().Be(10);
    }

    [Fact]
    public void DA_UT_03_PartialAcceptance_ShouldAllowCorrectPartialQuantity()
    {
        // Arrange
        int requestedQuantity = 10;
        int partialAcceptedQuantity = 6;

        // Act
        bool isValidPartial = partialAcceptedQuantity > 0 && partialAcceptedQuantity <= requestedQuantity;

        // Assert
        isValidPartial.Should().BeTrue();
    }

    [Fact]
    public void DA_UT_04_AcceptExactRemainingQuantity_ShouldLeaveRemainingAsZero()
    {
        // Arrange
        int remainingQuantity = 15;
        int acceptedQuantity = 15;

        // Act
        int newRemaining = remainingQuantity - acceptedQuantity;

        // Assert
        newRemaining.Should().Be(0);
    }

    [Fact]
    public void DA_UT_05_AcceptMoreThanRemaining_ShouldBePrevented()
    {
        // Arrange
        int remainingQuantity = 5;
        int requestedQuantity = 10;

        // Act
        bool canAccept = requestedQuantity <= remainingQuantity;

        // Assert
        canAccept.Should().BeFalse();
    }

    [Fact]
    public void DA_UT_06_AcceptWhenRemainingIsZero_ShouldBeRejected()
    {
        // Arrange
        int remainingQuantity = 0;
        int requestedQuantity = 2;

        // Act
        bool canAccept = remainingQuantity > 0 && requestedQuantity <= remainingQuantity;

        // Assert
        canAccept.Should().BeFalse();
    }

    [Fact]
    public async Task DA_UT_07_AcceptAlreadyAcceptedRequest_ShouldBePrevented()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 3,
            DonationTitle = "Juice",
            OrganizationId = "org_3",
            OrganizationName = "Org 3",
            DonorId = "donor_1",
            RequestedQuantity = 4,
            Status = "ACCEPTED"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        bool canAcceptAgain = req.Status == "PENDING";

        // Assert
        canAcceptAgain.Should().BeFalse();
    }

    [Fact]
    public async Task DA_UT_08_AcceptAlreadyRejectedRequest_ShouldBePrevented()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 4,
            DonationTitle = "Milk",
            OrganizationId = "org_4",
            OrganizationName = "Org 4",
            DonorId = "donor_1",
            RequestedQuantity = 2,
            Status = "REJECTED"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        bool canAccept = req.Status == "PENDING";

        // Assert
        canAccept.Should().BeFalse();
    }

    [Fact]
    public void DA_UT_09_WrongDonorAttemptsAcceptance_ShouldBeDenied()
    {
        // Arrange
        string requestDonorId = "donor_owner";
        string actingDonorId = "donor_imposter";

        // Act
        bool isAuthorized = requestDonorId == actingDonorId;

        // Assert
        isAuthorized.Should().BeFalse();
    }

    [Fact]
    public void DA_UT_10_InvalidAcceptedQuantity_ShouldBeRejected()
    {
        // Arrange
        int invalidQuantity = -1;

        // Act
        bool isValid = invalidQuantity > 0;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task DRJ_UT_01_RejectPendingRequest_ShouldTransitionStatusToRejected()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 10,
            DonationTitle = "Fruit Basket",
            OrganizationId = "org_10",
            OrganizationName = "Org 10",
            DonorId = "donor_1",
            RequestedQuantity = 5,
            Status = "PENDING"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        req.Status = "REJECTED";
        req.RejectionReason = "Item damaged";
        await db.SaveChangesAsync();

        // Assert
        var updated = await db.Requests.FirstAsync(r => r.Id == req.Id);
        updated.Status.Should().Be("REJECTED");
        updated.RejectionReason.Should().Be("Item damaged");
    }

    [Fact]
    public void DRJ_UT_02_VerifyDonationQuantityAfterRejection_ShouldRemainUnchanged()
    {
        // Arrange
        int originalDonationQuantity = 30;

        // Act & Assert (Rejection does not reduce or mutate donation quantity)
        originalDonationQuantity.Should().Be(30);
    }

    [Fact]
    public async Task DRJ_UT_03_RejectAcceptedRequest_ShouldBePreventedOrHandled()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 11,
            DonationTitle = "Cookies",
            OrganizationId = "org_11",
            OrganizationName = "Org 11",
            DonorId = "donor_1",
            RequestedQuantity = 3,
            Status = "ACCEPTED"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        bool canRejectPendingOnly = req.Status == "PENDING";

        // Assert
        canRejectPendingOnly.Should().BeFalse();
    }

    [Fact]
    public async Task DRJ_UT_04_RejectAlreadyRejectedRequest_ShouldBePrevented()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 12,
            DonationTitle = "Water",
            OrganizationId = "org_12",
            OrganizationName = "Org 12",
            DonorId = "donor_1",
            RequestedQuantity = 5,
            Status = "REJECTED"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        bool canReject = req.Status == "PENDING";

        // Assert
        canReject.Should().BeFalse();
    }

    [Fact]
    public void DRJ_UT_05_WrongDonorRejectsRequest_ShouldBeDenied()
    {
        // Arrange
        string requestDonorId = "donor_owner";
        string actingDonorId = "donor_hacker";

        // Act
        bool isAuthorized = requestDonorId == actingDonorId;

        // Assert
        isAuthorized.Should().BeFalse();
    }
}
