using System.Text.Json;
using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for handling Google OAuth authentication
/// </summary>
public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly GoogleOAuthSettings _googleSettings;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly HttpClient _httpClient;

    public GoogleOAuthService(
        IOptions<GoogleOAuthSettings> googleSettings,
        IUserService userService,
        IJwtService jwtService,
        ILogger<GoogleOAuthService> logger,
        HttpClient httpClient
    )
    {
        _googleSettings = googleSettings.Value;
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Authenticates a user using Google OAuth token
    /// </summary>
    public async Task<GoogleOAuthResponse> AuthenticateAsync(GoogleOAuthRequest request)
    {
        try
        {
            GoogleUserInfo? googleUser = null;

            // Validate token based on type
            if (request.TokenType.ToLower() == "id_token")
            {
                googleUser = await ValidateIdTokenAsync(request.Token);
            }
            else if (request.TokenType.ToLower() == "code")
            {
                googleUser = await ExchangeCodeForUserInfoAsync(request.Token);
            }
            else
            {
                throw new ArgumentException("Invalid token type. Must be 'id_token' or 'code'");
            }

            if (googleUser == null)
            {
                throw new UnauthorizedAccessException("Invalid Google token");
            }

            // Find or create user using UserService
            var (user, isNewUser) = await FindOrCreateUserAsync(googleUser);

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Create or update user session using UserService
            var userSession = await _userService.CreateOrUpdateUserSessionAsync(
                user.Id,
                refreshToken
            );

            // Get user profile data using UserService
            var userProfile = await _userService.GetUserProfileAsync(user.Id);

            _logger.LogInformation(
                "Google OAuth authentication successful for user {Email}. New user: {IsNewUser}",
                googleUser.Email,
                isNewUser
            );

            return new GoogleOAuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Match your JWT expiration
                User = googleUser,
                IsNewUser = isNewUser,
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
            _logger.LogError(ex, "Google OAuth authentication failed");
            throw;
        }
    }

    /// <summary>
    /// Validates a Google ID token and extracts user information
    /// </summary>
    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken)
    {
        try
        {
            // Validate the ID token with Google
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSettings.ClientId }
                }
            );

            return new GoogleUserInfo
            {
                Id = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                Picture = payload.Picture,
                EmailVerified = payload.EmailVerified,
                Locale = payload.Locale
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Google ID token");
            return null;
        }
    }

    /// <summary>
    /// Exchanges authorization code for access token and user info
    /// </summary>
    public async Task<GoogleUserInfo?> ExchangeCodeForUserInfoAsync(string authorizationCode)
    {
        try
        {
            // Create OAuth2 flow
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _googleSettings.ClientId,
                        ClientSecret = _googleSettings.ClientSecret
                    },
                    Scopes = _googleSettings.Scopes
                }
            );

            // Exchange code for token
            var token = await flow.ExchangeCodeForTokenAsync(
                userId: "user",
                code: authorizationCode,
                redirectUri: _googleSettings.RedirectUri,
                CancellationToken.None
            );

            // Get user info from Google API
            var userInfo = await GetUserInfoFromAccessTokenAsync(token.AccessToken);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange authorization code for user info");
            return null;
        }
    }

    /// <summary>
    /// Gets user information from Google using access token
    /// </summary>
    private async Task<GoogleUserInfo?> GetUserInfoFromAccessTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://www.googleapis.com/oauth2/v2/userinfo"
            );
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get user info from Google API. Status: {StatusCode}",
                    response.StatusCode
                );
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleApiUserInfo>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (userInfo == null)
            {
                return null;
            }

            return new GoogleUserInfo
            {
                Id = userInfo.Id,
                Email = userInfo.Email,
                Name = userInfo.Name,
                FirstName = userInfo.GivenName,
                LastName = userInfo.FamilyName,
                Picture = userInfo.Picture,
                EmailVerified = userInfo.VerifiedEmail,
                Locale = userInfo.Locale
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user info from access token");
            return null;
        }
    }

    /// <summary>
    /// Finds existing user or creates a new one based on Google user info
    /// </summary>
    private async Task<(User user, bool isNewUser)> FindOrCreateUserAsync(GoogleUserInfo googleUser)
    {
        // Try to find existing user by email using UserService
        var existingUser = await _userService.FindUserByEmailAsync(googleUser.Email);

        if (existingUser != null)
        {
            // Update user info if needed
            var updated = false;

            if (
                string.IsNullOrEmpty(existingUser.UserName)
                && !string.IsNullOrEmpty(googleUser.Email)
            )
            {
                existingUser.UserName = googleUser.Email;
                updated = true;
            }

            if (_googleSettings.AutoVerifyEmail && !existingUser.EmailConfirmed)
            {
                existingUser.EmailConfirmed = true;
                updated = true;
            }

            if (updated)
            {
                var updateSuccess = await _userService.UpdateUserAsync(existingUser);
                if (updateSuccess)
                {
                    _logger.LogInformation(
                        "Updated existing user {Email} with Google OAuth data",
                        googleUser.Email
                    );
                }
            }

            return (existingUser, false);
        }

        // Create new user using UserService
        var (newUser, success) = await _userService.CreateUserAsync(
            googleUser.Email,
            googleUser.Email,
            _googleSettings.AutoVerifyEmail && googleUser.EmailVerified
        );

        if (!success)
        {
            throw new InvalidOperationException(
                $"Failed to create user for email: {googleUser.Email}"
            );
        }

        _logger.LogInformation("Created new user from Google OAuth: {Email}", googleUser.Email);
        return (newUser, true);
    }

    /// <summary>
    /// Model for Google API user info response
    /// </summary>
    private class GoogleApiUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public bool VerifiedEmail { get; set; }
        public string? Locale { get; set; }
    }
}
