using Gchain.DTOS;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for guest authentication operations
/// </summary>
public interface IGuestAuthService
{
    /// <summary>
    /// Creates a new guest user and returns authentication response
    /// </summary>
    /// <param name="httpContext">HTTP context for browser identification</param>
    /// <returns>Authentication response with JWT tokens and user information</returns>
    Task<GuestAuthResponse> CreateGuestAsync(HttpContext httpContext);

    /// <summary>
    /// Generates a unique guest username
    /// </summary>
    /// <returns>Unique username in format Guest_&lt;number&gt;</returns>
    Task<string> GenerateUniqueGuestUsernameAsync();
}
