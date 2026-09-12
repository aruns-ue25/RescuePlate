using DonationService.Services;

namespace DonationService.Services;

/// <summary>
/// Background worker that runs periodically to automatically transition food donations
/// that have reached the end of their availability period into 'Expired' status.
/// Satisfies Scenario 3: Availability Period Ends.
/// </summary>
public class DonationExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DonationExpiryBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public DonationExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DonationExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DonationExpiryBackgroundService started. Periodic check every {Interval}s.", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var donationService = scope.ServiceProvider.GetRequiredService<IDonationService>();
                
                var expiredCount = await donationService.ProcessExpiredDonationsAsync();
                if (expiredCount > 0)
                {
                    _logger.LogInformation("DonationExpiryBackgroundService automatically transitioned {Count} expired donation(s) to 'Expired' status.", expiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired donations in background service.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("DonationExpiryBackgroundService stopped.");
    }
}
