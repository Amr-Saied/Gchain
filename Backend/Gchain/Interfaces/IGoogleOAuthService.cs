using Gchain.DTOS;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for Google OAuth authentication services
/// </summary>
public interface IGoogleOAuthService
{
    /// <summary>
    /// Authenticates a user using Google OAuth token
    /// </summary>
    /// <param name="request">Google OAuth request containing token and token type</param>
    /// <returns>Authentication response with JWT tokens and user info</returns>
    Task<GoogleOAuthResponse> AuthenticateAsync(GoogleOAuthRequest request);

    /// <summary>
    /// Validates a Google ID token and extracts user information
    /// </summary>
    /// <param name="idToken">Google ID token to validate</param>
    /// <returns>Google user information if token is valid</returns>
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken);

    /// <summary>
    /// Exchanges authorization code for access token and user info
    /// </summary>
    /// <param name="authorizationCode">Authorization code from Google</param>
    /// <returns>Google user information if code is valid</returns>
    Task<GoogleUserInfo?> ExchangeCodeForUserInfoAsync(string authorizationCode);
}
