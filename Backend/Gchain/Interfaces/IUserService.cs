using Gchain.DTOS;
using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user in the database
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="userName">User's username</param>
    /// <param name="emailConfirmed">Whether email is confirmed</param>
    /// <returns>Created user and success status</returns>
    Task<(User user, bool success)> CreateUserAsync(
        string email,
        string userName,
        bool emailConfirmed
    );

    /// <summary>
    /// Finds an existing user by email
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> FindUserByEmailAsync(string email);

    /// <summary>
    /// Updates user information
    /// </summary>
    /// <param name="user">User to update</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateUserAsync(User user);

    /// <summary>
    /// Creates a new user session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="deviceInfo">Optional device information</param>
    /// <param name="ipAddress">Optional IP address</param>
    /// <param name="expirationDays">Session expiration in days (default: 7)</param>
    /// <returns>Created user session</returns>
    Task<UserSession> CreateUserSessionAsync(
        string userId,
        string refreshToken,
        string? deviceInfo = null,
        string? ipAddress = null,
        int expirationDays = 7
    );

    /// <summary>
    /// Checks if a browser already has an active guest session
    /// </summary>
    /// <param name="browserId">Browser identifier (IP + User-Agent hash)</param>
    /// <returns>Existing guest session if found, null otherwise</returns>
    Task<UserSession?> CheckForExistingGuestSessionAsync(string browserId);

    /// <summary>
    /// Updates user session information
    /// </summary>
    /// <param name="userSession">User session to update</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateUserSessionAsync(UserSession userSession);

    /// <summary>
    /// Gets user profile data including game statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile data</returns>
    Task<UserProfileData> GetUserProfileAsync(string userId);

    /// <summary>
    /// Gets user game statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Game statistics</returns>
    Task<GameStatsSummary> GetUserGameStatsAsync(string userId);

    /// <summary>
    /// Finds an existing user by username
    /// </summary>
    /// <param name="username">Username to search for</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> FindUserByUsernameAsync(string username);

    /// <summary>
    /// Creates a new guest user in the database
    /// </summary>
    /// <param name="username">Guest username</param>
    /// <param name="preferences">Optional user preferences JSON</param>
    /// <returns>Created user and success status</returns>
    Task<(User user, bool success)> CreateGuestUserAsync(string username, string? preferences);
}
