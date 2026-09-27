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

public class OrganizationAndDonorViewUnitTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task OR_UT_01_ViewOwnSubmittedRequests_ShouldReturnOnlyOwnRequests()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest { DonationId = 1, DonationTitle = "Apples", OrganizationId = "org_100", OrganizationName = "Org 100", DonorId = "donor_1", RequestedQuantity = 5, Status = "PENDING" },
            new FoodRequest { DonationId = 2, DonationTitle = "Oranges", OrganizationId = "org_100", OrganizationName = "Org 100", DonorId = "donor_2", RequestedQuantity = 3, Status = "ACCEPTED" },
            new FoodRequest { DonationId = 3, DonationTitle = "Bananas", OrganizationId = "org_200", OrganizationName = "Org 200", DonorId = "donor_1", RequestedQuantity = 2, Status = "PENDING" }
        );
        await db.SaveChangesAsync();

        // Act
        var result = await db.Requests.Where(r => r.OrganizationId == "org_100").ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.OrganizationId == "org_100").Should().BeTrue();
    }

    [Fact]
    public async Task OR_UT_02_ViewWhenNoRequestsExist_ShouldHandleEmptyResult()
    {
        // Arrange
        using var db = GetInMemoryDbContext();

        // Act
        var result = await db.Requests.Where(r => r.OrganizationId == "org_empty").ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task OR_UT_03_VerifyPendingStatus_ShouldReflectCorrectStatus()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest { DonationId = 10, DonationTitle = "Juice", OrganizationId = "org_1", OrganizationName = "Org 1", DonorId = "d_1", RequestedQuantity = 4, Status = "PENDING" };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        var fetched = await db.Requests.FirstAsync(r => r.Id == req.Id);

        // Assert
        fetched.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task OR_UT_04_VerifyAcceptedStatusAndQuantity_ShouldReturnCorrectQuantity()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest { DonationId = 11, DonationTitle = "Milk", OrganizationId = "org_1", OrganizationName = "Org 1", DonorId = "d_1", RequestedQuantity = 10, Status = "ACCEPTED" };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        var fetched = await db.Requests.FirstAsync(r => r.Id == req.Id);

        // Assert
        fetched.Status.Should().Be("ACCEPTED");
        fetched.RequestedQuantity.Should().Be(10);
    }

    [Fact]
    public async Task OR_UT_05_VerifyRejectedStatus_ShouldReflectCorrectStatus()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest { DonationId = 12, DonationTitle = "Bread", OrganizationId = "org_1", OrganizationName = "Org 1", DonorId = "d_1", RequestedQuantity = 2, Status = "REJECTED", RejectionReason = "Out of stock" };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        var fetched = await db.Requests.FirstAsync(r => r.Id == req.Id);

        // Assert
        fetched.Status.Should().Be("REJECTED");
        fetched.RejectionReason.Should().Be("Out of stock");
    }

    [Fact]
    public async Task DRV_UT_01_ViewRequestsForDonorsDonations_ShouldReturnCorrectRequests()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest { DonationId = 50, DonationTitle = "Meal Boxes", OrganizationId = "org_A", OrganizationName = "Org A", DonorId = "donor_target", RequestedQuantity = 10, Status = "PENDING" },
            new FoodRequest { DonationId = 51, DonationTitle = "Water Bottles", OrganizationId = "org_B", OrganizationName = "Org B", DonorId = "donor_other", RequestedQuantity = 20, Status = "PENDING" }
        );
        await db.SaveChangesAsync();

        // Act
        var donorRequests = await db.Requests.Where(r => r.DonorId == "donor_target").ToListAsync();

        // Assert
        donorRequests.Should().HaveCount(1);
        donorRequests.First().DonorId.Should().Be("donor_target");
    }

    [Fact]
    public async Task DRV_UT_02_DonorHasMultipleDonations_ShouldGroupOrReturnCorrectly()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.Requests.AddRange(
            new FoodRequest { DonationId = 101, DonationTitle = "Salad", OrganizationId = "org_A", OrganizationName = "Org A", DonorId = "donor_multi", RequestedQuantity = 5, Status = "PENDING" },
            new FoodRequest { DonationId = 102, DonationTitle = "Sandwiches", OrganizationId = "org_B", OrganizationName = "Org B", DonorId = "donor_multi", RequestedQuantity = 8, Status = "PENDING" }
        );
        await db.SaveChangesAsync();

        // Act
        var requests = await db.Requests.Where(r => r.DonorId == "donor_multi").ToListAsync();

        // Assert
        requests.Should().HaveCount(2);
        requests.Select(r => r.DonationId).Should().BeEquivalentTo(new[] { 101, 102 });
    }

    [Fact]
    public async Task DRV_UT_03_DonorHasNoRequests_ShouldReturnEmptyList()
    {
        // Arrange
        using var db = GetInMemoryDbContext();

        // Act
        var requests = await db.Requests.Where(r => r.DonorId == "donor_none").ToListAsync();

        // Assert
        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DRV_UT_04_VerifyOrganizationRequestQuantityDetails_ShouldContainCorrectInfo()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var req = new FoodRequest
        {
            DonationId = 300,
            DonationTitle = "Soup Cans",
            OrganizationId = "org_detail",
            OrganizationName = "Detail Charity",
            DonorId = "donor_detail",
            RequestedQuantity = 15,
            Unit = "Cans",
            Notes = "Urgent need",
            Status = "PENDING"
        };
        db.Requests.Add(req);
        await db.SaveChangesAsync();

        // Act
        var fetched = await db.Requests.FirstAsync(r => r.Id == req.Id);

        // Assert
        fetched.OrganizationName.Should().Be("Detail Charity");
        fetched.RequestedQuantity.Should().Be(15);
        fetched.Unit.Should().Be("Cans");
        fetched.Notes.Should().Be("Urgent need");
    }
}
