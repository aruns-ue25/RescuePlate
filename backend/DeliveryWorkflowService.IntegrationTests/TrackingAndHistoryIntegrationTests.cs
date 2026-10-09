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

public class TrackingAndHistoryIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TrackingAndHistoryIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    private async Task<DeliveryArrangement> SeedArrangementAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var arrangement = new DeliveryArrangement
        {
            RequestId = 3005,
            DonationId = 4005,
            DonationTitle = "Fresh Milk Cartons",
            DonorId = "donor_t_it",
            DonorName = "Dairy Donor",
            OrganizationId = "org_t_it",
            OrganizationName = "Shelter Org",
            PickupAddress = "100 Street",
            DeliveryAddress = "200 Center",
            ContactName = "Contact",
            ContactPhone = "12345",
            ScheduledCollectionTime = DateTime.UtcNow.AddDays(1),
            Status = "CollectionArranged",
            ArrangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.DeliveryArrangements.Add(arrangement);
        await db.SaveChangesAsync();
        return arrangement;
    }

    [Fact]
    public async Task T_I01_DonorRetrievesTrackingByDeliveryId_Returns200AndMatchesData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_t_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/deliveries/{arrangement.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(arrangement.Id);
        body.Data.DonorId.Should().Be("donor_t_it");
    }

    [Fact]
    public async Task T_I02_OrganizationRetrievesTrackingByRequestId_Returns200AndMatchesData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("org_t_it", "ORGANIZATION");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/deliveries/request/{arrangement.RequestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        body.Should().NotBeNull();
        body!.Data!.RequestId.Should().Be(arrangement.RequestId);
    }

    [Fact]
    public async Task T_I03_AdminRetrievesTracking_Returns200()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("admin_user", "ADMIN");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/deliveries/{arrangement.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task T_I04_UnknownDeliveryOrRequestId_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("donor_t_it", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res1 = await client.GetAsync("/api/deliveries/9999");
        var res2 = await client.GetAsync("/api/deliveries/request/8888");

        // Assert
        res1.StatusCode.Should().Be(HttpStatusCode.NotFound);
        res2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T_I05_UnrelatedUserRequestsTracking_Returns403()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync();

        var client = _factory.CreateClient();
        var token = AuthTestHelper.GenerateJwtToken("unrelated_user", "DONOR");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/deliveries/{arrangement.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task T_I06_RunCompleteLifecycleAndRetrieveTracking_HistoryShowsAllTransitionsInOrder()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        _factory.SeedRequest(reqId: 5001, donationId: 6001, donorId: "donor_lifecycle", orgId: "org_lifecycle", status: "ACCEPTED");

        var donorClient = _factory.CreateClient();
        donorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("donor_lifecycle", "DONOR"));

        var orgClient = _factory.CreateClient();
        orgClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthTestHelper.GenerateJwtToken("org_lifecycle", "ORGANIZATION"));

        // 1. Arrange
        var arrangeDto = new ArrangeCollectionDto
        {
            RequestId = 5001,
            PickupAddress = "Addr",
            DeliveryAddress = "Addr",
            ContactName = "Name",
            ContactPhone = "123",
            ScheduledCollectionTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var arrangeRes = await donorClient.PostAsJsonAsync("/api/deliveries/arrange", arrangeDto);
        arrangeRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var arrangeBody = await arrangeRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        int deliveryId = arrangeBody!.Data!.Id;

        // 2. Collect
        var collectRes = await donorClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/collect", new RecordCollectionDto());
        collectRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Receive
        var receiveRes = await orgClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/receive", new ConfirmReceiptDto());
        receiveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Complete
        var completeRes = await donorClient.PostAsJsonAsync($"/api/deliveries/{deliveryId}/complete", new CompleteDonationDto());
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Retrieve Tracking
        var trackingRes = await donorClient.GetAsync($"/api/deliveries/{deliveryId}");
        trackingRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var trackingBody = await trackingRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryTrackingResponseDto>>();
        var data = trackingBody!.Data!;
        data.Status.Should().Be("Completed");
        data.StatusHistory.Should().HaveCount(4);
        data.StatusHistory[0].NewStatus.Should().Be("CollectionArranged");
        data.StatusHistory[1].NewStatus.Should().Be("Collected");
        data.StatusHistory[2].NewStatus.Should().Be("Received");
        data.StatusHistory[3].NewStatus.Should().Be("Completed");
    }

    [Fact]
    public async Task T_I07_MissingBearerToken_Returns401()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var arrangement = await SeedArrangementAsync();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/deliveries/{arrangement.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
