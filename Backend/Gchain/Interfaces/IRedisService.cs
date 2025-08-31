namespace Gchain.Interfaces;

/// <summary>
/// Interface for Redis operations
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Check if Redis connection is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Get a string value from Redis
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Get a typed value from Redis (JSON deserialized)
    /// </summary>
    Task<T?> GetAsync<T>(string key)
        where T : class;

    /// <summary>
    /// Set a string value in Redis with optional TTL
    /// </summary>
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// Set a typed value in Redis (JSON serialized) with optional TTL
    /// </summary>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        where T : class;

    /// <summary>
    /// Delete a key from Redis
    /// </summary>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Check if a key exists in Redis
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Set expiration for a key
    /// </summary>
    Task<bool> ExpireAsync(string key, TimeSpan expiry);

    /// <summary>
    /// Get time to live for a key
    /// </summary>
    Task<TimeSpan?> GetTtlAsync(string key);

    /// <summary>
    /// Increment a numeric value (for counters, rate limiting)
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null);

    /// <summary>
    /// Decrement a numeric value
    /// </summary>
    Task<long> DecrementAsync(string key, long value = 1);

    /// <summary>
    /// Add value to a set
    /// </summary>
    Task<bool> SetAddAsync(string key, string value);

    /// <summary>
    /// Remove value from a set
    /// </summary>
    Task<bool> SetRemoveAsync(string key, string value);

    /// <summary>
    /// Check if value exists in a set
    /// </summary>
    Task<bool> SetContainsAsync(string key, string value);

    /// <summary>
    /// Get all values from a set
    /// </summary>
    Task<string[]> SetGetAllAsync(string key);

    /// <summary>
    /// Add value to a sorted set with score
    /// </summary>
    Task<bool> SortedSetAddAsync(string key, string value, double score);

    /// <summary>
    /// Get top N values from sorted set (highest scores)
    /// </summary>
    Task<(string Value, double Score)[]> SortedSetGetTopAsync(string key, int count);

    /// <summary>
    /// Get rank of a value in sorted set
    /// </summary>
    Task<long?> SortedSetGetRankAsync(string key, string value);

    /// <summary>
    /// Remove value from sorted set
    /// </summary>
    Task<bool> SortedSetRemoveAsync(string key, string value);

    /// <summary>
    /// Set hash field value
    /// </summary>
    Task<bool> HashSetAsync(string key, string field, string value);

    /// <summary>
    /// Get hash field value
    /// </summary>
    Task<string?> HashGetAsync(string key, string field);

    /// <summary>
    /// Get all hash fields and values
    /// </summary>
    Task<Dictionary<string, string>> HashGetAllAsync(string key);

    /// <summary>
    /// Set multiple hash fields
    /// </summary>
    Task HashSetMultipleAsync(string key, Dictionary<string, string> fields);

    /// <summary>
    /// Delete hash field
    /// </summary>
    Task<bool> HashDeleteAsync(string key, string field);

    /// <summary>
    /// Scan for keys matching a pattern
    /// </summary>
    Task<string[]> ScanKeysAsync(string pattern, int count = 1000);
}
