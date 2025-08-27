using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.AspNetCore.Http;

namespace Gchain.Services;

/// <summary>
/// Service for handling guest authentication
/// </summary>
public class GuestAuthService : IGuestAuthService
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<GuestAuthService> _logger;

    public GuestAuthService(
        IUserService userService,
        IJwtService jwtService,
        ILogger<GuestAuthService> logger
    )
    {
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new guest user and returns authentication response
    /// </summary>
    public async Task<GuestAuthResponse> CreateGuestAsync(HttpContext httpContext)
    {
        try
        {
            // Generate consistent browser identifier from HTTP context
            var browserId = GenerateBrowserId(httpContext);

            // Check if browser already has an active guest session within 1 hour
            var existingSession = await _userService.CheckForExistingGuestSessionAsync(browserId);

            if (existingSession != null)
            {
                _logger.LogInformation(
                    "Found existing guest session within 1 hour for browser {BrowserId}, refreshing session for user {Username}",
                    browserId,
                    existingSession.User.UserName
                );

                // Refresh existing session
                var refreshedSession = await _userService.RefreshExistingGuestSessionAsync(
                    existingSession
                );

                // Generate new JWT tokens for existing user
                var existingAccessToken = _jwtService.GenerateAccessToken(existingSession.User);
                var existingRefreshToken = refreshedSession.RefreshToken;

                // Get user profile data
                var existingUserProfile = await _userService.GetUserProfileAsync(
                    existingSession.User.Id
                );

                // Create guest user info for existing user
                var existingGuestUserInfo = new GuestUserInfo
                {
                    Id = existingSession.User.Id,
                    Email = string.Empty,
                    Name = existingSession.User.UserName ?? string.Empty,
                    FirstName = "Guest",
                    LastName = string.Empty,
                    Picture = string.Empty,
                    EmailVerified = false,
                    Locale = null
                };

                return new GuestAuthResponse
                {
                    AccessToken = existingAccessToken,
                    RefreshToken = existingRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    User = existingGuestUserInfo,
                    IsNewUser = false, // Existing user within 1 hour
                    DatabaseUserId = existingSession.User.Id,
                    UserSession = new UserSessionInfo
                    {
                        Id = refreshedSession.Id.ToString(),
                        RefreshToken = refreshedSession.RefreshToken,
                        ExpiresAt = refreshedSession.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
                        CreatedAt = refreshedSession.CreatedAt
                    },
                    UserProfile = existingUserProfile
                };
            }

            // No active session within 1 hour - create new guest user
            _logger.LogInformation(
                "No active guest session within 1 hour for browser {BrowserId}, creating new guest user",
                browserId
            );

            // Create new guest user (either no session exists or session is older than 1 hour)
            var username = await GenerateUniqueGuestUsernameAsync();
            var (user, success) = await _userService.CreateGuestUserAsync(username, null);

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Failed to create guest user with username: {username}"
                );
            }

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Create user session with browser ID
            var userSession = await _userService.CreateOrUpdateUserSessionAsync(
                user.Id,
                refreshToken
            );

            // Update session with browser ID
            userSession.DeviceInfo = browserId;
            await _userService.UpdateUserSessionAsync(userSession);

            // Get user profile data
            var userProfile = await _userService.GetUserProfileAsync(user.Id);

            // Create guest user info
            var guestUserInfo = new GuestUserInfo
            {
                Id = user.Id,
                Email = string.Empty,
                Name = username,
                FirstName = "Guest",
                LastName = string.Empty,
                Picture = string.Empty,
                EmailVerified = false,
                Locale = null
            };

            _logger.LogInformation(
                "Created new guest user {Username} for browser {BrowserId} (no active session within 1 hour). Database ID: {UserId}",
                username,
                browserId,
                user.Id
            );

            return new GuestAuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = guestUserInfo,
                IsNewUser = true,
                DatabaseUserId = user.Id,
                UserSession = new UserSessionInfo
                {
                    Id = userSession.Id.ToString(),
                    RefreshToken = userSession.RefreshToken,
                    ExpiresAt = userSession.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
                    CreatedAt = userSession.CreatedAt
                },
                UserProfile = userProfile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Guest authentication failed");
            throw;
        }
    }

    /// <summary>
    /// Generates a unique guest username
    /// </summary>
    public async Task<string> GenerateUniqueGuestUsernameAsync()
    {
        const int maxRetries = 10;

        for (int i = 0; i < maxRetries; i++)
        {
            // timestamp (last 8 digits) + random 3 digits
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var timestampSuffix =
                timestamp.Length >= 8 ? timestamp.Substring(timestamp.Length - 8) : timestamp;
            var random = Random.Shared.Next(100, 1000);
            var username = $"Guest_{timestampSuffix}{random}";

            // Check uniqueness in database
            var existingUser = await _userService.FindUserByUsernameAsync(username);
            if (existingUser == null)
            {
                _logger.LogDebug(
                    "Generated unique guest username: {Username} on attempt {Attempt}",
                    username,
                    i + 1
                );
                return username;
            }

            _logger.LogDebug(
                "Username collision for {Username} on attempt {Attempt}, retrying...",
                username,
                i + 1
            );
        }

        throw new InvalidOperationException(
            "Failed to generate unique guest username after multiple attempts"
        );
    }

    /// <summary>
    /// Generates a consistent browser identifier based on IP and User-Agent
    /// </summary>
    private string GenerateBrowserId(HttpContext httpContext)
    {
        try
        {
            // Get IP address
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Get User-Agent
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "unknown";

            // Create a hash of IP + User-Agent for consistent identification
            var combined = $"{ipAddress}|{userAgent}";
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(combined)
            );
            var browserId = Convert.ToHexString(hash).Substring(0, 16).ToLower();

            _logger.LogDebug(
                "Generated consistent browser ID: {BrowserId} from IP: {IP}, User-Agent: {UserAgent}",
                browserId,
                ipAddress,
                userAgent
            );
            return browserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate browser ID from HTTP context, using fallback");
            return $"browser_fallback_{Guid.NewGuid():N}";
        }
    }
}
