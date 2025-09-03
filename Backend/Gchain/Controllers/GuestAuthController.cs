using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers;

/// <summary>
/// Controller for guest authentication
/// </summary>
[ApiController]
[Route("api/auth/guest")]
public class GuestAuthController : ControllerBase
{
    private readonly IGuestAuthService _guestAuthService;
    private readonly ILogger<GuestAuthController> _logger;

    private readonly IGameService _gameService;
    private readonly IUserService _userService;

    public GuestAuthController(
        IGuestAuthService guestAuthService,
        IGameService gameService,
        IUserService userService,
        ILogger<GuestAuthController> logger
    )
    {
        _guestAuthService = guestAuthService;
        _gameService = gameService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new guest user and returns authentication tokens
    /// </summary>
    /// <returns>Authentication response with JWT tokens and user information</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(GuestAuthResponse), 200)]
    [ProducesResponseType(typeof(GoogleOAuthErrorResponse), 500)]
    public async Task<IActionResult> GuestLogin()
    {
        try
        {
            _logger.LogInformation("Guest authentication request received");

            var response = await _guestAuthService.CreateGuestAsync(HttpContext);

            _logger.LogInformation(
                "Guest authentication successful for user: {Username}",
                response.User.Name
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Guest authentication failed: {Message}", ex.Message);
            return BadRequest(
                new GoogleOAuthErrorResponse
                {
                    Error = "user_creation_failed",
                    ErrorDescription = "Failed to create guest user. Please try again."
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during guest authentication");
            return StatusCode(
                500,
                new GoogleOAuthErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred during guest authentication"
                }
            );
        }
    }

    /// <summary>
    /// Returns information about guest authentication for client integration
    /// </summary>
    /// <returns>Guest authentication configuration</returns>
    /// <response code="200">Configuration retrieved successfully</response>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetGuestInfo()
    {
        var info = new
        {
            AuthType = "Guest",
            RequiresCredentials = false,
            RequiresInput = false,
            UsernameFormat = "Guest_<number>",
            Features = new[]
            {
                "No registration required",
                "No input required",
                "One-click sign in",
                "Immediate game access",
                "Temporary session",
                "Full game functionality"
            }
        };

        return Ok(info);
    }

    /// <summary>
    /// Refreshes access token using a valid refresh token
    /// </summary>
    /// <response code="200">Tokens refreshed successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "refresh_token_required" });
        }

        var session = await _userService.FindActiveSessionByRefreshTokenAsync(request.RefreshToken);
        if (session == null)
        {
            return BadRequest(new { error = "invalid_refresh_token" });
        }

        var jwt = HttpContext.RequestServices.GetService(typeof(IJwtService)) as IJwtService;
        if (jwt == null)
        {
            return StatusCode(500, new { error = "jwt_service_unavailable" });
        }

        // Issue new tokens
        var accessToken = jwt.GenerateAccessToken(session.User);
        var newRefreshToken = jwt.GenerateRefreshToken();

        // Rotate refresh token and extend session
        session.RefreshToken = newRefreshToken;
        session.ExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userService.UpdateUserSessionAsync(session);

        return Ok(
            new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        );
    }

    // TEMPORARY TESTING ENDPOINT: Creates a dummy guest user and joins them to game 6, team 1
    // COMMENTED OUT - May be needed for future testing
    // [HttpPost("test-dummy-user")]
    // [ProducesResponseType(typeof(object), 200)]
    // [ProducesResponseType(typeof(object), 500)]
    // public async Task<IActionResult> CreateDummyUserForTesting()
    // {
    //     try
    //     {
    //         _logger.LogInformation("Creating dummy guest user for testing");

    //         // Create a dummy guest user
    //         var username = $"TestGuest_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    //         var (user, success) = await _userService.CreateGuestUserAsync(username, null);

    //         if (!success)
    //         {
    //             return BadRequest(new { error = "Failed to create dummy guest user" });
    //         }

    //         // Join the user to game 6, team 1
    //         var joinRequest = new JoinTeamRequest { GameSessionId = 6, TeamId = 11 };

    //         var joinResponse = await _gameService.JoinTeamAsync(joinRequest, user.Id);

    //         if (!joinResponse.Success)
    //         {
    //             return BadRequest(
    //                 new
    //                 {
    //                     error = "Failed to join dummy user to game",
    //                     details = joinResponse.Message
    //                 }
    //             );
    //         }

    //         _logger.LogInformation(
    //             "Successfully created dummy guest user {Username} and joined to game 6, team 1",
    //             username
    //         );

    //         return Ok(
    //             new
    //             {
    //                 message = "Dummy guest user created and joined successfully",
    //                 username = username,
    //                 userId = user.Id,
    //                 gameSessionId = 6,
    //                 teamId = 1,
    //                 joinResponse = joinResponse
    //             }
    //         );
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to create dummy guest user for testing");
    //         return StatusCode(
    //             500,
    //             new { error = "Failed to create dummy user", details = ex.Message }
    //         );
    //     }
    // }
}
