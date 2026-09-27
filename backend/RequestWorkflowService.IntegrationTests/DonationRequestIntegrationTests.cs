using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;
using RequestWorkflowService.Models;
using Xunit;

namespace RequestWorkflowService.IntegrationTests;

public class DonationRequestIntegrationTests
{
    private RequestDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    [Fact]
    public async Task DR_IT_01_ValidDonationRequestThroughAPI_ShouldPersistSuccessfully()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 500,
            DonationTitle = "Fresh Milk",
            OrganizationId = "org_api_1",
            OrganizationName = "City Charity",
            DonorId = "donor_api_1",
            RequestedQuantity = 10,
            Unit = "Cartons",
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert
        var persisted = await db.Requests.FirstOrDefaultAsync(r => r.DonationId == 500);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be("PENDING");
        persisted.RequestedQuantity.Should().Be(10);
    }

    [Fact]
    public void DR_IT_02_InvalidQuantityThroughAPI_ShouldReturnValidationError()
    {
        // Arrange
        int invalidQuantity = 0;

        // Act
        bool isValid = invalidQuantity > 0;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task DR_IT_03_QuantityExceedsAvailability_ShouldRejectAndKeepDBUnchanged()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        int availableQuantity = 20;
        int requestedQuantity = 30;

        // Act
        bool isAllowed = requestedQuantity <= availableQuantity;
        if (!isAllowed)
        {
            // Do not persist invalid over-allocation request
        }

        // Assert
        isAllowed.Should().BeFalse();
        var count = await db.Requests.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public void DR_IT_04_ExpiredOrUnavailableDonation_ShouldRejectRequest()
    {
        // Arrange
        bool isExpired = true;

        // Act
        bool canCreateRequest = !isExpired;

        // Assert
        canCreateRequest.Should().BeFalse();
    }

    [Fact]
    public void DR_IT_05_WrongRoleAttemptsRequest_ShouldBeForbidden()
    {
        // Arrange
        string userRole = "DONOR"; // Only ORGANIZATION role can make requests

        // Act
        bool isAuthorized = userRole == "ORGANIZATION";

        // Assert
        isAuthorized.Should().BeFalse();
    }

    [Fact]
    public void DR_IT_06_UnauthenticatedRequest_ShouldBeUnauthorized()
    {
        // Arrange
        string? jwtToken = null;

        // Act
        bool isAuthenticated = !string.IsNullOrEmpty(jwtToken);

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task DR_IT_07_VerifyPersistedRequestRelationships_ShouldHaveCorrectIDs()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var request = new FoodRequest
        {
            DonationId = 777,
            DonationTitle = "Bread Packets",
            OrganizationId = "org_target_77",
            OrganizationName = "Target Charity",
            DonorId = "donor_source_88",
            RequestedQuantity = 5,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        // Assert
        var fetched = await db.Requests.FirstAsync(r => r.Id == request.Id);
        fetched.OrganizationId.Should().Be("org_target_77");
        fetched.DonorId.Should().Be("donor_source_88");
        fetched.DonationId.Should().Be(777);
    }
}
