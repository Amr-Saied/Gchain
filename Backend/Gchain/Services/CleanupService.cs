using Gchain.Data;
using Gchain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Services;

/// <summary>
/// Background service for performing cleanup operations
/// </summary>
public class CleanupService : ICleanupService
{
    private readonly ApplicationDbContext _context;
    private readonly ITurnTimerService _turnTimerService;
    private readonly IGameStateCacheService _gameStateCacheService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CleanupService> _logger;
    private readonly Timer? _timer;
    private volatile bool _isRunning = false;
    private DateTime? _lastCleanupTime;

    public string ServiceName => "CleanupService";
    public bool IsRunning => _isRunning;
    public TimeSpan CleanupInterval => TimeSpan.FromMinutes(5); // Run every 5 minutes
    public DateTime? LastCleanupTime => _lastCleanupTime;

    public CleanupService(
        ApplicationDbContext context,
        ITurnTimerService turnTimerService,
        IGameStateCacheService gameStateCacheService,
        INotificationService notificationService,
        ILogger<CleanupService> logger
    )
    {
        _context = context;
        _turnTimerService = turnTimerService;
        _gameStateCacheService = gameStateCacheService;
        _notificationService = notificationService;
        _logger = logger;

        // Create timer that runs every 5 minutes
        _timer = new Timer(PerformCleanupCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("CleanupService is already running");
            return Task.CompletedTask;
        }

        _isRunning = true;
        _logger.LogInformation("Starting CleanupService");

        // Start the timer
        _timer?.Change(TimeSpan.Zero, CleanupInterval);

        // Perform initial cleanup in background
        _ = Task.Run(async () => await PerformCleanupAsync(), cancellationToken);

        _logger.LogInformation("CleanupService started successfully");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("CleanupService is not running");
            return Task.CompletedTask;
        }

        _isRunning = false;
        _logger.LogInformation("Stopping CleanupService");

        // Stop the timer
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("CleanupService stopped successfully");
        return Task.CompletedTask;
    }

    private async void PerformCleanupCallback(object? state)
    {
        if (!_isRunning)
            return;

        try
        {
            await PerformCleanupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during cleanup callback");
        }
    }

    public async Task<int> PerformCleanupAsync()
    {
        var totalCleaned = 0;

        try
        {
            _logger.LogDebug("Starting cleanup operations");

            // 1. Clean up expired turn timers
            var expiredTimers = await _turnTimerService.CleanupExpiredTimersAsync();
            totalCleaned += expiredTimers;

            // 2. Clean up expired user sessions
            var expiredSessions = await CleanupExpiredUserSessionsAsync();
            totalCleaned += expiredSessions;

            // 3. Clean up old notifications
            var oldNotifications = await _notificationService.DeleteOldNotificationsAsync(30);
            totalCleaned += oldNotifications;

            // 4. Clean up abandoned game sessions
            var abandonedGames = await CleanupAbandonedGameSessionsAsync();
            totalCleaned += abandonedGames;

            // 5. Clean up orphaned game state cache
            var orphanedCache = await CleanupOrphanedGameStateCacheAsync();
            totalCleaned += orphanedCache;

            _lastCleanupTime = DateTime.UtcNow;

            if (totalCleaned > 0)
            {
                _logger.LogInformation(
                    "Cleanup completed. Total items cleaned: {TotalCleaned}",
                    totalCleaned
                );
            }
            else
            {
                _logger.LogDebug("Cleanup completed. No items needed cleaning");
            }

            return totalCleaned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during cleanup operations");
            return totalCleaned;
        }
    }

    private async Task<int> CleanupExpiredUserSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context
                .UserSessions.Where(us =>
                    us.ExpiresAt.HasValue && us.ExpiresAt.Value < DateTime.UtcNow
                )
                .ToListAsync();

            if (expiredSessions.Any())
            {
                _context.UserSessions.RemoveRange(expiredSessions);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleaned up {Count} expired user sessions",
                    expiredSessions.Count
                );
                return expiredSessions.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired user sessions");
            return 0;
        }
    }

    private async Task<int> CleanupAbandonedGameSessionsAsync()
    {
        try
        {
            // Find game sessions that have been inactive for more than 2 hours
            var cutoffTime = DateTime.UtcNow.AddHours(-2);

            var abandonedGames = await _context
                .GameSessions.Where(gs => gs.IsActive && gs.CreatedAt < cutoffTime)
                .ToListAsync();

            var cleanedCount = 0;

            foreach (var game in abandonedGames)
            {
                try
                {
                    // Mark game as abandoned
                    game.IsActive = false;
                    // Note: GameSession doesn't have UpdatedAt or EndedAt properties

                    // Clean up game state cache
                    await _gameStateCacheService.DeleteGameStateAsync(game.Id);

                    // End any active turn timers
                    await _turnTimerService.EndTurnAsync(game.Id);

                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup abandoned game {GameId}", game.Id);
                }
            }

            if (cleanedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} abandoned game sessions", cleanedCount);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup abandoned game sessions");
            return 0;
        }
    }

    private async Task<int> CleanupOrphanedGameStateCacheAsync()
    {
        try
        {
            // Get all active game sessions from database
            var activeGameIds = await _context
                .GameSessions.Where(gs => gs.IsActive)
                .Select(gs => gs.Id)
                .ToListAsync();

            // Get all cached game states
            var cachedGameIds = await _gameStateCacheService.GetActiveGameSessionsAsync();

            // Find orphaned cache entries (cached but not in database)
            var orphanedIds = cachedGameIds.Except(activeGameIds).ToList();

            var cleanedCount = 0;

            foreach (var gameId in orphanedIds)
            {
                try
                {
                    await _gameStateCacheService.DeleteGameStateAsync(gameId);
                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to cleanup orphaned game state cache for game {GameId}",
                        gameId
                    );
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} orphaned game state cache entries",
                    cleanedCount
                );
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup orphaned game state cache");
            return 0;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
