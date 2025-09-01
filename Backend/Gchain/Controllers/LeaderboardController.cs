using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gchain.Controllers;

/// <summary>
/// Controller for leaderboard and ranking functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        ILogger<LeaderboardController> logger
    )
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the leaderboard based on specified criteria
    /// </summary>
    /// <param name="request">Leaderboard request parameters</param>
    /// <returns>Leaderboard entries</returns>
    /// <response code="200">Leaderboard retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(LeaderboardResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] GetLeaderboardRequest request)
    {
        try
        {
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 20;

            var leaderboard = await _leaderboardService.GetLeaderboardAsync(request);
            
            // Mark current user in the results
            var currentUserId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                foreach (var entry in leaderboard.Entries)
                {
                    entry.IsCurrentUser = entry.UserId == currentUserId;
                }
            }

            _logger.LogInformation(
                "Retrieved leaderboard: Type={Type}, Page={Page}, Count={Count}",
                request.Type, request.Page, leaderboard.Entries.Count
            );

            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard");
            return StatusCode(500, new { error = "Failed to retrieve leaderboard" });
        }
    }

    /// <summary>
    /// Gets a specific user's rank and nearby players
    /// </summary>
    /// <param name="userId">User ID to get rank for</param>
    /// <param name="type">Leaderboard type</param>
    /// <param name="language">Language filter</param>
    /// <returns>User rank information</returns>
    /// <response code="200">User rank retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("user/{userId}/rank")]
    [ProducesResponseType(typeof(UserRankResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetUserRank(
        string userId,
        [FromQuery] LeaderboardType type = LeaderboardType.Overall,
        [FromQuery] string? language = null
    )
    {
        try
        {
            var request = new GetUserRankRequest
            {
                UserId = userId,
                Type = type,
                Language = language
            };

            var userRank = await _leaderboardService.GetUserRankAsync(request);

            _logger.LogInformation(
                "Retrieved user rank: UserId={UserId}, Type={Type}, Rank={Rank}",
                userId, type, userRank.UserEntry.Rank
            );

            return Ok(userRank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user rank for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve user rank" });
        }
    }

    /// <summary>
    /// Gets current user's rank and nearby players
    /// </summary>
    /// <param name="type">Leaderboard type</param>
    /// <param name="language">Language filter</param>
    /// <returns>Current user's rank information</returns>
    /// <response code="200">User rank retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("my-rank")]
    [ProducesResponseType(typeof(UserRankResponse), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetMyRank(
        [FromQuery] LeaderboardType type = LeaderboardType.Overall,
        [FromQuery] string? language = null
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var request = new GetUserRankRequest
            {
                UserId = currentUserId,
                Type = type,
                Language = language
            };

            var userRank = await _leaderboardService.GetUserRankAsync(request);

            _logger.LogInformation(
                "Retrieved current user rank: UserId={UserId}, Type={Type}, Rank={Rank}",
                currentUserId, type, userRank.UserEntry.Rank
            );

            return Ok(userRank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user rank");
            return StatusCode(500, new { error = "Failed to retrieve user rank" });
        }
    }

    /// <summary>
    /// Gets overall leaderboard statistics
    /// </summary>
    /// <returns>Leaderboard statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(LeaderboardStatsResponse), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetLeaderboardStats()
    {
        try
        {
            var stats = await _leaderboardService.GetLeaderboardStatsAsync();

            _logger.LogInformation("Retrieved leaderboard statistics");

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard stats");
            return StatusCode(500, new { error = "Failed to retrieve leaderboard statistics" });
        }
    }

    /// <summary>
    /// Gets top players for a specific category
    /// </summary>
    /// <param name="type">Leaderboard type</param>
    /// <param name="count">Number of top players to return</param>
    /// <param name="language">Language filter</param>
    /// <returns>Top players list</returns>
    /// <response code="200">Top players retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("top")]
    [ProducesResponseType(typeof(List<LeaderboardEntry>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetTopPlayers(
        [FromQuery] LeaderboardType type = LeaderboardType.Overall,
        [FromQuery] int count = 10,
        [FromQuery] string? language = null
    )
    {
        try
        {
            if (count < 1 || count > 50) count = 10;

            var topPlayers = await _leaderboardService.GetTopPlayersAsync(type, count, language);

            _logger.LogInformation(
                "Retrieved top players: Type={Type}, Count={Count}",
                type, topPlayers.Count
            );

            return Ok(topPlayers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top players");
            return StatusCode(500, new { error = "Failed to retrieve top players" });
        }
    }

    /// <summary>
    /// Gets current user's calculated rank
    /// </summary>
    /// <returns>User's current rank</returns>
    /// <response code="200">User rank calculated successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("my-rank/calculate")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> CalculateMyRank()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var rank = await _leaderboardService.CalculateUserRankAsync(currentUserId);

            _logger.LogInformation(
                "Calculated user rank: UserId={UserId}, Rank={Rank}",
                currentUserId, rank
            );

            return Ok(new { rank = rank });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate user rank");
            return StatusCode(500, new { error = "Failed to calculate user rank" });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
