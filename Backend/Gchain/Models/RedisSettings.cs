namespace Gchain.Models;

/// <summary>
/// Configuration settings for Redis
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Default database index to use
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// Key prefix for all Redis keys
    /// </summary>
    public string KeyPrefix { get; set; } = "gchain:";

    /// <summary>
    /// Default TTL for cached data in seconds
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Game state cache TTL in seconds
    /// </summary>
    public int GameStateTtlSeconds { get; set; } = 7200; // 2 hours

    /// <summary>
    /// Rate limiting window in seconds
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Turn timer TTL in seconds
    /// </summary>
    public int TurnTimerTtlSeconds { get; set; } = 120; // 2 minutes max turn time

    /// <summary>
    /// Word similarity cache TTL in seconds
    /// </summary>
    public int SimilarityCacheTtlSeconds { get; set; } = 86400; // 24 hours
}
