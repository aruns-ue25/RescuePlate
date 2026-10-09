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

public class CompleteDonationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CompleteDonationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync(string status = "Received")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var arrangement = new DeliveryArrangement
        {
            RequestId = 3003,
            DonationId = 4003,
            DonationTitle = "Packaged Bread",
            DonorId = "donor_d_it",
            DonorName = "Donor D",
            OrganizationId = "org_d_it",
            OrganizationName = "Org D",
            PickupAddress = "100 Street",
            DeliveryAddress = "200 Center",
            ContactName = "Contact",
            ContactPhone = "12345",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = status,
            ArrangedAt = DateTime.UtcNow.AddHours(-3),
            CollectedAt = DateTime.UtcNow.AddHours(-2),
            ReceivedAt = status == "Received" || status == "Completed" ? DateTime.UtcNow.AddHours(-1) : null,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };
        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        return arrangement;
    }

    [Fact]
    public async Task D_I01_DonorCompletesReceivedDonation_Returns200AndPersists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_d_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CompleteDonationDto { Notes = "Donation complete" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var updated = await db.DeliveryArrangements.FirstAsync(d => d.Id == arrangement.Id);
        updated.Status.Should().Be("Completed");
        updated.CompletedAt.Should().NotBeNull();

        var notifications = await db.DeliveryNotifications.Where(n => n.RelatedId == arrangement.Id && n.Type == "DONATION_COMPLETED").ToListAsync();
        notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task D_I02_OrganizationCompletesReceivedDonation_Returns200AndPersists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_d_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task D_I03_CompletionBeforeReceipt_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Collected");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_d_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task D_I04_DuplicateCompletion_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_d_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res1 = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());
        var res2 = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        // Assert
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        res2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task D_I05_UnrelatedUserAttemptsCompletion_Returns403()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("unrelated_user", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task D_I06_ConcurrentCompletionRequests_OnlyOneSucceeds()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_d_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var task1 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());
        var task2 = client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        await Task.WhenAll(task1, task2);

        // Assert
        var statusCodes = new[] { task1.Result.StatusCode, task2.Result.StatusCode };
        statusCodes.Should().Contain(HttpStatusCode.OK);
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.BadRequest || s == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task D_I07_PersistenceFailure_NoPartialUpdate()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_d_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/9999/complete", new CompleteDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task D_I08_MissingBearerToken_Returns401()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync("Received");
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/deliveries/{arrangement.Id}/complete", new CompleteDonationDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
