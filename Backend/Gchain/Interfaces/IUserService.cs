using Gchain.Models;
using Gchain.DTOS;

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
    /// Creates or updates a user session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>User session</returns>
    Task<UserSession> CreateOrUpdateUserSessionAsync(string userId, string refreshToken);

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
}
