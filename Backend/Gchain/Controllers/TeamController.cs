using System.Security.Claims;
using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamController> _logger;

        public TeamController(
            IGameService gameService,
            ITeamService teamService,
            ILogger<TeamController> logger
        )
        {
            _gameService = gameService;
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Get team details by ID
        /// </summary>
        [HttpGet("{teamId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTeam(int teamId)
        {
            try
            {
                var team = await _teamService.GetTeamAsync(teamId);
                if (team == null)
                {
                    return NotFound(new { error = "Team not found" });
                }

                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get team {TeamId}", teamId);
                return StatusCode(
                    500,
                    new { error = "An error occurred while retrieving team details" }
                );
            }
        }

        /// <summary>
        /// Get team members for a specific team
        /// </summary>
        [HttpGet("{teamId}/members")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TeamMemberDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTeamMembers(int teamId)
        {
            try
            {
                var teamMembers = await _teamService.GetTeamMembersAsync(teamId);
                return Ok(teamMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get team members for team {TeamId}", teamId);
                return StatusCode(
                    500,
                    new { error = "An error occurred while retrieving team members" }
                );
            }
        }

        /// <summary>
        /// Check if user can join a specific team
        /// </summary>
        [HttpGet("{teamId}/can-join")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CanJoinTeam(int teamId, [FromQuery] int gameSessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var canJoin = await _gameService.CanUserJoinTeamAsync(
                    gameSessionId,
                    teamId,
                    userId
                );
                return Ok(new { canJoin });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if user can join team {TeamId}", teamId);
                return StatusCode(
                    500,
                    new { error = "An error occurred while checking team join eligibility" }
                );
            }
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
