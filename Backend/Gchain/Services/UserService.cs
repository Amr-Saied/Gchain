using Gchain.Data;
using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        ILogger<UserService> logger
    )
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user in the database
    /// </summary>
    public async Task<(User user, bool success)> CreateUserAsync(
        string email,
        string userName,
        bool emailConfirmed
    )
    {
        try
        {
            var newUser = new User
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = emailConfirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user {Email}: {Errors}", email, errors);
                return (newUser, false);
            }

            _logger.LogInformation("Successfully created new user: {Email}", email);
            return (newUser, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating user {Email}", email);
            return (new User(), false);
        }
    }

    /// <summary>
    /// Finds an existing user by email
    /// </summary>
    public async Task<User?> FindUserByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while finding user by email {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Updates user information
    /// </summary>
    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully updated user: {Email}", user.Email);
                return true;
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update user {Email}: {Errors}", user.Email, errors);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating user {Email}", user.Email);
            return false;
        }
    }

    /// <summary>
    /// Creates a new user session
    /// </summary>
    public async Task<UserSession> CreateUserSessionAsync(
        string userId,
        string refreshToken,
        string? deviceInfo = null,
        string? ipAddress = null,
        int expirationDays = 7
    )
    {
        try
        {
            var newSession = new UserSession
            {
                UserId = userId,
                RefreshToken = refreshToken,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.UserSessions.Add(newSession);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created new user session for user {UserId}", userId);
            return newSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user session for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Finds an active user session by its refresh token
    /// </summary>
    public async Task<UserSession?> FindActiveSessionByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            return await _dbContext
                .UserSessions.Include(us => us.User)
                .FirstOrDefaultAsync(us =>
                    us.RefreshToken == refreshToken && us.IsActive && us.ExpiresAt > DateTime.UtcNow
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find session by refresh token");
            return null;
        }
    }

    /// <summary>
    /// Gets user profile data including game statistics
    /// </summary>
    public async Task<UserProfileData> GetUserProfileAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for profile generation: {UserId}", userId);
                return new UserProfileData();
            }

            // Get game statistics
            var gameStats = await GetUserGameStatsAsync(userId);

            return new UserProfileData
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                Preferences = user.Preferences,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ProfilePictureUrl = null, // Can be enhanced later
                DisplayName = user.UserName,
                Bio = null, // Can be enhanced later
                DateOfBirth = null, // Can be enhanced later
                Location = null, // Can be enhanced later
                Website = null, // Can be enhanced later
                SocialMediaLinks = null, // Can be enhanced later
                Timezone = null, // Can be enhanced later
                Language = null, // Can be enhanced later
                IsProfilePublic = true,
                GameStats = gameStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build user profile for user {UserId}", userId);
            // Return basic profile if statistics fail
            return new UserProfileData { Id = userId, IsProfilePublic = true };
        }
    }

    /// <summary>
    /// Gets user game statistics
    /// </summary>
    public async Task<GameStatsSummary> GetUserGameStatsAsync(string userId)
    {
        try
        {
            // Get word guesses count
            var totalGamesPlayed = await _dbContext
                .WordGuesses.Where(wg => wg.UserId == userId)
                .Select(wg => wg.GameSessionId)
                .Distinct()
                .CountAsync();

            // Get badges count
            var badgesEarned = await _dbContext
                .UserBadges.Where(ub => ub.UserId == userId)
                .CountAsync();

            // Get last game played
            var lastGamePlayed = await _dbContext
                .WordGuesses.Where(wg => wg.UserId == userId)
                .OrderByDescending(wg => wg.CreatedAt)
                .Select(wg => wg.CreatedAt)
                .FirstOrDefaultAsync();

            return new GameStatsSummary
            {
                TotalGamesPlayed = totalGamesPlayed,
                TotalGamesWon = 0, // Enhance based on your game logic
                TotalScore = 0, // Enhance based on your scoring system
                BadgesEarned = badgesEarned,
                CurrentRank = "Beginner", // Enhance based on your ranking system
                ExperiencePoints = totalGamesPlayed * 10, // Basic XP calculation
                LastGamePlayed = lastGamePlayed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build game stats for user {UserId}", userId);
            return new GameStatsSummary
            {
                TotalGamesPlayed = 0,
                TotalGamesWon = 0,
                TotalScore = 0,
                BadgesEarned = 0,
                CurrentRank = "Beginner",
                ExperiencePoints = 0
            };
        }
    }

    /// <summary>
    /// Finds an existing user by username
    /// </summary>
    public async Task<User?> FindUserByUsernameAsync(string username)
    {
        try
        {
            return await _userManager.FindByNameAsync(username);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while finding user by username {Username}",
                username
            );
            return null;
        }
    }

    /// <summary>
    /// Creates a new guest user in the database
    /// </summary>
    public async Task<(User user, bool success)> CreateGuestUserAsync(
        string username,
        string? preferences
    )
    {
        try
        {
            var newUser = new User
            {
                UserName = username,
                Email = $"{username}@guest.local", // Dummy email for Identity requirements
                EmailConfirmed = false,
                AuthProvider = AuthProvider.Guest,
                Preferences = preferences,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError(
                    "Failed to create guest user {Username}: {Errors}",
                    username,
                    errors
                );
                return (newUser, false);
            }

            _logger.LogInformation("Successfully created new guest user: {Username}", username);
            return (newUser, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while creating guest user {Username}",
                username
            );
            return (new User(), false);
        }
    }

    /// <summary>
    /// Checks if a browser already has an active guest session within 1 hour
    /// </summary>
    public async Task<UserSession?> CheckForExistingGuestSessionAsync(string browserId)
    {
        try
        {
            // Find active guest session for this browser within 1 hour
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var existingSession = await _dbContext
                .UserSessions.Include(us => us.User)
                .FirstOrDefaultAsync(us =>
                    us.DeviceInfo == browserId
                    && us.IsActive
                    && us.ExpiresAt > DateTime.UtcNow
                    && us.User.AuthProvider == AuthProvider.Guest
                    && us.CreatedAt > oneHourAgo // Session must be created within last hour
                );

            if (existingSession != null)
            {
                _logger.LogInformation(
                    "Found active guest session within 1 hour for browser {BrowserId}, user: {Username}",
                    browserId,
                    existingSession.User.UserName
                );
            }
            else
            {
                _logger.LogInformation(
                    "No active guest session within 1 hour for browser {BrowserId}, will create new guest",
                    browserId
                );
            }

            return existingSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check for existing guest session for browser {BrowserId}",
                browserId
            );
            return null;
        }
    }

    /// <summary>
    /// Updates user session information
    /// </summary>
    public async Task<bool> UpdateUserSessionAsync(UserSession userSession)
    {
        try
        {
            _dbContext.UserSessions.Update(userSession);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user session {SessionId}", userSession.Id);
            return false;
        }
    }
}
