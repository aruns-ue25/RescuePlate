using System.Text.Json;
using Confluent.Kafka;

namespace RequestWorkflowService.Kafka;

public class KafkaRequestEventProducer : IRequestEventProducer, IDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly int _maxRetries;
    private readonly int _retryBackoffMs;
    private readonly ILogger<KafkaRequestEventProducer> _logger;
    private readonly bool _isEnabled;

    public KafkaRequestEventProducer(
        IConfiguration configuration,
        ILogger<KafkaRequestEventProducer> logger)
    {
        _logger = logger;

        _isEnabled = configuration.GetValue<bool>("Kafka:EnableProducer", true);

        _maxRetries = int.TryParse(configuration["Kafka:PublishRetries"], out var retries)
            ? Math.Max(1, retries)
            : 3;

        _retryBackoffMs = int.TryParse(configuration["Kafka:RetryBackoffMs"], out var backoff)
            ? Math.Max(100, backoff)
            : 500;

        if (!_isEnabled)
        {
            _logger.LogInformation("KafkaRequestEventProducer is disabled via configuration.");
            _producer = null;
            return;
        }

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var securityProtocolValue = configuration["Kafka:SecurityProtocol"] ?? "Plaintext";
        var saslMechanismValue = configuration["Kafka:SaslMechanism"];
        var saslUsername = configuration["Kafka:SaslUsername"];
        var saslPassword = configuration["Kafka:SaslPassword"];

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = 5000,
                RequestTimeoutMs = 5000
            };

            if (string.Equals(securityProtocolValue, "SASL_SSL", StringComparison.OrdinalIgnoreCase))
            {
                config.SecurityProtocol = SecurityProtocol.SaslSsl;
            }
            else if (Enum.TryParse<SecurityProtocol>(securityProtocolValue, true, out var secProtocol))
            {
                config.SecurityProtocol = secProtocol;
            }

            if (!string.IsNullOrWhiteSpace(saslMechanismValue) &&
                Enum.TryParse<SaslMechanism>(saslMechanismValue, true, out var saslMech))
            {
                config.SaslMechanism = saslMech;
            }

            if (!string.IsNullOrWhiteSpace(saslUsername))
            {
                config.SaslUsername = saslUsername;
            }

            if (!string.IsNullOrWhiteSpace(saslPassword))
            {
                config.SaslPassword = saslPassword;
            }

            _logger.LogInformation(
                "Kafka producer configured: BootstrapServers='{BootstrapServers}', SecurityProtocol={SecurityProtocol}, SaslMechanism={SaslMechanism}",
                bootstrapServers,
                config.SecurityProtocol,
                config.SaslMechanism);

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger.LogInformation("KafkaRequestEventProducer initialized successfully for bootstrap servers '{BootstrapServers}'.", bootstrapServers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Kafka producer. Service will operate in offline/degraded event mode.");
            _producer = null;
        }
    }

    public async Task<bool> PublishEventAsync<T>(string topic, string key, T @event, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producer == null)
        {
            _logger.LogDebug("Kafka producer disabled or unavailable. Skipping event publishing to topic '{Topic}'.", topic);
            return false;
        }

        string jsonPayload;
        try
        {
            jsonPayload = JsonSerializer.Serialize(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize event object of type {Type} for topic '{Topic}'.", typeof(T).Name, topic);
            return false;
        }

        var message = new Message<string, string>
        {
            Key = key,
            Value = jsonPayload
        };

        int attempt = 0;
        while (attempt < _maxRetries)
        {
            attempt++;
            try
            {
                var result = await _producer.ProduceAsync(topic, message, cancellationToken);
                if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
                {
                    _logger.LogInformation(
                        "[KAFKA_PUBLISH_SUCCESS] Published {EventType} to topic '{Topic}' [Partition {Partition} @ Offset {Offset}] on attempt {Attempt}/{MaxRetries}",
                        typeof(T).Name, topic, result.Partition.Value, result.Offset.Value, attempt, _maxRetries);
                    return true;
                }
            }
            catch (ProduceException<string, string> pEx)
            {
                _logger.LogWarning(
                    "[KAFKA_PUBLISH_RETRY] Attempt {Attempt}/{MaxRetries} failed to publish {EventType} to topic '{Topic}'. Error: {Reason}",
                    attempt, _maxRetries, typeof(T).Name, topic, pEx.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[KAFKA_PUBLISH_RETRY] Attempt {Attempt}/{MaxRetries} failed to publish {EventType} to topic '{Topic}'. Error: {Message}",
                    attempt, _maxRetries, typeof(T).Name, topic, ex.Message);
            }

            if (attempt < _maxRetries)
            {
                await Task.Delay(_retryBackoffMs, cancellationToken);
            }
        }

        _logger.LogError(
            "[KAFKA_PUBLISH_FINAL_FAILURE] All {MaxRetries} publish attempts failed for event {EventType} to topic '{Topic}'. The business transaction has completed successfully and was not affected.",
            _maxRetries, typeof(T).Name, topic);

        return false;
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(2));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing Kafka producer.");
        }
    }
}
