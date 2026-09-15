using System.Threading;
using System.Threading.Tasks;
using DonationService.Events;

namespace DonationService.Kafka;

public interface IDonationEventProducer
{
    Task<bool> PublishEventAsync(DonationEvent @event, CancellationToken cancellationToken = default);
}
