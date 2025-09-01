namespace Gchain.Interfaces;

/// <summary>
/// Interface for background services that perform cleanup and maintenance tasks
/// </summary>
public interface IBackgroundService
{
    /// <summary>
    /// Starts the background service
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background service
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the service name
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Gets whether the service is running
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Interface for cleanup services
/// </summary>
public interface ICleanupService : IBackgroundService
{
    /// <summary>
    /// Performs cleanup operations
    /// </summary>
    Task<int> PerformCleanupAsync();

    /// <summary>
    /// Gets the cleanup interval
    /// </summary>
    TimeSpan CleanupInterval { get; }

    /// <summary>
    /// Gets the last cleanup time
    /// </summary>
    DateTime? LastCleanupTime { get; }
}
