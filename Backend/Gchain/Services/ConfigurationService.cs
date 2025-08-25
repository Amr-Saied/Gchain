using Gchain.Interfaces;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfigurationService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return "http://localhost:5024";
        }

        var scheme = request.Scheme;
        var host = request.Host.Value;
        return $"{scheme}://{host}";
    }

    public string GetDatabaseConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public string GetJwtSecretKey()
    {
        return _configuration["JwtSettings:SecretKey"] ?? string.Empty;
    }

    public string GetJwtIssuer()
    {
        return _configuration["JwtSettings:Issuer"] ?? "Gchain";
    }

    public string GetJwtAudience()
    {
        return _configuration["JwtSettings:Audience"] ?? "GchainUsers";
    }

    public int GetJwtAccessTokenExpirationMinutes()
    {
        return int.TryParse(
            _configuration["JwtSettings:AccessTokenExpirationMinutes"],
            out var minutes
        )
            ? minutes
            : 15;
    }

    public int GetJwtRefreshTokenExpirationDays()
    {
        return int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days)
            ? days
            : 7;
    }
}
