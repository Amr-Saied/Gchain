using Gchain.Data;
using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Services;

/// <summary>
/// Service for managing leaderboards and user rankings
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(ApplicationDbContext context, ILogger<LeaderboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync(GetLeaderboardRequest request)
    {
        try
        {
            var query = BuildLeaderboardQuery(request);
            
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            
            var entries = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new LeaderboardResponse
            {
                Entries = entries,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                Type = request.Type,
                Language = request.Language,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard for type {Type}", request.Type);
            throw;
        }
    }

    public async Task<UserRankResponse> GetUserRankAsync(GetUserRankRequest request)
    {
        try
        {
            var leaderboardRequest = new GetLeaderboardRequest
            {
                Type = request.Type,
                Language = request.Language,
                PageSize = 1000 // Get more entries to find user's position
            };

            var leaderboard = await GetLeaderboardAsync(leaderboardRequest);
            var userEntry = leaderboard.Entries.FirstOrDefault(e => e.UserId == request.UserId);

            if (userEntry == null)
            {
                // User not found in leaderboard, return empty response
                return new UserRankResponse
                {
                    UserEntry = new LeaderboardEntry { UserId = request.UserId, Rank = 0 },
                    Type = request.Type,
                    Language = request.Language,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            // Get nearby entries (5 above and 5 below)
            var userIndex = leaderboard.Entries.FindIndex(e => e.UserId == request.UserId);
            var startIndex = Math.Max(0, userIndex - 5);
            var endIndex = Math.Min(leaderboard.Entries.Count, userIndex + 6);
            var nearbyEntries = leaderboard.Entries.GetRange(startIndex, endIndex - startIndex);

            return new UserRankResponse
            {
                UserEntry = userEntry,
                NearbyEntries = nearbyEntries,
                Type = request.Type,
                Language = request.Language,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user rank for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<LeaderboardStatsResponse> GetLeaderboardStatsAsync()
    {
        try
        {
            var totalPlayers = await _context.Users.CountAsync();
            
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var monthAgo = DateTime.UtcNow.AddDays(-30);
            
            var activePlayersThisWeek = await _context.WordGuesses
                .Where(wg => wg.CreatedAt >= weekAgo)
                .Select(wg => wg.UserId)
                .Distinct()
                .CountAsync();
                
            var activePlayersThisMonth = await _context.WordGuesses
                .Where(wg => wg.CreatedAt >= monthAgo)
                .Select(wg => wg.UserId)
                .Distinct()
                .CountAsync();

            var totalGamesPlayed = await _context.WordGuesses
                .Select(wg => wg.GameSessionId)
                .Distinct()
                .CountAsync();

            var totalGamesThisWeek = await _context.WordGuesses
                .Where(wg => wg.CreatedAt >= weekAgo)
                .Select(wg => wg.GameSessionId)
                .Distinct()
                .CountAsync();

            var totalGamesThisMonth = await _context.WordGuesses
                .Where(wg => wg.CreatedAt >= monthAgo)
                .Select(wg => wg.GameSessionId)
                .Distinct()
                .CountAsync();

            // Calculate average win rate
            var gamesWithWinners = await _context.GameSessions
                .Where(gs => gs.WinningTeamId != null)
                .CountAsync();

            var averageWinRate = totalGamesPlayed > 0 ? (double)gamesWithWinners / totalGamesPlayed * 100 : 0;

            // Get most popular language
            var mostPopularLanguage = await _context.GameSessions
                .GroupBy(gs => gs.Language)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.ToString())
                .FirstOrDefaultAsync() ?? "English";

            return new LeaderboardStatsResponse
            {
                TotalPlayers = totalPlayers,
                ActivePlayersThisWeek = activePlayersThisWeek,
                ActivePlayersThisMonth = activePlayersThisMonth,
                TotalGamesPlayed = totalGamesPlayed,
                TotalGamesThisWeek = totalGamesThisWeek,
                TotalGamesThisMonth = totalGamesThisMonth,
                AverageWinRate = averageWinRate,
                MostPopularLanguage = mostPopularLanguage,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard stats");
            throw;
        }
    }

    public async Task UpdateUserStatsAsync(string userId, int gameSessionId, bool won, int score, string language)
    {
        try
        {
            // This method can be enhanced to update cached statistics
            // For now, we'll just log the update
            _logger.LogInformation(
                "User {UserId} completed game {GameId}: Won={Won}, Score={Score}, Language={Language}",
                userId, gameSessionId, won, score, language
            );

            // Future enhancement: Update cached user statistics
            // This could involve updating a separate UserStats table or Redis cache
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(LeaderboardType type, int count = 10, string? language = null)
    {
        try
        {
            var request = new GetLeaderboardRequest
            {
                Type = type,
                Page = 1,
                PageSize = count,
                Language = language
            };

            var leaderboard = await GetLeaderboardAsync(request);
            return leaderboard.Entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top players for type {Type}", type);
            throw;
        }
    }

    public async Task<string> CalculateUserRankAsync(string userId)
    {
        try
        {
            var userStats = await GetUserGameStatsAsync(userId);
            
            // Simple ranking system based on experience points
            if (userStats.ExperiencePoints >= 1000) return "Master";
            if (userStats.ExperiencePoints >= 500) return "Expert";
            if (userStats.ExperiencePoints >= 200) return "Advanced";
            if (userStats.ExperiencePoints >= 50) return "Intermediate";
            return "Beginner";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate user rank for user {UserId}", userId);
            return "Beginner";
        }
    }

    private IQueryable<LeaderboardEntry> BuildLeaderboardQuery(GetLeaderboardRequest request)
    {
        var baseQuery = from user in _context.Users
                        select new LeaderboardEntry
                        {
                            UserId = user.Id,
                            UserName = user.UserName ?? user.Email ?? "Unknown",
                            ProfilePictureUrl = null, // Can be enhanced later
                            AccountCreated = user.CreatedAt,
                            TotalGamesPlayed = _context.WordGuesses
                                .Where(wg => wg.UserId == user.Id)
                                .Select(wg => wg.GameSessionId)
                                .Distinct()
                                .Count(),
                            TotalGamesWon = _context.GameSessions
                                .Where(gs => gs.WinningTeamId != null &&
                                           gs.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == user.Id && t.Id == gs.WinningTeamId)))
                                .Count(),
                            TotalScore = _context.WordGuesses
                                .Where(wg => wg.UserId == user.Id && wg.IsCorrect)
                                .Count() * 10, // Simple scoring: 10 points per correct guess
                            BadgesEarned = _context.UserBadges
                                .Where(ub => ub.UserId == user.Id)
                                .Count(),
                            ExperiencePoints = _context.WordGuesses
                                .Where(wg => wg.UserId == user.Id)
                                .Select(wg => wg.GameSessionId)
                                .Distinct()
                                .Count() * 10, // 10 XP per game played
                            LastGamePlayed = _context.WordGuesses
                                .Where(wg => wg.UserId == user.Id)
                                .OrderByDescending(wg => wg.CreatedAt)
                                .Select(wg => wg.CreatedAt)
                                .FirstOrDefault()
                        };

        // Apply filters based on request type
        switch (request.Type)
        {
            case LeaderboardType.Weekly:
                var weekAgo = DateTime.UtcNow.AddDays(-7);
                baseQuery = baseQuery.Where(entry => entry.LastGamePlayed >= weekAgo);
                break;
            case LeaderboardType.Monthly:
                var monthAgo = DateTime.UtcNow.AddDays(-30);
                baseQuery = baseQuery.Where(entry => entry.LastGamePlayed >= monthAgo);
                break;
            case LeaderboardType.Language when !string.IsNullOrEmpty(request.Language):
                // Filter by language - this would require joining with GameSessions
                // For now, we'll apply this filter after the main query
                break;
        }

        // Apply date range filters
        if (request.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(entry => entry.LastGamePlayed >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(entry => entry.LastGamePlayed <= request.ToDate.Value);
        }

        // Calculate derived fields
        var queryWithCalculations = baseQuery.Select(entry => new LeaderboardEntry
        {
            UserId = entry.UserId,
            UserName = entry.UserName,
            ProfilePictureUrl = entry.ProfilePictureUrl,
            AccountCreated = entry.AccountCreated,
            TotalGamesPlayed = entry.TotalGamesPlayed,
            TotalGamesWon = entry.TotalGamesWon,
            TotalScore = entry.TotalScore,
            BadgesEarned = entry.BadgesEarned,
            ExperiencePoints = entry.ExperiencePoints,
            LastGamePlayed = entry.LastGamePlayed,
            WinRate = entry.TotalGamesPlayed > 0 ? (double)entry.TotalGamesWon / entry.TotalGamesPlayed * 100 : 0,
            CurrentRank = entry.ExperiencePoints >= 1000 ? "Master" :
                         entry.ExperiencePoints >= 500 ? "Expert" :
                         entry.ExperiencePoints >= 200 ? "Advanced" :
                         entry.ExperiencePoints >= 50 ? "Intermediate" : "Beginner"
        });

        // Order by different criteria based on type
        return request.Type switch
        {
            LeaderboardType.WinRate => queryWithCalculations.OrderByDescending(e => e.WinRate).ThenByDescending(e => e.TotalGamesPlayed),
            LeaderboardType.Badges => queryWithCalculations.OrderByDescending(e => e.BadgesEarned).ThenByDescending(e => e.ExperiencePoints),
            LeaderboardType.Experience => queryWithCalculations.OrderByDescending(e => e.ExperiencePoints),
            _ => queryWithCalculations.OrderByDescending(e => e.ExperiencePoints).ThenByDescending(e => e.WinRate)
        };
    }

    private async Task<GameStatsSummary> GetUserGameStatsAsync(string userId)
    {
        var totalGamesPlayed = await _context.WordGuesses
            .Where(wg => wg.UserId == userId)
            .Select(wg => wg.GameSessionId)
            .Distinct()
            .CountAsync();

        var totalGamesWon = await _context.GameSessions
            .Where(gs => gs.WinningTeamId != null &&
                       gs.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId && t.Id == gs.WinningTeamId)))
            .CountAsync();

        var badgesEarned = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .CountAsync();

        return new GameStatsSummary
        {
            TotalGamesPlayed = totalGamesPlayed,
            TotalGamesWon = totalGamesWon,
            TotalScore = totalGamesPlayed * 10, // Simple scoring
            BadgesEarned = badgesEarned,
            CurrentRank = await CalculateUserRankAsync(userId),
            ExperiencePoints = totalGamesPlayed * 10
        };
    }
}
