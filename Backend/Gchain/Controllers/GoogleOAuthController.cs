using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers;

/// <summary>
/// Controller for Google OAuth authentication
/// </summary>
[ApiController]
[Route("api/auth/google")]
public class GoogleOAuthController : ControllerBase
{
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly ILogger<GoogleOAuthController> _logger;

    public GoogleOAuthController(
        IGoogleOAuthService googleOAuthService,
        ILogger<GoogleOAuthController> logger
    )
    {
        _googleOAuthService = googleOAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user using Google OAuth token
    /// </summary>
    /// <param name="request">Google OAuth request containing token and type</param>
    /// <returns>Authentication response with JWT tokens and user information</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid request or token</response>
    /// <response code="401">Authentication failed</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(GoogleOAuthResponse), 200)]
    [ProducesResponseType(typeof(GoogleOAuthErrorResponse), 400)]
    [ProducesResponseType(typeof(GoogleOAuthErrorResponse), 401)]
    [ProducesResponseType(typeof(GoogleOAuthErrorResponse), 500)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleOAuthRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(
                    new GoogleOAuthErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Token is required"
                    }
                );
            }

            if (string.IsNullOrEmpty(request.TokenType))
            {
                return BadRequest(
                    new GoogleOAuthErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Token type is required"
                    }
                );
            }

            var response = await _googleOAuthService.AuthenticateAsync(request);

            _logger.LogInformation(
                "Google OAuth login successful for user: {Email}",
                response.User.Email
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid Google OAuth request: {Message}", ex.Message);
            return BadRequest(
                new GoogleOAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = ex.Message
                }
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Google OAuth authentication failed: {Message}", ex.Message);
            return Unauthorized(
                new GoogleOAuthErrorResponse
                {
                    Error = "invalid_token",
                    ErrorDescription = ex.Message
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Google OAuth authentication");
            return StatusCode(
                500,
                new GoogleOAuthErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred during authentication"
                }
            );
        }
    }
}
