using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DonationService.Data;
using DonationService.IntegrationTests.Helpers;

namespace DonationService.IntegrationTests;

[Collection("DonationIntegrationTests")]
public class DatabaseAndSetupIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DatabaseAndSetupIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Service_CanStartWithTestConfiguration_AndHealthEndpointReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("RescuePlate.DonationService");
        content.Should().Contain("Healthy");
        content.Should().Contain("PostgreSQL");
    }

    [Fact]
    public async Task DbContext_CanConnectToDedicatedTestPostgresDb_AndSchemaIsCreated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DonationDbContext>();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();
        var count = await dbContext.Donations.CountAsync();

        // Assert
        canConnect.Should().BeTrue();
        count.Should().Be(0);
        _factory.ConnectionString.Should().Contain("RescuePlate_Donation_IntegrationTestDB");
    }

    [Fact]
    public async Task ResetDatabaseAsync_ClearsDonationRecordsSuccessfully()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            db.Donations.Add(new Models.Donation
            {
                DonorId = "temp-donor",
                DonorName = "Temp Donor",
                FoodTitle = "Temp Food",
                Category = "Bakery",
                TotalQuantity = 5,
                RemainingQuantity = 5,
                ExpiryTime = DateTime.UtcNow.AddHours(2),
                Status = "Posted"
            });
            await db.SaveChangesAsync();
        }

        // Act
        await _factory.ResetDatabaseAsync();

        // Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
            var count = await db.Donations.CountAsync();
            count.Should().Be(0);
        }
    }
}
