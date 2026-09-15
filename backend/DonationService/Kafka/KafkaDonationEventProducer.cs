using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DonationService.Events;

namespace DonationService.Kafka;

public class KafkaDonationEventProducer : IDonationEventProducer, IDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly string _topic;
    private readonly int _maxRetries;
    private readonly int _retryBackoffMs;
    private readonly ILogger<KafkaDonationEventProducer> _logger;
    private readonly bool _isEnabled;

    public KafkaDonationEventProducer(
        IConfiguration configuration,
        ILogger<KafkaDonationEventProducer> logger)
    {
        _logger = logger;

        var bootstrapServers =
            configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        _topic =
            configuration["Kafka:Topic"] ?? "donation-events";

        _maxRetries =
            int.TryParse(configuration["Kafka:PublishRetries"], out var retries)
                ? retries
                : 3;

        _retryBackoffMs =
            int.TryParse(configuration["Kafka:RetryBackoffMs"], out var backoff)
                ? backoff
                : 500;

        var securityProtocolValue =
            configuration["Kafka:SecurityProtocol"] ?? "Plaintext";

        var saslMechanismValue =
            configuration["Kafka:SaslMechanism"];

        var saslUsername =
            configuration["Kafka:SaslUsername"];

        var saslPassword =
            configuration["Kafka:SaslPassword"];

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = 3000,
                SocketTimeoutMs = 3000,

                SecurityProtocol =
                    Enum.Parse<SecurityProtocol>(
                        securityProtocolValue,
                        ignoreCase: true)
            };

            // Configure SASL only when it is provided.
            // Local Kafka uses Plaintext, so these remain empty locally.
            if (!string.IsNullOrWhiteSpace(saslMechanismValue))
            {
                config.SaslMechanism =
                    Enum.Parse<SaslMechanism>(
                        saslMechanismValue,
                        ignoreCase: true);
            }

            if (!string.IsNullOrWhiteSpace(saslUsername))
            {
                config.SaslUsername = saslUsername;
            }

            if (!string.IsNullOrWhiteSpace(saslPassword))
            {
                config.SaslPassword = saslPassword;
            }

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                {
                    if (error.IsFatal)
                    {
                        _logger.LogError(
                            "[KAFKA_PRODUCER_FATAL] {Reason}",
                            error.Reason);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "[KAFKA_PRODUCER_INFO] {Reason}",
                            error.Reason);
                    }
                })
                .Build();

            _isEnabled = true;

            _logger.LogInformation(
                "KafkaDonationEventProducer initialized. " +
                "Bootstrap servers: {Servers}, Topic: {Topic}, Security: {SecurityProtocol}",
                bootstrapServers,
                _topic,
                securityProtocolValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize Kafka Producer with bootstrap servers: {Servers}. " +
                "Event publishing will fail safely.",
                bootstrapServers);

            _isEnabled = false;
        }
    }

    public async Task<bool> PublishEventAsync(
        DonationEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producer == null)
        {
            _logger.LogWarning(
                "[KAFKA_PUBLISH_UNAVAILABLE] Kafka producer is not configured or disabled. " +
                "Event {EventType} (ID {DonationId}) was not published.",
                @event.EventType,
                @event.DonationId);

            return false;
        }

        var key = @event.DonationId.ToString();
        var payload = JsonSerializer.Serialize(@event);

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var message = new Message<string, string>
                {
                    // DonationId is used as the Kafka key
                    // to preserve ordering for the same donation.
                    Key = key,
                    Value = payload,
                    Timestamp = new Timestamp(@event.TimestampUtc)
                };

                var deliveryResult =
                    await _producer.ProduceAsync(
                        _topic,
                        message,
                        cancellationToken);

                _logger.LogInformation(
                    "[KAFKA_PUBLISHED] Event '{EventType}' for Donation #{DonationId} " +
                    "published to topic '{Topic}', partition {Partition}, offset {Offset}",
                    @event.EventType,
                    @event.DonationId,
                    deliveryResult.Topic,
                    deliveryResult.Partition.Value,
                    deliveryResult.Offset.Value);

                return true;
            }
            catch (ProduceException<string, string> pEx)
            {
                _logger.LogWarning(
                    pEx,
                    "[KAFKA_PUBLISH_RETRY] Attempt {Attempt}/{MaxRetries} failed to publish " +
                    "{EventType} for Donation #{DonationId}. Error: {Reason}",
                    attempt,
                    _maxRetries,
                    @event.EventType,
                    @event.DonationId,
                    pEx.Error.Reason);

                if (attempt < _maxRetries)
                {
                    await Task.Delay(
                        _retryBackoffMs * attempt,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[KAFKA_PUBLISH_RETRY] Attempt {Attempt}/{MaxRetries} unexpected error " +
                    "publishing {EventType} for Donation #{DonationId}",
                    attempt,
                    _maxRetries,
                    @event.EventType,
                    @event.DonationId);

                if (attempt < _maxRetries)
                {
                    await Task.Delay(
                        _retryBackoffMs * attempt,
                        cancellationToken);
                }
            }
        }

        _logger.LogError(
            "[KAFKA_PUBLISH_FAILED] Dual-write notice: Donation #{DonationId} " +
            "committed to DB, but event '{EventType}' failed to publish after {Retries} retries.",
            @event.DonationId,
            @event.EventType,
            _maxRetries);

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
            _logger.LogDebug(
                ex,
                "Error disposing Kafka producer.");
        }
    }
}