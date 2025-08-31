using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for managing turn timers using Redis
/// </summary>
public class TurnTimerService : ITurnTimerService
{
    private readonly IRedisService _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<TurnTimerService> _logger;

    public TurnTimerService(
        IRedisService redis,
        IOptions<RedisSettings> settings,
        ILogger<TurnTimerService> logger
    )
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetTurnDeadlineKey(int gameSessionId) => $"game:{gameSessionId}:turn:deadline";

    private string GetCurrentPlayerKey(int gameSessionId) => $"game:{gameSessionId}:turn:player";

    private string GetActiveTurnTimersKey() => "turns:active";

    public async Task<DateTime?> StartTurnAsync(
        int gameSessionId,
        string userId,
        int turnDurationSeconds
    )
    {
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(turnDurationSeconds);
            var deadlineKey = GetTurnDeadlineKey(gameSessionId);
            var playerKey = GetCurrentPlayerKey(gameSessionId);
            var activeTimersKey = GetActiveTurnTimersKey();

            // Set turn deadline with TTL slightly longer than turn duration for cleanup
            var ttl = TimeSpan.FromSeconds(turnDurationSeconds + 30);

            await _redis.SetAsync(deadlineKey, deadline.ToString("O"), ttl);
            await _redis.SetAsync(playerKey, userId, ttl);

            // Add to active timers set with score as deadline timestamp
            var score = ((DateTimeOffset)deadline).ToUnixTimeSeconds();
            await _redis.SortedSetAddAsync(activeTimersKey, gameSessionId.ToString(), score);

            _logger.LogInformation(
                "Started turn timer for game {GameSessionId}, player {UserId}, duration {Duration}s",
                gameSessionId,
                userId,
                turnDurationSeconds
            );

            return deadline;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start turn timer for game {GameSessionId}, player {UserId}",
                gameSessionId,
                userId
            );
            return null;
        }
    }

    public async Task<TimeSpan?> GetRemainingTimeAsync(int gameSessionId)
    {
        try
        {
            var deadline = await GetTurnDeadlineAsync(gameSessionId);
            if (deadline == null)
                return null;

            var remaining = deadline.Value - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get remaining time for game {GameSessionId}",
                gameSessionId
            );
            return null;
        }
    }

    public async Task<DateTime?> GetTurnDeadlineAsync(int gameSessionId)
    {
        try
        {
            var key = GetTurnDeadlineKey(gameSessionId);
            var deadlineStr = await _redis.GetAsync(key);

            if (string.IsNullOrEmpty(deadlineStr))
                return null;

            if (DateTime.TryParse(deadlineStr, out var deadline))
                return deadline;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get turn deadline for game {GameSessionId}",
                gameSessionId
            );
            return null;
        }
    }

    public async Task<bool> IsTurnExpiredAsync(int gameSessionId)
    {
        try
        {
            var deadline = await GetTurnDeadlineAsync(gameSessionId);
            if (deadline == null)
                return false;

            return DateTime.UtcNow > deadline.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check if turn expired for game {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<bool> EndTurnAsync(int gameSessionId)
    {
        try
        {
            var deadlineKey = GetTurnDeadlineKey(gameSessionId);
            var playerKey = GetCurrentPlayerKey(gameSessionId);
            var activeTimersKey = GetActiveTurnTimersKey();

            // Delete turn timer data
            var deadlineDeleted = await _redis.DeleteAsync(deadlineKey);
            var playerDeleted = await _redis.DeleteAsync(playerKey);

            // Remove from active timers
            await _redis.SortedSetRemoveAsync(activeTimersKey, gameSessionId.ToString());

            var success = deadlineDeleted || playerDeleted;

            if (success)
            {
                _logger.LogInformation("Ended turn timer for game {GameSessionId}", gameSessionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to end turn timer for game {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<string?> GetCurrentPlayerAsync(int gameSessionId)
    {
        try
        {
            var key = GetCurrentPlayerKey(gameSessionId);
            return await _redis.GetAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get current player for game {GameSessionId}",
                gameSessionId
            );
            return null;
        }
    }

    public async Task<DateTime?> ExtendTurnAsync(int gameSessionId, int additionalSeconds)
    {
        try
        {
            var currentDeadline = await GetTurnDeadlineAsync(gameSessionId);
            if (currentDeadline == null)
                return null;

            var newDeadline = currentDeadline.Value.AddSeconds(additionalSeconds);
            var deadlineKey = GetTurnDeadlineKey(gameSessionId);
            var activeTimersKey = GetActiveTurnTimersKey();

            // Update deadline
            var success = await _redis.SetAsync(deadlineKey, newDeadline.ToString("O"));

            if (success)
            {
                // Update score in active timers sorted set
                var score = ((DateTimeOffset)newDeadline).ToUnixTimeSeconds();
                await _redis.SortedSetAddAsync(activeTimersKey, gameSessionId.ToString(), score);

                _logger.LogInformation(
                    "Extended turn timer for game {GameSessionId} by {AdditionalSeconds}s",
                    gameSessionId,
                    additionalSeconds
                );

                return newDeadline;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to extend turn timer for game {GameSessionId}",
                gameSessionId
            );
            return null;
        }
    }

    public async Task<Dictionary<int, DateTime>> GetActiveTimersAsync()
    {
        try
        {
            var activeTimersKey = GetActiveTurnTimersKey();
            var timers = await _redis.SortedSetGetTopAsync(activeTimersKey, 1000); // Get up to 1000 active timers

            var result = new Dictionary<int, DateTime>();

            foreach (var (value, score) in timers)
            {
                if (int.TryParse(value, out var gameSessionId))
                {
                    var deadline = DateTimeOffset.FromUnixTimeSeconds((long)score).DateTime;
                    result[gameSessionId] = deadline;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active timers");
            return new Dictionary<int, DateTime>();
        }
    }

    public async Task<int> CleanupExpiredTimersAsync()
    {
        try
        {
            var activeTimersKey = GetActiveTurnTimersKey();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Get all timers that have expired (score < current timestamp)
            var expiredTimers = await _redis.SortedSetGetTopAsync(activeTimersKey, 1000);

            var cleanedCount = 0;

            foreach (var (value, score) in expiredTimers)
            {
                if (score < now) // Timer has expired
                {
                    if (int.TryParse(value, out var gameSessionId))
                    {
                        await EndTurnAsync(gameSessionId);
                        cleanedCount++;
                    }
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired turn timers", cleanedCount);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired timers");
            return 0;
        }
    }
}
