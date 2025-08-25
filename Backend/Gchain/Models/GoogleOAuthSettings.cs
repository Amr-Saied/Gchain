namespace Gchain.Models;

/// <summary>
/// Configuration settings for Google OAuth integration
/// </summary>
public class GoogleOAuthSettings
{
    /// <summary>
    /// Google OAuth Client ID from Google Console
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth Client Secret from Google Console
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI configured in Google Console
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes to request from Google
    /// </summary>
    public string[] Scopes { get; set; } = { "openid", "profile", "email" };

    /// <summary>
    /// Whether to auto-verify email addresses from Google
    /// </summary>
    public bool AutoVerifyEmail { get; set; } = true;
}
