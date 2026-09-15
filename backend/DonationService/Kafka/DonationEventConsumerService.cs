using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DonationService.Events;

namespace DonationService.Kafka;

public class DonationEventConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DonationEventConsumerService> _logger;

    public DonationEventConsumerService(
        IConfiguration configuration,
        ILogger<DonationEventConsumerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var isEnabled =
            _configuration.GetValue<bool>(
                "Kafka:EnableConsumer",
                true);

        if (!isEnabled)
        {
            _logger.LogInformation(
                "DonationEventConsumerService is disabled via configuration.");

            return;
        }

        var bootstrapServers =
            _configuration["Kafka:BootstrapServers"]
            ?? "localhost:9092";

        var topic =
            _configuration["Kafka:Topic"]
            ?? "donation-events";

        var groupId =
            _configuration["Kafka:ConsumerGroupId"]
            ?? "rescueplate-donation-consumers";

        var securityProtocolValue =
            _configuration["Kafka:SecurityProtocol"]
            ?? "Plaintext";

        var saslMechanismValue =
            _configuration["Kafka:SaslMechanism"];

        var saslUsername =
            _configuration["Kafka:SaslUsername"];

        var saslPassword =
            _configuration["Kafka:SaslPassword"];

        try
        {
            // Give the application a few seconds to start.
            await Task.Delay(3000, stoppingToken);

            _logger.LogInformation(
                "DonationEventConsumerService starting. " +
                "Group: '{GroupId}', Topic: '{Topic}', Servers: '{Servers}', Security: '{SecurityProtocol}'",
                groupId,
                topic,
                bootstrapServers,
                securityProtocolValue);

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,

                // Read messages from the beginning if this is a new consumer group.
                AutoOffsetReset = AutoOffsetReset.Earliest,

                // We manually commit after processing.
                EnableAutoCommit = false,

                SocketTimeoutMs = 5000,
                SessionTimeoutMs = 10000,

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

            await Task.Run(
                () =>
                {
                    using var consumer =
                        new ConsumerBuilder<string, string>(config)
                            .SetErrorHandler((_, error) =>
                            {
                                if (error.IsFatal)
                                {
                                    _logger.LogError(
                                        "[KAFKA_CONSUMER_FATAL] {Reason}",
                                        error.Reason);
                                }
                                else
                                {
                                    _logger.LogDebug(
                                        "[KAFKA_CONSUMER_NOTICE] {Reason}",
                                        error.Reason);
                                }
                            })
                            .Build();

                    try
                    {
                        consumer.Subscribe(topic);

                        _logger.LogInformation(
                            "Subscribed to Kafka topic '{Topic}'. " +
                            "Waiting for donation events...",
                            topic);

                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                var consumeResult =
                                    consumer.Consume(
                                        TimeSpan.FromMilliseconds(500));

                                if (consumeResult == null ||
                                    consumeResult.IsPartitionEOF)
                                {
                                    continue;
                                }

                                ProcessEvent(
                                    consumeResult.Message.Key,
                                    consumeResult.Message.Value,
                                    consumeResult.TopicPartitionOffset);

                                // Commit only after the event has been processed.
                                consumer.Commit(consumeResult);
                            }
                            catch (ConsumeException cEx)
                            {
                                _logger.LogDebug(
                                    "Kafka consume transient notice: {Reason}",
                                    cEx.Error.Reason);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Unexpected error in Kafka consumer message loop.");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation(
                            "Kafka consumer cancelled gracefully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Kafka consumer stopped unexpectedly or broker not reachable. " +
                            "Service will remain safe.");
                    }
                    finally
                    {
                        try
                        {
                            consumer.Close();
                        }
                        catch (Exception closeEx)
                        {
                            _logger.LogDebug(
                                closeEx,
                                "Error closing Kafka consumer.");
                        }
                    }
                },
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "DonationEventConsumerService startup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "DonationEventConsumerService failed to start. " +
                "Service will remain safe.");
        }
    }

    private void ProcessEvent(
        string key,
        string jsonValue,
        TopicPartitionOffset tpo)
    {
        try
        {
            var donationEvent =
                JsonSerializer.Deserialize<DonationEvent>(
                    jsonValue);

            if (donationEvent == null)
            {
                _logger.LogWarning(
                    "[KAFKA_CONSUMER] Received null or invalid JSON payload " +
                    "at offset {Offset}",
                    tpo.Offset.Value);

                return;
            }

            switch (donationEvent.EventType)
            {
                case "DonationCreated":

                    _logger.LogInformation(
                        "[KAFKA CONSUMER - NEW DONATION BROADCAST] " +
                        "EventId: {EventId} | Donation #{DonationId} - '{FoodTitle}' ({Category}) | " +
                        "Quantity: {TotalQty} {Unit} | Location: '{Location}' | " +
                        "Donor: '{DonorName}' | Expires: {ExpiryUtc:u} | " +
                        "Ready for downstream notification to registered charities.",
                        donationEvent.EventId,
                        donationEvent.DonationId,
                        donationEvent.FoodTitle,
                        donationEvent.Category,
                        donationEvent.TotalQuantity,
                        donationEvent.Unit,
                        donationEvent.Location,
                        donationEvent.DonorName,
                        donationEvent.ExpiryTime);

                    break;

                case "DonationUpdated":

                    _logger.LogInformation(
                        "[KAFKA CONSUMER - DONATION UPDATED] " +
                        "EventId: {EventId} | Donation #{DonationId} - '{FoodTitle}' | " +
                        "Available Quantity: {RemainingQty} / {TotalQty} {Unit} | " +
                        "Status: '{Status}' | " +
                        "Updated parameters broadcasted asynchronously.",
                        donationEvent.EventId,
                        donationEvent.DonationId,
                        donationEvent.FoodTitle,
                        donationEvent.RemainingQuantity,
                        donationEvent.TotalQuantity,
                        donationEvent.Unit,
                        donationEvent.Status);

                    break;

                case "DonationCancelled":

                    _logger.LogInformation(
                        "[KAFKA CONSUMER - DONATION CANCELLED] " +
                        "EventId: {EventId} | Donation #{DonationId} - " +
                        "'{FoodTitle}' cancelled by Donor #{DonorId}. " +
                        "Inventory removed from active listings.",
                        donationEvent.EventId,
                        donationEvent.DonationId,
                        donationEvent.FoodTitle,
                        donationEvent.DonorId);

                    break;

                default:

                    _logger.LogInformation(
                        "[KAFKA CONSUMER - GENERIC EVENT] " +
                        "EventType: '{EventType}', Donation #{DonationId} " +
                        "at offset {Offset}",
                        donationEvent.EventType,
                        donationEvent.DonationId,
                        tpo.Offset.Value);

                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[KAFKA_CONSUMER] Failed to deserialize or process event payload " +
                "at offset {Offset}. Payload: {Payload}",
                tpo.Offset.Value,
                jsonValue);
        }
    }
}