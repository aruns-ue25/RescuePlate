namespace RequestWorkflowService.Kafka;

public interface IRequestEventProducer
{
    Task<bool> PublishEventAsync<T>(string topic, string key, T @event, CancellationToken cancellationToken = default);
}
