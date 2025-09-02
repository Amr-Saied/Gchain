using System.Text.Json;
using Gchain.Data;
using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Services;

/// <summary>
/// Service for managing badges and user achievements
/// </summary>
public class BadgeService : IBadgeService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BadgeService> _logger;

    public BadgeService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<BadgeService> logger
    )
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CreateBadgeResponse> CreateBadgeAsync(CreateBadgeRequest request)
    {
        try
        {
            var badge = new Badge
            {
                Name = request.Name,
                Description = request.Description,
                Criteria = request.Criteria,
                IconUrl = request.IconUrl,
                Type = request.Type,
                RequiredValue = request.RequiredValue,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Badges.Add(badge);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created badge {BadgeId}: {BadgeName}", badge.Id, badge.Name);

            return new CreateBadgeResponse
            {
                BadgeId = badge.Id,
                Success = true,
                Message = "Badge created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create badge: {BadgeName}", request.Name);
            return new CreateBadgeResponse { Success = false, Message = "Failed to create badge" };
        }
    }

    public async Task<bool> UpdateBadgeAsync(UpdateBadgeRequest request)
    {
        try
        {
            var badge = await _context.Badges.FindAsync(request.Id);
            if (badge == null)
            {
                _logger.LogWarning("Badge {BadgeId} not found for update", request.Id);
                return false;
            }

            if (!string.IsNullOrEmpty(request.Name))
                badge.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                badge.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Criteria))
                badge.Criteria = request.Criteria;
            if (request.IconUrl != null)
                badge.IconUrl = request.IconUrl;
            if (request.Type.HasValue)
                badge.Type = request.Type.Value;
            if (request.RequiredValue.HasValue)
                badge.RequiredValue = request.RequiredValue;
            if (request.IsActive.HasValue)
                badge.IsActive = request.IsActive.Value;

            badge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated badge {BadgeId}: {BadgeName}", badge.Id, badge.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update badge {BadgeId}", request.Id);
            return false;
        }
    }

    public async Task<List<BadgeResponse>> GetAllBadgesAsync(bool activeOnly = true)
    {
        try
        {
            var query = _context.Badges.AsQueryable();

            if (activeOnly)
                query = query.Where(b => b.IsActive);

            var badges = await query
                .OrderBy(b => b.Type)
                .ThenBy(b => b.Name)
                .Select(b => new BadgeResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Criteria = b.Criteria,
                    IconUrl = b.IconUrl,
                    Type = b.Type,
                    RequiredValue = b.RequiredValue,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return badges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all badges");
            return new List<BadgeResponse>();
        }
    }

    public async Task<BadgeResponse?> GetBadgeByIdAsync(int badgeId)
    {
        try
        {
            var badge = await _context
                .Badges.Where(b => b.Id == badgeId)
                .Select(b => new BadgeResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Criteria = b.Criteria,
                    IconUrl = b.IconUrl,
                    Type = b.Type,
                    RequiredValue = b.RequiredValue,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return badge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge {BadgeId}", badgeId);
            return null;
        }
    }

    public async Task<UserBadgesListResponse> GetUserBadgesAsync(GetUserBadgesRequest request)
    {
        try
        {
            var earnedBadges = await _context
                .UserBadges.Include(ub => ub.Badge)
                .Where(ub => ub.UserId == request.UserId)
                .Select(ub => new UserBadgeResponse
                {
                    BadgeId = ub.BadgeId,
                    BadgeName = ub.Badge.Name,
                    BadgeDescription = ub.Badge.Description,
                    BadgeIconUrl = ub.Badge.IconUrl,
                    BadgeType = ub.Badge.Type,
                    EarnedAt = ub.EarnedAt,
                    Reason = ub.Reason,
                    IsRecentlyEarned = ub.EarnedAt >= DateTime.UtcNow.AddDays(-1)
                })
                .OrderByDescending(ub => ub.EarnedAt)
                .ToListAsync();

            var availableBadgesQuery = _context.Badges.Where(b =>
                b.IsActive
                && !_context.UserBadges.Any(ub => ub.UserId == request.UserId && ub.BadgeId == b.Id)
            );

            if (request.Type.HasValue)
                availableBadgesQuery = availableBadgesQuery.Where(b =>
                    b.Type == request.Type.Value
                );

            var availableBadges = await availableBadgesQuery
                .Select(b => new BadgeResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Criteria = b.Criteria,
                    IconUrl = b.IconUrl,
                    Type = b.Type,
                    RequiredValue = b.RequiredValue,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .OrderBy(b => b.Type)
                .ThenBy(b => b.Name)
                .ToListAsync();

            var totalCount = earnedBadges.Count + availableBadges.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new UserBadgesListResponse
            {
                EarnedBadges = earnedBadges,
                AvailableBadges = availableBadges,
                TotalEarned = earnedBadges.Count,
                TotalAvailable = availableBadges.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user badges for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<BadgeStatsResponse> GetUserBadgeStatsAsync(string userId)
    {
        try
        {
            var totalBadges = await _context.Badges.Where(b => b.IsActive).CountAsync();
            var earnedBadges = await _context
                .UserBadges.Where(ub => ub.UserId == userId)
                .CountAsync();
            var availableBadges = totalBadges - earnedBadges;

            var badgesByType = await _context
                .UserBadges.Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .GroupBy(ub => ub.Badge.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            var recentlyEarned = await _context
                .UserBadges.Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId && ub.EarnedAt >= DateTime.UtcNow.AddDays(-7))
                .Select(ub => new UserBadgeResponse
                {
                    BadgeId = ub.BadgeId,
                    BadgeName = ub.Badge.Name,
                    BadgeDescription = ub.Badge.Description,
                    BadgeIconUrl = ub.Badge.IconUrl,
                    BadgeType = ub.Badge.Type,
                    EarnedAt = ub.EarnedAt,
                    Reason = ub.Reason,
                    IsRecentlyEarned = true
                })
                .OrderByDescending(ub => ub.EarnedAt)
                .ToListAsync();

            return new BadgeStatsResponse
            {
                TotalBadges = totalBadges,
                EarnedBadges = earnedBadges,
                AvailableBadges = availableBadges,
                BadgesByType = badgesByType,
                RecentlyEarned = recentlyEarned,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AwardBadgeResponse> AwardBadgeAsync(AwardBadgeRequest request)
    {
        try
        {
            // Check if user already has this badge
            var existingUserBadge = await _context.UserBadges.FirstOrDefaultAsync(ub =>
                ub.UserId == request.UserId && ub.BadgeId == request.BadgeId
            );

            if (existingUserBadge != null)
            {
                return new AwardBadgeResponse
                {
                    Success = false,
                    Message = "User already has this badge",
                    IsNewBadge = false
                };
            }

            // Get the badge
            var badge = await _context.Badges.FindAsync(request.BadgeId);
            if (badge == null || !badge.IsActive)
            {
                return new AwardBadgeResponse
                {
                    Success = false,
                    Message = "Badge not found or inactive"
                };
            }

            // Create user badge
            var userBadge = new UserBadge
            {
                UserId = request.UserId,
                BadgeId = request.BadgeId,
                EarnedAt = DateTime.UtcNow,
                Reason = request.Reason
            };

            _context.UserBadges.Add(userBadge);
            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.CreateAchievementNotificationAsync(
                request.UserId,
                $"Congratulations! You earned the '{badge.Name}' badge!",
                request.BadgeId
            );

            // Send email notification
            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendBadgeAchievementEmailAsync(
                        user.Email,
                        user.UserName ?? user.Email,
                        badge,
                        request.Reason
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send badge achievement email to user {UserId}",
                    request.UserId
                );
                // Don't fail the badge award if email fails
            }

            _logger.LogInformation(
                "Awarded badge {BadgeId} to user {UserId}: {BadgeName}",
                request.BadgeId,
                request.UserId,
                badge.Name
            );

            return new AwardBadgeResponse
            {
                Success = true,
                Message = $"Badge '{badge.Name}' awarded successfully",
                IsNewBadge = true,
                Badge = new UserBadgeResponse
                {
                    BadgeId = badge.Id,
                    BadgeName = badge.Name,
                    BadgeDescription = badge.Description,
                    BadgeIconUrl = badge.IconUrl,
                    BadgeType = badge.Type,
                    EarnedAt = userBadge.EarnedAt,
                    Reason = userBadge.Reason,
                    IsRecentlyEarned = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to award badge {BadgeId} to user {UserId}",
                request.BadgeId,
                request.UserId
            );
            return new AwardBadgeResponse { Success = false, Message = "Failed to award badge" };
        }
    }

    public async Task<BadgeEligibilityResponse> CheckBadgeEligibilityAsync(
        CheckBadgeEligibilityRequest request
    )
    {
        try
        {
            var badge = await _context.Badges.FindAsync(request.BadgeId);
            if (badge == null || !badge.IsActive)
            {
                return new BadgeEligibilityResponse
                {
                    IsEligible = false,
                    Message = "Badge not found or inactive"
                };
            }

            // Check if user already has this badge
            var hasBadge = await _context.UserBadges.AnyAsync(ub =>
                ub.UserId == request.UserId && ub.BadgeId == request.BadgeId
            );

            if (hasBadge)
            {
                return new BadgeEligibilityResponse
                {
                    IsEligible = false,
                    Message = "User already has this badge"
                };
            }

            // Calculate progress based on badge criteria
            var progress = await CalculateBadgeProgressAsync(request.UserId, badge);
            var required = badge.RequiredValue ?? 1;

            return new BadgeEligibilityResponse
            {
                IsEligible = progress >= required,
                Message = progress >= required ? "Eligible for badge" : "Not yet eligible",
                CurrentProgress = progress,
                RequiredProgress = required,
                ProgressPercentage = required > 0 ? (double)progress / required * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check badge eligibility for user {UserId}, badge {BadgeId}",
                request.UserId,
                request.BadgeId
            );
            return new BadgeEligibilityResponse
            {
                IsEligible = false,
                Message = "Error checking eligibility"
            };
        }
    }

    public async Task<List<BadgeProgressResponse>> GetUserBadgeProgressAsync(string userId)
    {
        try
        {
            var badges = await _context.Badges.Where(b => b.IsActive).ToListAsync();

            var userBadges = await _context
                .UserBadges.Where(ub => ub.UserId == userId)
                .ToDictionaryAsync(ub => ub.BadgeId, ub => ub);

            var progressList = new List<BadgeProgressResponse>();

            foreach (var badge in badges)
            {
                var hasBadge = userBadges.ContainsKey(badge.Id);
                var progress = hasBadge
                    ? badge.RequiredValue ?? 1
                    : await CalculateBadgeProgressAsync(userId, badge);
                var required = badge.RequiredValue ?? 1;

                progressList.Add(
                    new BadgeProgressResponse
                    {
                        BadgeId = badge.Id,
                        BadgeName = badge.Name,
                        BadgeDescription = badge.Description,
                        BadgeType = badge.Type,
                        CurrentProgress = progress,
                        RequiredProgress = required,
                        ProgressPercentage = required > 0 ? (double)progress / required * 100 : 0,
                        IsEarned = hasBadge,
                        EarnedAt = hasBadge ? userBadges[badge.Id].EarnedAt : null
                    }
                );
            }

            return progressList.OrderBy(b => b.BadgeType).ThenBy(b => b.BadgeName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge progress for user {UserId}", userId);
            return new List<BadgeProgressResponse>();
        }
    }

    public async Task<List<AwardBadgeResponse>> CheckAndAwardEligibleBadgesAsync(
        string userId,
        BadgeTriggerEvent triggerEvent
    )
    {
        try
        {
            var awardedBadges = new List<AwardBadgeResponse>();

            // Get badges that might be triggered by this event
            var relevantBadges = await GetRelevantBadgesForEventAsync(triggerEvent);

            foreach (var badge in relevantBadges)
            {
                // Check if user already has this badge
                var hasBadge = await _context.UserBadges.AnyAsync(ub =>
                    ub.UserId == userId && ub.BadgeId == badge.Id
                );

                if (hasBadge)
                    continue;

                // Check eligibility
                var progress = await CalculateBadgeProgressAsync(userId, badge);
                var required = badge.RequiredValue ?? 1;

                if (progress >= required)
                {
                    var awardRequest = new AwardBadgeRequest
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        Reason = $"Earned through {triggerEvent}"
                    };

                    var result = await AwardBadgeAsync(awardRequest);
                    if (result.Success)
                    {
                        awardedBadges.Add(result);
                    }
                }
            }

            return awardedBadges;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check and award eligible badges for user {UserId}, event {Event}",
                userId,
                triggerEvent
            );
            return new List<AwardBadgeResponse>();
        }
    }

    public async Task<bool> DeleteBadgeAsync(int badgeId)
    {
        try
        {
            var badge = await _context.Badges.FindAsync(badgeId);
            if (badge == null)
            {
                _logger.LogWarning("Badge {BadgeId} not found for deletion", badgeId);
                return false;
            }

            // Remove all user badges first
            var userBadges = await _context
                .UserBadges.Where(ub => ub.BadgeId == badgeId)
                .ToListAsync();
            _context.UserBadges.RemoveRange(userBadges);

            // Remove the badge
            _context.Badges.Remove(badge);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted badge {BadgeId}: {BadgeName}", badge.Id, badge.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete badge {BadgeId}", badgeId);
            return false;
        }
    }

    public async Task<List<BadgeResponse>> GetBadgesByTypeAsync(
        BadgeType type,
        bool activeOnly = true
    )
    {
        try
        {
            var query = _context.Badges.Where(b => b.Type == type);

            if (activeOnly)
                query = query.Where(b => b.IsActive);

            var badges = await query
                .OrderBy(b => b.Name)
                .Select(b => new BadgeResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Criteria = b.Criteria,
                    IconUrl = b.IconUrl,
                    Type = b.Type,
                    RequiredValue = b.RequiredValue,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return badges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badges by type {Type}", type);
            return new List<BadgeResponse>();
        }
    }

    private async Task<int> CalculateBadgeProgressAsync(string userId, Badge badge)
    {
        try
        {
            // Parse criteria and calculate progress based on badge type and criteria
            var criteria = badge.Criteria.ToLower();

            return badge.Type switch
            {
                BadgeType.Milestone => await CalculateMilestoneProgressAsync(userId, criteria),
                BadgeType.Skill => await CalculateSkillProgressAsync(userId, criteria),
                BadgeType.Social => await CalculateSocialProgressAsync(userId, criteria),
                BadgeType.Achievement => await CalculateAchievementProgressAsync(userId, criteria),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate progress for badge {BadgeId}", badge.Id);
            return 0;
        }
    }

    private async Task<int> CalculateMilestoneProgressAsync(string userId, string criteria)
    {
        if (criteria.Contains("games_played"))
        {
            return await _context
                .WordGuesses.Where(wg => wg.UserId == userId)
                .Select(wg => wg.GameSessionId)
                .Distinct()
                .CountAsync();
        }
        else if (criteria.Contains("games_won"))
        {
            return await _context
                .GameSessions.Where(gs =>
                    gs.WinningTeamId != null
                    && gs.Teams.Any(t =>
                        t.TeamMembers.Any(m => m.UserId == userId && t.Id == gs.WinningTeamId)
                    )
                )
                .CountAsync();
        }
        else if (criteria.Contains("correct_guesses"))
        {
            return await _context
                .WordGuesses.Where(wg => wg.UserId == userId && wg.IsCorrect)
                .CountAsync();
        }

        return 0;
    }

    private async Task<int> CalculateSkillProgressAsync(string userId, string criteria)
    {
        if (criteria.Contains("perfect_game"))
        {
            // Count games where user made no mistakes
            return await _context
                .GameSessions.Where(gs =>
                    gs.Teams.Any(t =>
                        t.TeamMembers.Any(m => m.UserId == userId && m.MistakesRemaining == 3)
                    )
                )
                .CountAsync();
        }
        else if (criteria.Contains("streak"))
        {
            // Calculate current win streak
            var recentGames = await _context
                .GameSessions.Where(gs =>
                    gs.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId))
                )
                .OrderByDescending(gs => gs.CreatedAt)
                .Take(10)
                .ToListAsync();

            int streak = 0;
            foreach (var game in recentGames)
            {
                var userTeam = game.Teams.FirstOrDefault(t =>
                    t.TeamMembers.Any(m => m.UserId == userId)
                );
                if (userTeam != null && game.WinningTeamId == userTeam.Id)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        return 0;
    }

    private async Task<int> CalculateSocialProgressAsync(string userId, string criteria)
    {
        if (criteria.Contains("team_player"))
        {
            return await _context.TeamMembers.Where(tm => tm.UserId == userId).CountAsync();
        }

        return 0;
    }

    private Task<int> CalculateAchievementProgressAsync(string userId, string criteria)
    {
        // Generic achievement progress calculation
        return Task.FromResult(0);
    }

    private async Task<List<Badge>> GetRelevantBadgesForEventAsync(BadgeTriggerEvent triggerEvent)
    {
        var relevantCriteria = triggerEvent switch
        {
            BadgeTriggerEvent.GameCompleted => new[] { "games_played", "games_won" },
            BadgeTriggerEvent.GameWon => new[] { "games_won", "streak" },
            BadgeTriggerEvent.FirstGame => new[] { "first_game" },
            BadgeTriggerEvent.WordGuessed => new[] { "correct_guesses" },
            BadgeTriggerEvent.PerfectGame => new[] { "perfect_game" },
            BadgeTriggerEvent.StreakAchieved => new[] { "streak" },
            BadgeTriggerEvent.TeamJoined => new[] { "team_player" },
            _ => new string[0]
        };

        var badges = new List<Badge>();
        foreach (var criteria in relevantCriteria)
        {
            var matchingBadges = await _context
                .Badges.Where(b => b.IsActive && b.Criteria.ToLower().Contains(criteria))
                .ToListAsync();
            badges.AddRange(matchingBadges);
        }

        return badges.DistinctBy(b => b.Id).ToList();
    }
}
