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
    public class GameFlowController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly ITurnTimerService _turnTimerService;
        private readonly ILogger<GameFlowController> _logger;

        public GameFlowController(
            IGameService gameService,
            ITurnTimerService turnTimerService,
            ILogger<GameFlowController> logger
        )
        {
            _gameService = gameService;
            _turnTimerService = turnTimerService;
            _logger = logger;
        }

        /// <summary>
        /// Submit a word guess for the current round
        /// </summary>
        [HttpPost("submit-guess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubmitWordGuess([FromBody] SubmitWordGuessRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Specific validations for clearer errors
                var currentPlayerId = await _turnTimerService.GetCurrentPlayerAsync(
                    request.GameSessionId
                );
                if (string.IsNullOrEmpty(currentPlayerId))
                {
                    return BadRequest(new { error = "No active turn" });
                }

                if (currentPlayerId != userId)
                {
                    return BadRequest(new { error = "Not your turn" });
                }

                var turnExpired = await _turnTimerService.IsTurnExpiredAsync(request.GameSessionId);
                if (turnExpired)
                {
                    return BadRequest(new { error = "Turn expired" });
                }

                // Ensure user is part of this game and active
                var gameState = await _gameService.GetCurrentGameStateAsync(request.GameSessionId);
                if (gameState == null || !gameState.IsActive)
                {
                    return BadRequest(new { error = "Game is not active" });
                }

                var isInGame = gameState.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId));
                if (!isInGame)
                {
                    return BadRequest(new { error = "You are not in this game" });
                }

                var isActiveMember = gameState
                    .Teams.SelectMany(t => t.TeamMembers)
                    .First(m => m.UserId == userId)
                    .IsActive;
                if (!isActiveMember)
                {
                    return BadRequest(new { error = "You are inactive in this game" });
                }

                var success = await _gameService.SubmitWordGuessAsync(
                    request.GameSessionId,
                    request.Word,
                    userId
                );

                if (success)
                {
                    return Ok(new { message = "Word guess submitted successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to submit word guess" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to submit word guess for user {UserId}",
                    GetCurrentUserId()
                );
                return StatusCode(
                    500,
                    new { error = "An error occurred while submitting word guess" }
                );
            }
        }

        /// <summary>
        /// Get current game state for a session
        /// </summary>
        [HttpGet("state/{gameSessionId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GameSessionResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentGameState(int gameSessionId)
        {
            try
            {
                var gameState = await _gameService.GetCurrentGameStateAsync(gameSessionId);

                if (gameState == null)
                {
                    return NotFound(new { error = "Game session not found" });
                }

                return Ok(gameState);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get current game state for session {GameSessionId}",
                    gameSessionId
                );
                return StatusCode(
                    500,
                    new { error = "An error occurred while retrieving game state" }
                );
            }
        }

        /// <summary>
        /// Process turn timeout and penalties
        /// </summary>
        [HttpPost("process-timeout/{gameSessionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessTurnTimeout(int gameSessionId)
        {
            try
            {
                var success = await _gameService.ProcessTurnTimeoutAsync(gameSessionId);

                if (success)
                {
                    return Ok(new { message = "Turn timeout processed successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to process turn timeout" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process turn timeout for game {GameSessionId}",
                    gameSessionId
                );
                return StatusCode(
                    500,
                    new { error = "An error occurred while processing turn timeout" }
                );
            }
        }

        /// <summary>
        /// Advance to the next round
        /// </summary>
        [HttpPost("advance-round/{gameSessionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdvanceToNextRound(int gameSessionId)
        {
            try
            {
                var success = await _gameService.AdvanceToNextRoundAsync(gameSessionId);

                if (success)
                {
                    return Ok(new { message = "Advanced to next round successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to advance to next round" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to advance to next round for game {GameSessionId}",
                    gameSessionId
                );
                return StatusCode(
                    500,
                    new { error = "An error occurred while advancing to next round" }
                );
            }
        }

        /// <summary>
        /// Process dice-based revival for a team
        /// </summary>
        [HttpPost("revival/{gameSessionId}/team/{teamId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessTeamRevival(int gameSessionId, int teamId)
        {
            try
            {
                var success = await _gameService.ProcessTeamRevivalAsync(gameSessionId, teamId);

                if (success)
                {
                    return Ok(new { message = "Team revival successful" });
                }
                else
                {
                    return Ok(new { message = "Team revival failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process team revival for team {TeamId} in game {GameSessionId}",
                    teamId,
                    gameSessionId
                );
                return StatusCode(
                    500,
                    new { error = "An error occurred while processing team revival" }
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

    public class SubmitWordGuessRequest
    {
        public int GameSessionId { get; set; }
        public string Word { get; set; } = string.Empty;
    }
}
