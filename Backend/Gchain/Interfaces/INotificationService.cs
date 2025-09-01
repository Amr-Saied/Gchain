using Gchain.DTOS;
using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Service for managing notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification for a user
    /// </summary>
    Task<CreateNotificationResponse> CreateNotificationAsync(CreateNotificationRequest request);

    /// <summary>
    /// Creates notifications for multiple users
    /// </summary>
    Task<BulkNotificationResponse> CreateBulkNotificationsAsync(BulkNotificationRequest request);

    /// <summary>
    /// Gets notifications for a user with pagination and filtering
    /// </summary>
    Task<NotificationsListResponse> GetUserNotificationsAsync(string userId, GetNotificationsRequest request);

    /// <summary>
    /// Marks a specific notification as read
    /// </summary>
    Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task<int> MarkAllNotificationsAsReadAsync(string userId, MarkAllNotificationsReadRequest? request = null);

    /// <summary>
    /// Gets notification statistics for a user
    /// </summary>
    Task<NotificationStatsResponse> GetNotificationStatsAsync(string userId);

    /// <summary>
    /// Deletes old notifications (cleanup)
    /// </summary>
    Task<int> DeleteOldNotificationsAsync(int daysOld = 30);

    /// <summary>
    /// Sends real-time notification via SignalR
    /// </summary>
    Task<bool> SendRealTimeNotificationAsync(string userId, Notification notification);

    /// <summary>
    /// Creates game-related notifications
    /// </summary>
    Task CreateGameNotificationAsync(string userId, string message, NotificationType type, int gameSessionId);

    /// <summary>
    /// Creates achievement notifications
    /// </summary>
    Task CreateAchievementNotificationAsync(string userId, string message, int badgeId);

    /// <summary>
    /// Creates team-related notifications
    /// </summary>
    Task CreateTeamNotificationAsync(string userId, string message, NotificationType type, int teamId);
}
