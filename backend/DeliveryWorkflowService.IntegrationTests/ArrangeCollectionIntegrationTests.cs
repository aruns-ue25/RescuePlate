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
using Xunit;

namespace DeliveryWorkflowService.IntegrationTests;

public class ArrangeCollectionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ArrangeCollectionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task A_I01_DonorArrangesCollectionForSeededAcceptedRequest_Returns201AndPersists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2001, donationId: 3001, donorId: "donor_it1", orgId: "org_it1", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it1", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2001,
            PickupAddress = "100 Bakery Rd",
            DeliveryAddress = "200 Community Center",
            ContactName = "Chef Mark",
            ContactPhone = "0771234567",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1),
            Notes = "Keep refrigerated"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be("CollectionArranged");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var arrangement = await db.DeliveryArrangements.Include(d => d.History).FirstOrDefaultAsync(d => d.RequestId == 2001);
        arrangement.Should().NotBeNull();
        arrangement!.Status.Should().Be("CollectionArranged");
        arrangement.History.Should().HaveCount(1);
    }

    [Fact]
    public async Task A_I02_IntendedOrganizationArrangesCollection_Returns201AndPersists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2002, donationId: 3002, donorId: "donor_it2", orgId: "org_it2", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_it2", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2002,
            PickupAddress = "100 Donor St",
            DeliveryAddress = "200 Org St",
            ContactName = "Sarah Org",
            ContactPhone = "0779876543",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(2)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var notifications = await db.DeliveryNotifications.ToListAsync();
        notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task A_I03_AttemptArrangementForPendingOrRejectedRequest_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2003, donationId: 3003, donorId: "donor_it3", orgId: "org_it3", status: "PENDING");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it3", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2003,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        (await db.DeliveryArrangements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_I04_AttemptArrangementForUnknownRequest_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 9999,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_I05_AttemptArrangementAsUnrelatedAuthenticatedUser_Returns403()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2005, donationId: 3005, donorId: "donor_it5", orgId: "org_it5", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("unrelated_user", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2005,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_I06_SubmitInvalidOrPastCollectionDetails_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2006, donationId: 3006, donorId: "donor_it6", orgId: "org_it6", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it6", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2006,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_I07_SubmitSameRequestTwice_SecondAttemptReturns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2007, donationId: 3007, donorId: "donor_it7", orgId: "org_it7", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it7", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2007,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var res1 = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);
        var res2 = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        res1.StatusCode.Should().Be(HttpStatusCode.Created);
        res2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        (await db.DeliveryArrangements.CountAsync(d => d.RequestId == 2007)).Should().Be(1);
    }

    [Fact]
    public async Task A_I08_AtomicTransactionIntegrity_AllOrNothingPersistence()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 2008, donationId: 3008, donorId: "donor_it8", orgId: "org_it8", status: "ACCEPTED");

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_it8", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2008,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        (await db.DeliveryArrangements.CountAsync()).Should().Be(1);
        (await db.DeliveryStatusHistories.CountAsync()).Should().Be(1);
        (await db.DeliveryNotifications.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task A_I09_SendRequestWithoutBearerToken_Returns401Unauthorized()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient(); // No auth header

        var dto = new ArrangeCollectionDto
        {
            RequestId = 2009,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/deliveries/arrange", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
