using Gchain.Interfaces;

namespace Gchain.Services;

/// <summary>
/// Hosted service that manages background services
/// </summary>
public class BackgroundServiceHost : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundServiceHost> _logger;
    private readonly List<IBackgroundService> _backgroundServices = new();

    public BackgroundServiceHost(
        IServiceProvider serviceProvider,
        ILogger<BackgroundServiceHost> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundServiceHost starting...");

        try
        {
            // Initialize background services
            await InitializeBackgroundServicesAsync(stoppingToken);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                // Check service health periodically
                await CheckServiceHealthAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BackgroundServiceHost is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackgroundServiceHost encountered an error");
        }
        finally
        {
            // Stop all background services
            await StopAllBackgroundServicesAsync();
        }
    }

    private async Task InitializeBackgroundServicesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            // Initialize cleanup service
            var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();
            await cleanupService.StartAsync(cancellationToken);
            _backgroundServices.Add(cleanupService);

            _logger.LogInformation("Background services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize background services");
        }
    }

    private Task CheckServiceHealthAsync()
    {
        foreach (var service in _backgroundServices)
        {
            try
            {
                if (!service.IsRunning)
                {
                    _logger.LogWarning(
                        "Background service {ServiceName} is not running",
                        service.ServiceName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking health of background service {ServiceName}",
                    service.ServiceName
                );
            }
        }

        return Task.CompletedTask;
    }

    private async Task StopAllBackgroundServicesAsync()
    {
        _logger.LogInformation("Stopping all background services...");

        foreach (var service in _backgroundServices)
        {
            try
            {
                await service.StopAsync();
                _logger.LogInformation(
                    "Stopped background service: {ServiceName}",
                    service.ServiceName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error stopping background service {ServiceName}",
                    service.ServiceName
                );
            }
        }

        _backgroundServices.Clear();
        _logger.LogInformation("All background services stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundServiceHost stop requested");
        await base.StopAsync(cancellationToken);
    }
}
