using System.Text.Json;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Gchain.Services;

/// <summary>
/// Redis service implementation using StackExchange.Redis
/// </summary>
public class RedisService : IRedisService, IDisposable
{
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisService> _logger;
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisService(IOptions<RedisSettings> settings, ILogger<RedisService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<ConnectionMultiplexer>(CreateConnection);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private ConnectionMultiplexer CreateConnection()
    {
        try
        {
            var configurationOptions = ConfigurationOptions.Parse(_settings.ConnectionString);
            configurationOptions.ConnectTimeout = _settings.ConnectTimeout;
            configurationOptions.SyncTimeout = _settings.SyncTimeout;
            configurationOptions.AbortOnConnectFail = false;

            var connection = ConnectionMultiplexer.Connect(configurationOptions);
            _logger.LogInformation("Redis connection established successfully");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Redis");
            throw;
        }
    }

    private IDatabase Database => _connection.Value.GetDatabase(_settings.Database);

    private string GetKey(string key) => $"{_settings.KeyPrefix}{key}";

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await Database.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return false;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var value = await Database.StringGetAsync(GetKey(key));
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value for key {Key}", key);
            return null;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
        where T : class
    {
        try
        {
            var value = await GetAsync(key);
            if (string.IsNullOrEmpty(value))
                return null;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize value for key {Key} to type {Type}",
                key,
                typeof(T).Name
            );
            return null;
        }
    }

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            expiry ??= TimeSpan.FromSeconds(_settings.DefaultTtlSeconds);
            return await Database.StringSetAsync(GetKey(key), value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value for key {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            return await SetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize and set value for key {Key} of type {Type}",
                key,
                typeof(T).Name
            );
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            return await Database.KeyDeleteAsync(GetKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await Database.KeyExistsAsync(GetKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of key {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
    {
        try
        {
            return await Database.KeyExpireAsync(GetKey(key), expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set expiration for key {Key}", key);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string key)
    {
        try
        {
            return await Database.KeyTimeToLiveAsync(GetKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get TTL for key {Key}", key);
            return null;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null)
    {
        try
        {
            var redisKey = GetKey(key);
            var result = await Database.StringIncrementAsync(redisKey, value);

            if (expiry.HasValue)
            {
                await Database.KeyExpireAsync(redisKey, expiry);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment key {Key}", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        try
        {
            return await Database.StringDecrementAsync(GetKey(key), value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrement key {Key}", key);
            return 0;
        }
    }

    public async Task<bool> SetAddAsync(string key, string value)
    {
        try
        {
            return await Database.SetAddAsync(GetKey(key), value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add value to set {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetRemoveAsync(string key, string value)
    {
        try
        {
            return await Database.SetRemoveAsync(GetKey(key), value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove value from set {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetContainsAsync(string key, string value)
    {
        try
        {
            return await Database.SetContainsAsync(GetKey(key), value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if set {Key} contains value", key);
            return false;
        }
    }

    public async Task<string[]> SetGetAllAsync(string key)
    {
        try
        {
            var values = await Database.SetMembersAsync(GetKey(key));
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all values from set {Key}", key);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> SortedSetAddAsync(string key, string value, double score)
    {
        try
        {
            return await Database.SortedSetAddAsync(GetKey(key), value, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add value to sorted set {Key}", key);
            return false;
        }
    }

    public async Task<(string Value, double Score)[]> SortedSetGetTopAsync(string key, int count)
    {
        try
        {
            var values = await Database.SortedSetRangeByRankWithScoresAsync(
                GetKey(key),
                0,
                count - 1,
                Order.Descending
            );

            return values.Select(v => (v.Element.ToString(), v.Score)).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top values from sorted set {Key}", key);
            return Array.Empty<(string, double)>();
        }
    }

    public async Task<long?> SortedSetGetRankAsync(string key, string value)
    {
        try
        {
            return await Database.SortedSetRankAsync(GetKey(key), value, Order.Descending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rank for value in sorted set {Key}", key);
            return null;
        }
    }

    public async Task<bool> SortedSetRemoveAsync(string key, string value)
    {
        try
        {
            return await Database.SortedSetRemoveAsync(GetKey(key), value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove value from sorted set {Key}", key);
            return false;
        }
    }

    public async Task<bool> HashSetAsync(string key, string field, string value)
    {
        try
        {
            return await Database.HashSetAsync(GetKey(key), field, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set hash field {Field} for key {Key}", field, key);
            return false;
        }
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        try
        {
            var value = await Database.HashGetAsync(GetKey(key), field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hash field {Field} for key {Key}", field, key);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        try
        {
            var fields = await Database.HashGetAllAsync(GetKey(key));
            return fields.ToDictionary(
                field => field.Name.ToString(),
                field => field.Value.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all hash fields for key {Key}", key);
            return new Dictionary<string, string>();
        }
    }

    public async Task HashSetMultipleAsync(string key, Dictionary<string, string> fields)
    {
        try
        {
            var hashFields = fields.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
            await Database.HashSetAsync(GetKey(key), hashFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set multiple hash fields for key {Key}", key);
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field)
    {
        try
        {
            return await Database.HashDeleteAsync(GetKey(key), field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete hash field {Field} for key {Key}", field, key);
            return false;
        }
    }

    public Task<string[]> ScanKeysAsync(string pattern, int count = 1000)
    {
        try
        {
            var keys = new List<string>();
            var database = Database;
            var server = _connection.Value.GetServer(_connection.Value.GetEndPoints()[0]);

            var redisKeys = server.Keys(database.Database, GetKey(pattern), count);

            foreach (var key in redisKeys)
            {
                keys.Add(key.ToString().Substring(_settings.KeyPrefix.Length));
            }

            return Task.FromResult(keys.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan keys with pattern {Pattern}", pattern);
            return Task.FromResult(Array.Empty<string>());
        }
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value?.Dispose();
        }
    }
}
