using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Data;

namespace RequestWorkflowService.Kafka;

public class RequestEventConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestEventConsumerService> _logger;

    public RequestEventConsumerService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<RequestEventConsumerService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var isEnabled = _configuration.GetValue<bool>("Kafka:EnableConsumer", true);
        if (!isEnabled)
        {
            _logger.LogInformation("RequestEventConsumerService is disabled via configuration.");
            return;
        }

        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var topic = _configuration["Kafka:DonationTopic"] ?? "donation-events";
        var groupId = _configuration["Kafka:ConsumerGroupId"] ?? "rescueplate-requestworkflow-consumer-group";
        var securityProtocolValue = _configuration["Kafka:SecurityProtocol"] ?? "Plaintext";
        var saslMechanismValue = _configuration["Kafka:SaslMechanism"];
        var saslUsername = _configuration["Kafka:SaslUsername"];
        var saslPassword = _configuration["Kafka:SaslPassword"];

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 10000,
            MaxPollIntervalMs = 300000
        };

        if (Enum.TryParse<SecurityProtocol>(securityProtocolValue, true, out var secProtocol))
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

        await Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                {
                    if (error.IsFatal)
                    {
                        _logger.LogError("[KAFKA_CONSUMER_FATAL] {Reason}", error.Reason);
                    }
                    else
                    {
                        _logger.LogDebug("[KAFKA_CONSUMER_NOTICE] {Reason}", error.Reason);
                    }
                })
                .Build();

            try
            {
                consumer.Subscribe(topic);
                _logger.LogInformation("RequestEventConsumerService subscribed to Kafka topic '{Topic}' (Group: '{GroupId}').", topic, groupId);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
                        if (consumeResult == null || consumeResult.IsPartitionEOF)
                        {
                            continue;
                        }

                        ProcessEventAsync(consumeResult.Message.Key, consumeResult.Message.Value, consumeResult.TopicPartitionOffset).GetAwaiter().GetResult();

                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException cEx)
                    {
                        _logger.LogDebug("Kafka consume transient notice: {Reason}", cEx.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error in RequestEventConsumerService message loop.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RequestEventConsumerService cancelled gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RequestEventConsumerService stopped unexpectedly or broker not reachable. Service will remain safe.");
            }
            finally
            {
                try
                {
                    consumer.Close();
                }
                catch (Exception closeEx)
                {
                    _logger.LogDebug(closeEx, "Error closing Kafka consumer.");
                }
            }
        }, stoppingToken);
    }

    private async Task ProcessEventAsync(string key, string jsonValue, TopicPartitionOffset tpo)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonValue);
            var root = doc.RootElement;

            string eventType = string.Empty;
            if (root.TryGetProperty("EventType", out var eventTypeProp))
            {
                eventType = eventTypeProp.GetString() ?? string.Empty;
            }

            int donationId = 0;
            if (root.TryGetProperty("DonationId", out var donationIdProp) && donationIdProp.ValueKind == JsonValueKind.Number)
            {
                donationId = donationIdProp.GetInt32();
            }

            _logger.LogInformation("[KAFKA_CONSUMER_RECEIVED] EventType: '{EventType}' | DonationId: {DonationId} at offset {Offset}", eventType, donationId, tpo.Offset.Value);

            if ((eventType == "DonationCancelled" || eventType == "DonationExpired") && donationId > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();

                var pendingRequests = await dbContext.Requests
                    .Where(r => r.DonationId == donationId && r.Status == "PENDING")
                    .ToListAsync();

                if (pendingRequests.Count > 0)
                {
                    foreach (var req in pendingRequests)
                    {
                        req.Status = "EXPIRED";
                        req.UpdatedAt = DateTime.UtcNow;
                    }

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("[KAFKA_CONSUMER_UPDATED] Marked {Count} pending requests as EXPIRED for Donation {DonationId}", pendingRequests.Count, donationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming event payload from topic at offset {Offset}", tpo.Offset.Value);
        }
    }
}
