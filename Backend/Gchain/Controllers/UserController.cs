using System.Security.Claims;
using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gchain.Controllers;

/// <summary>
/// Controller for user profile management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Updates user profile (username and/or profile picture)
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Success status</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("profile")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Get current user
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User email not found" });
            }

            var user = await _userService.FindUserByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Update profile
            var success = await _userService.UpdateUserProfileAsync(
                user,
                request.NewUserName,
                request.ProfilePicture
            );

            if (success)
            {
                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
                return Ok(new { message = "Profile updated successfully" });
            }

            return BadRequest(new { error = "Failed to update profile" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets current user's profile information
    /// </summary>
    /// <returns>User profile data</returns>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var profile = await _userService.GetUserProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }
}
