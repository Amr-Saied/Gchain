using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for implementing rate limiting using Redis
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IRedisService _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RateLimitService> _logger;

    // Rate limit configurations
    private const int WordSubmissionMaxAttempts = 1; // 1 attempt per turn
    private const int ChatMessageMaxAttempts = 5; // 5 messages per minute
    private const int GameCreationMaxAttempts = 3; // 3 games per hour

    public RateLimitService(
        IRedisService redis,
        IOptions<RedisSettings> settings,
        ILogger<RateLimitService> logger
    )
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetRateLimitKey(string userId, string action) => $"rl:{userId}:{action}";

    public async Task<bool> IsActionAllowedAsync(
        string userId,
        string action,
        int maxAttempts,
        int windowSeconds
    )
    {
        try
        {
            var key = GetRateLimitKey(userId, action);
            var currentCount = await _redis.GetAsync(key);

            if (string.IsNullOrEmpty(currentCount))
                return true;

            if (int.TryParse(currentCount, out var count))
            {
                return count < maxAttempts;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check rate limit for user {UserId}, action {Action}",
                userId,
                action
            );
            // Fail open - allow action if we can't check rate limit
            return true;
        }
    }

    public async Task<RateLimitResult> CheckAndIncrementAsync(
        string userId,
        string action,
        int maxAttempts,
        int windowSeconds
    )
    {
        try
        {
            var key = GetRateLimitKey(userId, action);
            var window = TimeSpan.FromSeconds(windowSeconds);

            // Increment counter and get current value
            var currentCount = await _redis.IncrementAsync(key, 1, window);

            // Get remaining TTL
            var ttl = await _redis.GetTtlAsync(key) ?? window;

            var isAllowed = currentCount <= maxAttempts;

            var result = new RateLimitResult
            {
                IsAllowed = isAllowed,
                CurrentCount = (int)currentCount,
                MaxAttempts = maxAttempts,
                TimeUntilReset = ttl,
                Action = action,
                ReasonMessage = isAllowed
                    ? null
                    : $"Rate limit exceeded. Max {maxAttempts} {action} per {windowSeconds} seconds."
            };

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for user {UserId}, action {Action}. Count: {Count}/{Max}",
                    userId,
                    action,
                    currentCount,
                    maxAttempts
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check and increment rate limit for user {UserId}, action {Action}",
                userId,
                action
            );

            // Fail open - allow action if we can't check rate limit
            return new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 0,
                MaxAttempts = maxAttempts,
                TimeUntilReset = TimeSpan.Zero,
                Action = action,
                ReasonMessage = null
            };
        }
    }

    public async Task<RateLimitStatus> GetStatusAsync(string userId, string action)
    {
        try
        {
            var key = GetRateLimitKey(userId, action);
            var currentCountStr = await _redis.GetAsync(key);
            var ttl = await _redis.GetTtlAsync(key) ?? TimeSpan.Zero;

            var currentCount = 0;
            if (
                !string.IsNullOrEmpty(currentCountStr)
                && int.TryParse(currentCountStr, out var count)
            )
            {
                currentCount = count;
            }

            // Default max attempts based on action type
            var maxAttempts = action.ToLowerInvariant() switch
            {
                "word_submission" => WordSubmissionMaxAttempts,
                "chat_message" => ChatMessageMaxAttempts,
                "game_creation" => GameCreationMaxAttempts,
                _ => 10 // Default limit
            };

            return new RateLimitStatus
            {
                CurrentCount = currentCount,
                MaxAttempts = maxAttempts,
                TimeUntilReset = ttl,
                IsLimited = currentCount >= maxAttempts,
                Action = action
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get rate limit status for user {UserId}, action {Action}",
                userId,
                action
            );

            return new RateLimitStatus
            {
                CurrentCount = 0,
                MaxAttempts = 10,
                TimeUntilReset = TimeSpan.Zero,
                IsLimited = false,
                Action = action
            };
        }
    }

    public async Task<bool> ResetAsync(string userId, string action)
    {
        try
        {
            var key = GetRateLimitKey(userId, action);
            var deleted = await _redis.DeleteAsync(key);

            if (deleted)
            {
                _logger.LogInformation(
                    "Reset rate limit for user {UserId}, action {Action}",
                    userId,
                    action
                );
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to reset rate limit for user {UserId}, action {Action}",
                userId,
                action
            );
            return false;
        }
    }

    public async Task<RateLimitResult> CheckWordSubmissionAsync(string userId)
    {
        // Word submissions are limited per turn, not per time window
        // This is a special case where we check if user has already submitted for current turn
        return await CheckAndIncrementAsync(
            userId,
            "word_submission",
            WordSubmissionMaxAttempts,
            60
        );
    }

    public async Task<RateLimitResult> CheckChatMessageAsync(string userId)
    {
        return await CheckAndIncrementAsync(userId, "chat_message", ChatMessageMaxAttempts, 60);
    }

    public async Task<RateLimitResult> CheckGameCreationAsync(string userId)
    {
        return await CheckAndIncrementAsync(userId, "game_creation", GameCreationMaxAttempts, 3600);
    }
}
