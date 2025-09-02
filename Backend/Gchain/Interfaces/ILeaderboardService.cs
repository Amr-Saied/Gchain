using Gchain.DTOS;

namespace Gchain.Interfaces;

/// <summary>
/// Service for managing leaderboards and user rankings
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Gets the leaderboard based on specified criteria
    /// </summary>
    Task<LeaderboardResponse> GetLeaderboardAsync(GetLeaderboardRequest request);

    /// <summary>
    /// Gets a specific user's rank and nearby players
    /// </summary>
    Task<UserRankResponse> GetUserRankAsync(GetUserRankRequest request);

    /// <summary>
    /// Gets overall leaderboard statistics
    /// </summary>
    Task<LeaderboardStatsResponse> GetLeaderboardStatsAsync();

    /// <summary>
    /// Updates user statistics after a game completion
    /// </summary>
    Task UpdateUserStatsAsync(string userId, int gameSessionId, bool won, int score, string language);

    /// <summary>
    /// Gets top players for a specific category
    /// </summary>
    Task<List<LeaderboardEntry>> GetTopPlayersAsync(LeaderboardType type, int count = 10, string? language = null);

    /// <summary>
    /// Calculates user's current rank based on their stats
    /// </summary>
    Task<string> CalculateUserRankAsync(string userId);
}
