using Gchain.DTOS;
using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Service for managing badges and user achievements
/// </summary>
public interface IBadgeService
{
    /// <summary>
    /// Creates a new badge
    /// </summary>
    Task<CreateBadgeResponse> CreateBadgeAsync(CreateBadgeRequest request);

    /// <summary>
    /// Updates an existing badge
    /// </summary>
    Task<bool> UpdateBadgeAsync(UpdateBadgeRequest request);

    /// <summary>
    /// Gets all available badges
    /// </summary>
    Task<List<BadgeResponse>> GetAllBadgesAsync(bool activeOnly = true);

    /// <summary>
    /// Gets a specific badge by ID
    /// </summary>
    Task<BadgeResponse?> GetBadgeByIdAsync(int badgeId);

    /// <summary>
    /// Gets user's badges with pagination and filtering
    /// </summary>
    Task<UserBadgesListResponse> GetUserBadgesAsync(GetUserBadgesRequest request);

    /// <summary>
    /// Gets user's badge statistics
    /// </summary>
    Task<BadgeStatsResponse> GetUserBadgeStatsAsync(string userId);

    /// <summary>
    /// Awards a badge to a user
    /// </summary>
    Task<AwardBadgeResponse> AwardBadgeAsync(AwardBadgeRequest request);

    /// <summary>
    /// Checks if a user is eligible for a specific badge
    /// </summary>
    Task<BadgeEligibilityResponse> CheckBadgeEligibilityAsync(CheckBadgeEligibilityRequest request);

    /// <summary>
    /// Gets user's progress for all badges
    /// </summary>
    Task<List<BadgeProgressResponse>> GetUserBadgeProgressAsync(string userId);

    /// <summary>
    /// Checks and awards eligible badges for a user based on game events
    /// </summary>
    Task<List<AwardBadgeResponse>> CheckAndAwardEligibleBadgesAsync(
        string userId,
        BadgeTriggerEvent triggerEvent
    );

    /// <summary>
    /// Deletes a badge (admin only)
    /// </summary>
    Task<bool> DeleteBadgeAsync(int badgeId);

    /// <summary>
    /// Gets badges by type
    /// </summary>
    Task<List<BadgeResponse>> GetBadgesByTypeAsync(BadgeType type, bool activeOnly = true);
}

/// <summary>
/// Events that can trigger badge eligibility checks
/// </summary>
public enum BadgeTriggerEvent
{
    GameCompleted,
    GameWon,
    FirstGame,
    WordGuessed,
    PerfectGame,
    StreakAchieved,
    TeamJoined,
    LevelUp,
    ScoreMilestone,
    TimeMilestone
}
