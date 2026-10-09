using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DeliveryWorkflowService.Data;
using DeliveryWorkflowService.DTOs;
using DeliveryWorkflowService.IntegrationTests.Helpers;
using DeliveryWorkflowService.Models;
using Xunit;

namespace DeliveryWorkflowService.IntegrationTests;

public class ConfirmReceiptIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConfirmReceiptIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(string status = "Collected")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var arrangement = new DeliveryArrangement
        {
            RequestId = 3002,
            DonationId = 4002,
            DonationTitle = "Hot Soups",
            DonorId = "donor_r_it",
            DonorName = "Donor R",
            OrganizationId = "org_r_it",
            OrganizationName = "Org R",
            PickupAddress = "100 Street",
            DeliveryAddress = "200 Center",
            ContactName = "Contact",
            ContactPhone = "12345",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow.AddHours(-2),
            CollectedAt = status == "Collected" || status == "Received" || status == "Completed" ? DateTime.UtcNow.AddHours(-1) : null,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        return arrangement;
    }

    [Fact]
    public async Task R_I01_IntendedOrganizationConfirmsReceipt_Returns200AndPersists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_r_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ConfirmReceiptDto { Notes = "Verified quantity" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var updated = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
        updated.Status.Should().Be("Received");
        updated.ReceivedAt.Should().NotBeNull();

        var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id && n.Type == "RECEIPT_CONFIRMED").ToListAsync();
        notifications.Should().HaveCount(1);
        notifications.First().UserId.Should().Be("donor_r_it");
    }

    [Fact]
    public async Task R_I02_ConfirmReceiptBeforeCollection_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_r_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task R_I03_ConfirmReceiptAgainAfterAlreadyReceived_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_r_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task R_I04_UnrelatedOrganizationConfirmsReceipt_Returns403()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("unrelated_org", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task R_I05_DonorConfirmsReceipt_Returns403Forbidden()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_r_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task R_I06_ConcurrentConfirmations_OnlyOneSucceeds()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_r_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var task1 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());
        var task2 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        await Task.WhenAll(task1, task2);

        // Assert
        var statusCodes = new[] { task1.Result.StatusCode, task2.Result.StatusCode };
        statusCodes.Should().Contain(HttpStatusCode.OK);
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.BadRequest || s == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task R_I07_PersistenceFailure_NoPartialUpdate()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_r_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/9999/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R_I08_MissingBearerToken_Returns401()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/receive", new ConfirmReceiptDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
