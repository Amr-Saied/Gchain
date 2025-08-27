namespace Gchain.DTOS;

/// <summary>
/// Response model for guest authentication - mirrors GoogleOAuthResponse structure
/// </summary>
public class GuestAuthResponse
{
    /// <summary>
    /// JWT access token for authenticated user
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for token renewal
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in UTC
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public GuestUserInfo User { get; set; } = new();

    /// <summary>
    /// Whether this is a new user registration (always true for guests)
    /// </summary>
    public bool IsNewUser { get; set; } = true;

    /// <summary>
    /// Database user ID for the authenticated user
    /// </summary>
    public string DatabaseUserId { get; set; } = string.Empty;

    /// <summary>
    /// User session information
    /// </summary>
    public UserSessionInfo? UserSession { get; set; }

    /// <summary>
    /// Comprehensive user profile data
    /// </summary>
    public UserProfileData? UserProfile { get; set; }
}

/// <summary>
/// Guest user information (parallel to GoogleUserInfo)
/// </summary>
public class GuestUserInfo
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (always empty for guests)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name (generated guest name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = "Guest";

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// URL to user's profile picture (could be default avatar)
    /// </summary>
    public string Picture { get; set; } = string.Empty;

    /// <summary>
    /// Whether email is verified by provider (always false for guests)
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// User's locale/language preference (not used for guests)
    /// </summary>
    public string? Locale { get; set; } = null;
}
