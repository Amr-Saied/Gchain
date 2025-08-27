namespace Gchain.Models;

/// <summary>
/// Enum for different authentication providers
/// </summary>
public enum AuthProvider
{
    /// <summary>
    /// Google OAuth authentication
    /// </summary>
    Google = 1,

    /// <summary>
    /// Guest authentication (no credentials)
    /// </summary>
    Guest = 2
}
