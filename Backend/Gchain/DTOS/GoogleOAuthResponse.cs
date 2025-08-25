namespace Gchain.DTOS;

/// <summary>
/// Response model for successful Google OAuth authentication
/// </summary>
public class GoogleOAuthResponse
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
    public GoogleUserInfo User { get; set; } = new();

    /// <summary>
    /// Whether this is a new user registration
    /// </summary>
    public bool IsNewUser { get; set; }

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
/// User information from Google OAuth
/// </summary>
public class GoogleUserInfo
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// URL to user's profile picture
    /// </summary>
    public string Picture { get; set; } = string.Empty;

    /// <summary>
    /// Whether email is verified by Google
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// User's locale/language preference
    /// </summary>
    public string? Locale { get; set; }
}

/// <summary>
/// User session information for database tracking
/// </summary>
public class UserSessionInfo
{
    /// <summary>
    /// Session ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for this session
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the session expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Comprehensive user profile data from database
/// </summary>
public class UserProfileData
{
    /// <summary>
    /// User's unique identifier in the database
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// User's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether phone number is confirmed
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Whether two-factor authentication is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// When the account was locked out
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Whether the account is locked
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// User preferences (JSON string)
    /// </summary>
    public string? Preferences { get; set; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User's profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// User's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's bio/description
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// User's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's location/country
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// User's website URL
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// User's social media links
    /// </summary>
    public string? SocialMediaLinks { get; set; }

    /// <summary>
    /// User's timezone
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// User's language preference
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Whether the user profile is public
    /// </summary>
    public bool IsProfilePublic { get; set; } = true;

    /// <summary>
    /// User's game statistics summary
    /// </summary>
    public GameStatsSummary? GameStats { get; set; }
}

/// <summary>
/// Game statistics summary for the user
/// </summary>
public class GameStatsSummary
{
    /// <summary>
    /// Total number of games played
    /// </summary>
    public int TotalGamesPlayed { get; set; }

    /// <summary>
    /// Total number of games won
    /// </summary>
    public int TotalGamesWon { get; set; }

    /// <summary>
    /// Win percentage
    /// </summary>
    public double WinPercentage =>
        TotalGamesPlayed > 0 ? (double)TotalGamesWon / TotalGamesPlayed * 100 : 0;

    /// <summary>
    /// Total score accumulated
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// Average score per game
    /// </summary>
    public double AverageScore => TotalGamesPlayed > 0 ? (double)TotalScore / TotalGamesPlayed : 0;

    /// <summary>
    /// Number of badges earned
    /// </summary>
    public int BadgesEarned { get; set; }

    /// <summary>
    /// Current rank/level
    /// </summary>
    public string? CurrentRank { get; set; }

    /// <summary>
    /// Experience points
    /// </summary>
    public int ExperiencePoints { get; set; }

    /// <summary>
    /// Last game played date
    /// </summary>
    public DateTime? LastGamePlayed { get; set; }
}
