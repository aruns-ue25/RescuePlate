using DonationService.Events;
using DonationService.Kafka;

namespace DonationService.IntegrationTests.Helpers;

public class TestDonationEventProducer : IDonationEventProducer
{
    public List<DonationEvent> PublishedEvents { get; } = new();

    public Task<bool> PublishEventAsync(DonationEvent @event, CancellationToken cancellationToken = default)
    {
        lock (PublishedEvents)
        {
            PublishedEvents.Add(@event);
        }
        return Task.FromResult(true);
    }

    public void Clear()
    {
        lock (PublishedEvents)
        {
            PublishedEvents.Clear();
        }
    }
}
