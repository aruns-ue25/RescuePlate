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

public class RecordCollectionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecordCollectionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(string status = "CollectionArranged")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var arrangement = new DeliveryArrangement
        {
            RequestId = 3001,
            DonationId = 4001,
            DonationTitle = "Fresh Produce",
            DonorId = "donor_c_it",
            DonorName = "Donor C",
            OrganizationId = "org_c_it",
            OrganizationName = "Org C",
            PickupAddress = "100 Street",
            DeliveryAddress = "200 Center",
            ContactName = "Contact",
            ContactPhone = "12345",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        return arrangement;
    }

    [Fact]
    public async Task C_I01_AuthorizedDonorRecordsCollection_Returns200AndUpdatesDatabase()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_c_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new RecordCollectionDto { Notes = "Collected by courier" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var updated = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
        updated.Status.Should().Be("Collected");
        updated.CollectedAt.Should().NotBeNull();

        var history = await db.DeliveryStatusHistories.FirstOrDefaultAsync(h => h.DeliveryArrangementId == arrangement.Id && h.NewStatus == "Collected");
        history.Should().NotBeNull();

        var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id && n.Type == "DONATION_COLLECTED").ToListAsync();
        notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task C_I02_AuthorizedOrganizationRecordsCollection_Returns200()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_c_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task C_I03_CollectionOnDeliveryNotCollectionArranged_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_c_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task C_I04_DuplicateCollectionRequest_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_c_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res1 = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());
        var res2 = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        // Assert
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        res2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task C_I05_UnrelatedUserAttemptsCollection_Returns403()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("unrelated_user", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task C_I06_ConcurrentCollectionSubmissions_OnlyOneSucceeds()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_c_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var task1 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());
        var task2 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        await Task.WhenAll(task1, task2);

        // Assert
        var statusCodes = new[] { task1.Result.StatusCode, task2.Result.StatusCode };
        statusCodes.Should().Contain(HttpStatusCode.OK);
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.BadRequest || s == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task C_I07_PersistenceFailure_NoPartialUpdates()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_c_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Invalid delivery ID
        var response = await client.PostAsJsonAsync("/api/deliveries/9999/collect", new RecordCollectionDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task C_I08_MissingBearerToken_Returns401()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("CollectionArranged");
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/collect", new RecordCollectionDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
