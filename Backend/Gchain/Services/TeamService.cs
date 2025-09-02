using Gchain.Data;
using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gchain.Services;

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamService> _logger;

    public TeamService(ApplicationDbContext context, ILogger<TeamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TeamDto?> GetTeamAsync(int teamId)
    {
        try
        {
            var team = await _context
                .Teams.Include(t => t.GameSession)
                .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                _logger.LogWarning("Team with ID {TeamId} not found", teamId);
                return null;
            }

            var teamMembers = team
                .TeamMembers.Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    UserId = tm.UserId,
                    UserName = tm.User.UserName ?? "Unknown",
                    MistakesRemaining = tm.MistakesRemaining,
                    IsActive = tm.IsActive,
                    JoinOrder = tm.JoinOrder
                })
                .ToList();

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Color = team.Color,
                RoundsWon = team.RoundsWon,
                RevivalUsed = team.RevivalUsed,
                TeamMembers = teamMembers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team with ID {TeamId}", teamId);
            return null;
        }
    }

    public async Task<List<TeamMemberDto>> GetTeamMembersAsync(int teamId)
    {
        try
        {
            var teamMembers = await _context
                .TeamMembers.Include(tm => tm.User)
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    UserId = tm.UserId,
                    UserName = tm.User.UserName ?? "Unknown",
                    MistakesRemaining = tm.MistakesRemaining,
                    IsActive = tm.IsActive,
                    JoinOrder = tm.JoinOrder
                })
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} members for team {TeamId}",
                teamMembers.Count,
                teamId
            );
            return teamMembers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team members for team {TeamId}", teamId);
            return new List<TeamMemberDto>();
        }
    }

    public async Task<bool> CanJoinTeamAsync(int teamId, string userId)
    {
        try
        {
            // Check if team exists
            var team = await _context
                .Teams.Include(t => t.GameSession)
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                _logger.LogWarning("Team {TeamId} not found", teamId);
                return false;
            }

            // Check if game session is active (waiting for players)
            if (!team.GameSession.IsActive)
            {
                _logger.LogWarning(
                    "Game session {GameSessionId} is not active",
                    team.GameSessionId
                );
                return false;
            }

            // Check if user is already in this team
            var existingMember = team.TeamMembers.FirstOrDefault(tm => tm.UserId == userId);
            if (existingMember != null)
            {
                _logger.LogInformation(
                    "User {UserId} is already a member of team {TeamId}",
                    userId,
                    teamId
                );
                return false;
            }

            // Check if user is already in another team in the same game
            var userInOtherTeam = await _context.TeamMembers.AnyAsync(tm =>
                tm.UserId == userId && tm.Team.GameSessionId == team.GameSessionId
            );

            if (userInOtherTeam)
            {
                _logger.LogWarning(
                    "User {UserId} is already in another team in game {GameSessionId}",
                    userId,
                    team.GameSessionId
                );
                return false;
            }

            // Check if team is full (max 4 players per team)
            if (team.TeamMembers.Count >= 4)
            {
                _logger.LogWarning("Team {TeamId} is full", teamId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if user {UserId} can join team {TeamId}",
                userId,
                teamId
            );
            return false;
        }
    }

    public async Task<bool> IsTeamFullAsync(int teamId)
    {
        try
        {
            var memberCount = await _context.TeamMembers.CountAsync(tm => tm.TeamId == teamId);

            return memberCount >= 4; // Max 4 players per team
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if team {TeamId} is full", teamId);
            return true; // Assume full on error to be safe
        }
    }

    public async Task<int> GetTeamMemberCountAsync(int teamId)
    {
        try
        {
            return await _context.TeamMembers.CountAsync(tm => tm.TeamId == teamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member count for team {TeamId}", teamId);
            return 0;
        }
    }
}
