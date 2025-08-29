using Gchain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gchain.Services;

/// <summary>
/// Health check for Redis connectivity
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisService _redisService;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IRedisService redisService, ILogger<RedisHealthCheck> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var isHealthy = await _redisService.IsHealthyAsync();

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Redis is responding");
            }
            else
            {
                return HealthCheckResult.Unhealthy("Redis is not responding");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis health check failed", ex);
        }
    }
}
