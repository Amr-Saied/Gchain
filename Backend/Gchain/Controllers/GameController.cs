using System.Security.Claims;
using Gchain.DTOS;
using Gchain.Hubs;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Gchain.Controllers;

/// <summary>
/// Controller for game management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ILogger<GameController> _logger;
    private readonly IHubContext<GameHub> _gameHubContext;

    public GameController(
        IGameService gameService,
        ILogger<GameController> logger,
        IHubContext<GameHub> gameHubContext
    )
    {
        _gameService = gameService;
        _logger = logger;
        _gameHubContext = gameHubContext;
    }

    /// <summary>
    /// Create a new game session
    /// </summary>
    /// <param name="request">Game creation parameters</param>
    /// <returns>Created game session details</returns>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateGameResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data", details = ModelState });
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var response = await _gameService.CreateGameAsync(request, userId);

            _logger.LogInformation("Game created successfully by user {UserId}", userId);

            return CreatedAtAction(
                nameof(GetGameSession),
                new { gameSessionId = response.GameSessionId },
                response
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid game creation request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create game for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "An error occurred while creating the game" });
        }
    }

    /// <summary>
    /// Get available games for joining
    /// </summary>
    /// <returns>List of available games</returns>
    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AvailableGamesResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableGames()
    {
        try
        {
            var response = await _gameService.GetAvailableGamesAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available games");
            return StatusCode(
                500,
                new { error = "An error occurred while retrieving available games" }
            );
        }
    }

    /// <summary>
    /// Get game session details by ID
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>Game session details</returns>
    [HttpGet("{gameSessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GameSessionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGameSession(int gameSessionId)
    {
        try
        {
            var gameSession = await _gameService.GetGameSessionAsync(gameSessionId);

            if (gameSession == null)
            {
                return NotFound(new { error = "Game session not found" });
            }

            return Ok(gameSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game session {GameSessionId}", gameSessionId);
            return StatusCode(
                500,
                new { error = "An error occurred while retrieving the game session" }
            );
        }
    }

    /// <summary>
    /// Join a team in a game session
    /// </summary>
    /// <param name="request">Team join request</param>
    /// <returns>Join operation result</returns>
    [HttpPost("join-team")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JoinTeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> JoinTeam([FromBody] JoinTeamRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data", details = ModelState });
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var response = await _gameService.JoinTeamAsync(request, userId);

            if (response.Success)
            {
                _logger.LogInformation(
                    "User {UserId} joined team {TeamId} in game {GameSessionId}",
                    userId,
                    request.TeamId,
                    request.GameSessionId
                );
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join team for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "An error occurred while joining the team" });
        }
    }

    /// <summary>
    /// Leave a game session
    /// </summary>
    /// <param name="request">Leave game request</param>
    /// <returns>Leave operation result</returns>
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LeaveGameResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LeaveGame([FromBody] LeaveGameRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data", details = ModelState });
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var response = await _gameService.LeaveGameAsync(request, userId);

            if (response.Success)
            {
                _logger.LogInformation(
                    "User {UserId} left game {GameSessionId}",
                    userId,
                    request.GameSessionId
                );

                // Send SignalR notification to other players
                try
                {
                    await _gameHubContext
                        .Clients.Group($"game_{request.GameSessionId}")
                        .SendAsync(
                            "PlayerLeft",
                            new
                            {
                                UserId = userId,
                                GameSessionId = request.GameSessionId,
                                Reason = "Player left the game",
                                Timestamp = DateTime.UtcNow,
                                Type = "PlayerLeft"
                            }
                        );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send SignalR notification for player leaving game {GameSessionId}",
                        request.GameSessionId
                    );
                }

                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave game for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "An error occurred while leaving the game" });
        }
    }

    /// <summary>
    /// Start a game session
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>Start operation result</returns>
    [HttpPost("{gameSessionId}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartGame(int gameSessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var response = await _gameService.StartGameAsync(gameSessionId, userId);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Game {GameSessionId} started by user {UserId}",
                    gameSessionId,
                    userId
                );
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start game {GameSessionId}", gameSessionId);
            return StatusCode(500, new { error = "An error occurred while starting the game" });
        }
    }

    /// <summary>
    /// End a game session early
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>End operation result</returns>
    [HttpPost("{gameSessionId}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EndGame(int gameSessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var success = await _gameService.EndGameAsync(gameSessionId, userId);

            if (success)
            {
                _logger.LogInformation(
                    "Game {GameSessionId} ended by user {UserId}",
                    gameSessionId,
                    userId
                );
                return Ok(new { message = "Game ended successfully" });
            }
            else
            {
                return BadRequest(
                    new { error = "Cannot end game. Check if you are a participant." }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end game {GameSessionId}", gameSessionId);
            return StatusCode(500, new { error = "An error occurred while ending the game" });
        }
    }

    /// <summary>
    /// Get active games for the current user
    /// </summary>
    /// <returns>List of user's active games</returns>
    [HttpGet("my-games")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GameSessionResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyActiveGames()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var games = await _gameService.GetUserActiveGamesAsync(userId);
            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get active games for user {UserId}",
                GetCurrentUserId()
            );
            return StatusCode(
                500,
                new { error = "An error occurred while retrieving your active games" }
            );
        }
    }

    /// <summary>
    /// Check if user can join a specific team
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <param name="teamId">Team ID</param>
    /// <returns>Whether user can join the team</returns>
    [HttpGet("{gameSessionId}/team/{teamId}/can-join")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanJoinTeam(int gameSessionId, int teamId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var canJoin = await _gameService.CanUserJoinTeamAsync(gameSessionId, teamId, userId);
            return Ok(new { canJoin });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check if user {UserId} can join team {TeamId}",
                GetCurrentUserId(),
                teamId
            );
            return StatusCode(
                500,
                new { error = "An error occurred while checking team join eligibility" }
            );
        }
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    /// <returns>Current user ID or null</returns>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
