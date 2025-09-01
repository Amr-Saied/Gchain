using Gchain.DTOS;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gchain.Controllers;

/// <summary>
/// Controller for notification management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger
    )
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets notifications for the current user
    /// </summary>
    /// <param name="request">Notification request parameters</param>
    /// <returns>User's notifications</returns>
    /// <response code="200">Notifications retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationsListResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetNotifications([FromQuery] GetNotificationsRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 20;

            var notifications = await _notificationService.GetUserNotificationsAsync(currentUserId, request);

            _logger.LogInformation(
                "Retrieved {Count} notifications for user {UserId}",
                notifications.Notifications.Count, currentUserId
            );

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications");
            return StatusCode(500, new { error = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Gets notification statistics for the current user
    /// </summary>
    /// <returns>Notification statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(NotificationStatsResponse), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> GetNotificationStats()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var stats = await _notificationService.GetNotificationStatsAsync(currentUserId);

            _logger.LogInformation("Retrieved notification stats for user {UserId}", currentUserId);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats");
            return StatusCode(500, new { error = "Failed to retrieve notification statistics" });
        }
    }

    /// <summary>
    /// Marks a specific notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID to mark as read</param>
    /// <returns>Success status</returns>
    /// <response code="200">Notification marked as read successfully</response>
    /// <response code="400">Invalid notification ID</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Notification not found</response>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var success = await _notificationService.MarkNotificationAsReadAsync(notificationId, currentUserId);

            if (!success)
            {
                return NotFound(new { error = "Notification not found" });
            }

            _logger.LogInformation(
                "Marked notification {NotificationId} as read for user {UserId}",
                notificationId, currentUserId
            );

            return Ok(new { success = true, message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return StatusCode(500, new { error = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Marks all notifications as read for the current user
    /// </summary>
    /// <param name="request">Optional request to mark only specific type as read</param>
    /// <returns>Number of notifications marked as read</returns>
    /// <response code="200">Notifications marked as read successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> MarkAllNotificationsAsRead([FromBody] MarkAllNotificationsReadRequest? request = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var count = await _notificationService.MarkAllNotificationsAsReadAsync(currentUserId, request);

            _logger.LogInformation(
                "Marked {Count} notifications as read for user {UserId}",
                count, currentUserId
            );

            return Ok(new { 
                success = true, 
                message = $"Marked {count} notifications as read",
                count = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read");
            return StatusCode(500, new { error = "Failed to mark all notifications as read" });
        }
    }

    /// <summary>
    /// Creates a new notification (admin/system use)
    /// </summary>
    /// <param name="request">Notification creation request</param>
    /// <returns>Created notification details</returns>
    /// <response code="200">Notification created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateNotificationResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "UserId and Message are required" });
            }

            var response = await _notificationService.CreateNotificationAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            _logger.LogInformation(
                "Created notification {NotificationId} for user {UserId}",
                response.NotificationId, request.UserId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification");
            return StatusCode(500, new { error = "Failed to create notification" });
        }
    }

    /// <summary>
    /// Creates notifications for multiple users (admin/system use)
    /// </summary>
    /// <param name="request">Bulk notification request</param>
    /// <returns>Bulk notification results</returns>
    /// <response code="200">Bulk notifications processed successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkNotificationResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> CreateBulkNotifications([FromBody] BulkNotificationRequest request)
    {
        try
        {
            if (!request.UserIds.Any() || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "UserIds and Message are required" });
            }

            var response = await _notificationService.CreateBulkNotificationsAsync(request);

            _logger.LogInformation(
                "Created bulk notifications: {SuccessfullySent} sent, {Failed} failed",
                response.SuccessfullySent, response.FailedToSend
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk notifications");
            return StatusCode(500, new { error = "Failed to create bulk notifications" });
        }
    }

    /// <summary>
    /// Deletes old notifications (admin/system use)
    /// </summary>
    /// <param name="daysOld">Number of days old to delete (default: 30)</param>
    /// <returns>Number of notifications deleted</returns>
    /// <response code="200">Old notifications deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpDelete("cleanup")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<IActionResult> DeleteOldNotifications([FromQuery] int daysOld = 30)
    {
        try
        {
            if (daysOld < 1 || daysOld > 365)
            {
                return BadRequest(new { error = "DaysOld must be between 1 and 365" });
            }

            var count = await _notificationService.DeleteOldNotificationsAsync(daysOld);

            _logger.LogInformation("Deleted {Count} old notifications (older than {DaysOld} days)", count, daysOld);

            return Ok(new { 
                success = true, 
                message = $"Deleted {count} old notifications",
                count = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old notifications");
            return StatusCode(500, new { error = "Failed to delete old notifications" });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
