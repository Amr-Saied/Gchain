using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers;

/// <summary>
/// Controller for managing badges and user achievements
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BadgeController : ControllerBase
{
    private readonly IBadgeService _badgeService;
    private readonly ILogger<BadgeController> _logger;

    public BadgeController(IBadgeService badgeService, ILogger<BadgeController> logger)
    {
        _badgeService = badgeService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available badges
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BadgeResponse>>> GetAllBadges(
        [FromQuery] bool activeOnly = true
    )
    {
        try
        {
            var badges = await _badgeService.GetAllBadgesAsync(activeOnly);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all badges");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific badge by ID
    /// </summary>
    [HttpGet("{badgeId}")]
    public async Task<ActionResult<BadgeResponse>> GetBadge(int badgeId)
    {
        try
        {
            var badge = await _badgeService.GetBadgeByIdAsync(badgeId);
            if (badge == null)
            {
                return NotFound($"Badge with ID {badgeId} not found");
            }

            return Ok(badge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge {BadgeId}", badgeId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets badges by type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<ActionResult<List<BadgeResponse>>> GetBadgesByType(
        BadgeType type,
        [FromQuery] bool activeOnly = true
    )
    {
        try
        {
            var badges = await _badgeService.GetBadgesByTypeAsync(type, activeOnly);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badges by type {Type}", type);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets user's badges with pagination and filtering
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<UserBadgesListResponse>> GetUserBadges(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BadgeType? type = null,
        [FromQuery] bool? isEarned = null
    )
    {
        try
        {
            var request = new GetUserBadgesRequest
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize,
                Type = type,
                IsEarned = isEarned
            };

            var result = await _badgeService.GetUserBadgesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user badges for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets user's badge statistics
    /// </summary>
    [HttpGet("user/{userId}/stats")]
    public async Task<ActionResult<BadgeStatsResponse>> GetUserBadgeStats(string userId)
    {
        try
        {
            var stats = await _badgeService.GetUserBadgeStatsAsync(userId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge stats for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets user's progress for all badges
    /// </summary>
    [HttpGet("user/{userId}/progress")]
    public async Task<ActionResult<List<BadgeProgressResponse>>> GetUserBadgeProgress(string userId)
    {
        try
        {
            var progress = await _badgeService.GetUserBadgeProgressAsync(userId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get badge progress for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks if a user is eligible for a specific badge
    /// </summary>
    [HttpPost("check-eligibility")]
    public async Task<ActionResult<BadgeEligibilityResponse>> CheckBadgeEligibility(
        [FromBody] CheckBadgeEligibilityRequest request
    )
    {
        try
        {
            var result = await _badgeService.CheckBadgeEligibilityAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check badge eligibility for user {UserId}, badge {BadgeId}",
                request.UserId,
                request.BadgeId
            );
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Awards a badge to a user (admin only)
    /// </summary>
    [HttpPost("award")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AwardBadgeResponse>> AwardBadge(
        [FromBody] AwardBadgeRequest request
    )
    {
        try
        {
            var result = await _badgeService.AwardBadgeAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to award badge {BadgeId} to user {UserId}",
                request.BadgeId,
                request.UserId
            );
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new badge (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateBadgeResponse>> CreateBadge(
        [FromBody] CreateBadgeRequest request
    )
    {
        try
        {
            var result = await _badgeService.CreateBadgeAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetBadge), new { badgeId = result.BadgeId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create badge: {BadgeName}", request.Name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing badge (admin only)
    /// </summary>
    [HttpPut("{badgeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateBadge(int badgeId, [FromBody] UpdateBadgeRequest request)
    {
        try
        {
            request.Id = badgeId; // Ensure ID matches route parameter
            var success = await _badgeService.UpdateBadgeAsync(request);

            if (!success)
            {
                return NotFound($"Badge with ID {badgeId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update badge {BadgeId}", badgeId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a badge (admin only)
    /// </summary>
    [HttpDelete("{badgeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteBadge(int badgeId)
    {
        try
        {
            var success = await _badgeService.DeleteBadgeAsync(badgeId);

            if (!success)
            {
                return NotFound($"Badge with ID {badgeId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete badge {BadgeId}", badgeId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks and awards eligible badges for a user based on game events (internal use)
    /// </summary>
    [HttpPost("check-and-award/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AwardBadgeResponse>>> CheckAndAwardEligibleBadges(
        string userId,
        [FromBody] BadgeTriggerEvent triggerEvent
    )
    {
        try
        {
            var results = await _badgeService.CheckAndAwardEligibleBadgesAsync(
                userId,
                triggerEvent
            );
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check and award eligible badges for user {UserId}, event {Event}",
                userId,
                triggerEvent
            );
            return StatusCode(500, "Internal server error");
        }
    }
}
