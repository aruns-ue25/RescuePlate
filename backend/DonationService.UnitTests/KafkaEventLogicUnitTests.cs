using System.Text.Json;
using FluentAssertions;
using Moq;
using DonationService.DTOs;
using DonationService.Events;

namespace DonationService.UnitTests;

public class KafkaEventLogicUnitTests
{
    [Fact]
    public async Task CreateDonation_PublishesDonationCreatedEventWithCorrectPayload()
    {
        // Arrange
        var (service, _, eventProducerMock) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Fresh Bread Rolls",
            Category = "Bakery",
            TotalQuantity = 20,
            Unit = "portions",
            ExpiryHours = 4.0
        };

        // Act
        var result = await service.CreateDonationAsync("donor-101", "City Bakery", "bakery@example.com", dto);

        // Assert
        result.Success.Should().BeTrue();

        eventProducerMock.Verify(
            p => p.PublishEventAsync(
                It.Is<DonationEvent>(e =>
                    e.EventType == "DonationCreated" &&
                    e.DonationId == result.Data!.Id &&
                    e.DonorId == "donor-101" &&
                    e.DonorName == "City Bakery" &&
                    e.FoodTitle == "Fresh Bread Rolls" &&
                    e.TotalQuantity == 20 &&
                    e.RemainingQuantity == 20 &&
                    e.Category == "Bakery"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateDonation_PublishesDonationUpdatedEventWithCorrectPayload()
    {
        // Arrange
        var (service, dbContext, eventProducerMock) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Original Bread",
            Category = "Bakery",
            TotalQuantity = 10,
            ExpiryHours = 5.0
        };

        var createResult = await service.CreateDonationAsync("donor-101", "City Bakery", "bakery@example.com", dto);
        eventProducerMock.Invocations.Clear();

        var updateDto = new UpdateDonationDto
        {
            FoodTitle = "Updated Bread Title",
            TotalQuantity = 15
        };

        // Act
        var result = await service.UpdateDonationAsync(createResult.Data!.Id, "donor-101", updateDto);

        // Assert
        result.Success.Should().BeTrue();

        eventProducerMock.Verify(
            p => p.PublishEventAsync(
                It.Is<DonationEvent>(e =>
                    e.EventType == "DonationUpdated" &&
                    e.DonationId == createResult.Data.Id &&
                    e.DonorId == "donor-101" &&
                    e.FoodTitle == "Updated Bread Title" &&
                    e.TotalQuantity == 15 &&
                    e.RemainingQuantity == 15
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task CancelDonation_PublishesDonationCancelledEventWithCorrectPayload()
    {
        // Arrange
        var (service, _, eventProducerMock) = TestHelpers.CreateDonationService();
        var dto = new CreateDonationDto
        {
            FoodTitle = "Soup Portion",
            Category = "Cooked Meals",
            TotalQuantity = 10,
            ExpiryHours = 5.0
        };

        var createResult = await service.CreateDonationAsync("donor-101", "City Diner", "diner@example.com", dto);
        eventProducerMock.Invocations.Clear();

        // Act
        var result = await service.CancelDonationAsync(createResult.Data!.Id, "donor-101", "Store closing");

        // Assert
        result.Success.Should().BeTrue();

        eventProducerMock.Verify(
            p => p.PublishEventAsync(
                It.Is<DonationEvent>(e =>
                    e.EventType == "DonationCancelled" &&
                    e.DonationId == createResult.Data.Id &&
                    e.DonorId == "donor-101" &&
                    e.FoodTitle == "Soup Portion" &&
                    e.RemainingQuantity == 0 &&
                    e.Status == "Cancelled"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void DonationEvent_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var originalEvent = new DonationEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "DonationCreated",
            TimestampUtc = DateTime.UtcNow,
            DonationId = 42,
            DonorId = "donor-999",
            DonorName = "Quality Bakery",
            DonorEmail = "info@qualitybakery.com",
            FoodTitle = "Croissants",
            Category = "Bakery",
            TotalQuantity = 100,
            RemainingQuantity = 100,
            Unit = "portions",
            Location = "Main St",
            Status = "Posted",
            ExpiryTime = DateTime.UtcNow.AddHours(4),
            DietaryTags = "Vegetarian",
            Notes = "Fresh out of oven"
        };

        // Act
        var json = JsonSerializer.Serialize(originalEvent);
        var deserializedEvent = JsonSerializer.Deserialize<DonationEvent>(json);

        // Assert
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.EventId.Should().Be(originalEvent.EventId);
        deserializedEvent.EventType.Should().Be("DonationCreated");
        deserializedEvent.DonationId.Should().Be(42);
        deserializedEvent.DonorId.Should().Be("donor-999");
        deserializedEvent.FoodTitle.Should().Be("Croissants");
        deserializedEvent.TotalQuantity.Should().Be(100);
        deserializedEvent.RemainingQuantity.Should().Be(100);
    }

    [Fact]
    public void MalformedJsonPayload_DeserializationReturnsNullOrThrowsSafely()
    {
        // Arrange
        var malformedJson = "{ invalid_json_payload: missing_quotes }";

        // Act
        Action act = () => JsonSerializer.Deserialize<DonationEvent>(malformedJson);

        // Assert
        act.Should().Throw<JsonException>();
    }
}
