namespace Gchain.Interfaces;

/// <summary>
/// Interface for rate limiting operations
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Check if an action is rate limited for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action being performed</param>
    /// <param name="maxAttempts">Maximum attempts allowed</param>
    /// <param name="windowSeconds">Time window in seconds</param>
    /// <returns>True if action is allowed, false if rate limited</returns>
    Task<bool> IsActionAllowedAsync(
        string userId,
        string action,
        int maxAttempts,
        int windowSeconds
    );

    /// <summary>
    /// Check rate limit and increment counter if allowed
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action being performed</param>
    /// <param name="maxAttempts">Maximum attempts allowed</param>
    /// <param name="windowSeconds">Time window in seconds</param>
    /// <returns>RateLimitResult with details</returns>
    Task<RateLimitResult> CheckAndIncrementAsync(
        string userId,
        string action,
        int maxAttempts,
        int windowSeconds
    );

    /// <summary>
    /// Get current rate limit status for a user action
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action being checked</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitStatus> GetStatusAsync(string userId, string action);

    /// <summary>
    /// Reset rate limit for a user action
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action to reset</param>
    /// <returns>True if reset successful</returns>
    Task<bool> ResetAsync(string userId, string action);

    /// <summary>
    /// Check if word submission is allowed (specific rate limit)
    /// </summary>
    Task<RateLimitResult> CheckWordSubmissionAsync(string userId);

    /// <summary>
    /// Check if chat message is allowed (specific rate limit)
    /// </summary>
    Task<RateLimitResult> CheckChatMessageAsync(string userId);

    /// <summary>
    /// Check if game creation is allowed (specific rate limit)
    /// </summary>
    Task<RateLimitResult> CheckGameCreationAsync(string userId);
}

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int CurrentCount { get; set; }
    public int MaxAttempts { get; set; }
    public TimeSpan TimeUntilReset { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ReasonMessage { get; set; }
}

/// <summary>
/// Current status of rate limiting for an action
/// </summary>
public class RateLimitStatus
{
    public int CurrentCount { get; set; }
    public int MaxAttempts { get; set; }
    public TimeSpan TimeUntilReset { get; set; }
    public bool IsLimited { get; set; }
    public string Action { get; set; } = string.Empty;
}
