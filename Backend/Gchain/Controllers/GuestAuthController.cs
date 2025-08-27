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

    public GuestAuthController(
        IGuestAuthService guestAuthService,
        ILogger<GuestAuthController> logger
    )
    {
        _guestAuthService = guestAuthService;
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
}
