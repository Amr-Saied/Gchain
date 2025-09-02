using Gchain.Data;
using Gchain.DTOS;
using Gchain.Hubs;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Services;

/// <summary>
/// Service for managing notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GameHub> _gameHub;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<GameHub> gameHub,
        IHubContext<ChatHub> chatHub,
        ILogger<NotificationService> logger
    )
    {
        _context = context;
        _gameHub = gameHub;
        _chatHub = chatHub;
        _logger = logger;
    }

    public async Task<CreateNotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request
    )
    {
        try
        {
            var notification = new Notification
            {
                UserId = request.UserId,
                Message = request.Message,
                Type = request.Type,
                RelatedEntityId = request.RelatedEntityId,
                RelatedEntityType = request.RelatedEntityType,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsDelivered = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification
            var delivered = await SendRealTimeNotificationAsync(request.UserId, notification);

            _logger.LogInformation(
                "Created notification {NotificationId} for user {UserId}: {Message}",
                notification.Id,
                request.UserId,
                request.Message
            );

            return new CreateNotificationResponse
            {
                NotificationId = notification.Id,
                Success = true,
                Message = "Notification created successfully",
                Delivered = delivered
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", request.UserId);
            return new CreateNotificationResponse
            {
                Success = false,
                Message = "Failed to create notification"
            };
        }
    }

    public async Task<BulkNotificationResponse> CreateBulkNotificationsAsync(
        BulkNotificationRequest request
    )
    {
        try
        {
            var notifications = new List<Notification>();
            var failedUserIds = new List<string>();

            foreach (var userId in request.UserIds)
            {
                try
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Message = request.Message,
                        Type = request.Type,
                        RelatedEntityId = request.RelatedEntityId,
                        RelatedEntityType = request.RelatedEntityType,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        IsDelivered = false
                    };

                    notifications.Add(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to create notification for user {UserId}",
                        userId
                    );
                    failedUserIds.Add(userId);
                }
            }

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Send real-time notifications
                foreach (var notification in notifications)
                {
                    await SendRealTimeNotificationAsync(notification.UserId, notification);
                }
            }

            _logger.LogInformation(
                "Created {Count} bulk notifications, {Failed} failed",
                notifications.Count,
                failedUserIds.Count
            );

            return new BulkNotificationResponse
            {
                TotalSent = request.UserIds.Count,
                SuccessfullySent = notifications.Count,
                FailedToSend = failedUserIds.Count,
                FailedUserIds = failedUserIds,
                Message = $"Successfully sent {notifications.Count} notifications"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk notifications");
            return new BulkNotificationResponse
            {
                TotalSent = request.UserIds.Count,
                SuccessfullySent = 0,
                FailedToSend = request.UserIds.Count,
                FailedUserIds = request.UserIds,
                Message = "Failed to create bulk notifications"
            };
        }
    }

    public async Task<NotificationsListResponse> GetUserNotificationsAsync(
        string userId,
        GetNotificationsRequest request
    )
    {
        try
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            // Apply filters
            if (request.UnreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            if (request.Type.HasValue)
            {
                query = query.Where(n => n.Type == request.Type.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync();
            var unreadCount = await _context
                .Notifications.Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    Type = n.Type,
                    RelatedEntityId = n.RelatedEntityId,
                    RelatedEntityType = n.RelatedEntityType,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    IsDelivered = n.IsDelivered,
                    ReadAt = n.ReadAt,
                    DeliveredAt = n.DeliveredAt
                })
                .ToListAsync();

            return new NotificationsListResponse
            {
                Notifications = notifications,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n =>
                n.Id == notificationId && n.UserId == userId
            );

            if (notification == null)
            {
                _logger.LogWarning(
                    "Notification {NotificationId} not found for user {UserId}",
                    notificationId,
                    userId
                );
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Marked notification {NotificationId} as read for user {UserId}",
                    notificationId,
                    userId
                );
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark notification {NotificationId} as read for user {UserId}",
                notificationId,
                userId
            );
            return false;
        }
    }

    public async Task<int> MarkAllNotificationsAsReadAsync(
        string userId,
        MarkAllNotificationsReadRequest? request = null
    )
    {
        try
        {
            var query = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead);

            if (request?.Type.HasValue == true)
            {
                query = query.Where(n => n.Type == request.Type.Value);
            }

            var notifications = await query.ToListAsync();
            var count = notifications.Count;

            if (count > 0)
            {
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Marked {Count} notifications as read for user {UserId}",
                    count,
                    userId
                );
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark all notifications as read for user {UserId}",
                userId
            );
            return 0;
        }
    }

    public async Task<NotificationStatsResponse> GetNotificationStatsAsync(string userId)
    {
        try
        {
            var totalNotifications = await _context
                .Notifications.Where(n => n.UserId == userId)
                .CountAsync();

            var unreadNotifications = await _context
                .Notifications.Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            var readNotifications = totalNotifications - unreadNotifications;

            var notificationsByType = await _context
                .Notifications.Where(n => n.UserId == userId)
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            var lastNotification = await _context
                .Notifications.Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            return new NotificationStatsResponse
            {
                TotalNotifications = totalNotifications,
                UnreadNotifications = unreadNotifications,
                ReadNotifications = readNotifications,
                NotificationsByType = notificationsByType,
                LastNotificationAt = lastNotification,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> DeleteOldNotificationsAsync(int daysOld = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldNotifications = await _context
                .Notifications.Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToListAsync();

            var count = oldNotifications.Count;

            if (count > 0)
            {
                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} old notifications", count);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old notifications");
            return 0;
        }
    }

    public async Task<bool> SendRealTimeNotificationAsync(string userId, Notification notification)
    {
        try
        {
            // Try to send via GameHub first (for game-related notifications)
            if (
                notification.Type == NotificationType.GameEvent
                || notification.Type == NotificationType.GameInvite
                || notification.Type == NotificationType.TeamEvent
            )
            {
                await _gameHub
                    .Clients.User(userId)
                    .SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            id = notification.Id,
                            message = notification.Message,
                            type = notification.Type.ToString(),
                            relatedEntityId = notification.RelatedEntityId,
                            relatedEntityType = notification.RelatedEntityType,
                            createdAt = notification.CreatedAt,
                            isRead = notification.IsRead
                        }
                    );
            }
            else
            {
                // Send via ChatHub for other notifications
                await _chatHub
                    .Clients.User(userId)
                    .SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            id = notification.Id,
                            message = notification.Message,
                            type = notification.Type.ToString(),
                            relatedEntityId = notification.RelatedEntityId,
                            relatedEntityType = notification.RelatedEntityType,
                            createdAt = notification.CreatedAt,
                            isRead = notification.IsRead
                        }
                    );
            }

            // Mark as delivered
            notification.IsDelivered = true;
            notification.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sent real-time notification {NotificationId} to user {UserId}",
                notification.Id,
                userId
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send real-time notification {NotificationId} to user {UserId}",
                notification.Id,
                userId
            );
            return false;
        }
    }

    public async Task CreateGameNotificationAsync(
        string userId,
        string message,
        NotificationType type,
        int gameSessionId
    )
    {
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Message = message,
            Type = type,
            RelatedEntityId = gameSessionId.ToString(),
            RelatedEntityType = "Game"
        };

        await CreateNotificationAsync(request);
    }

    public async Task CreateAchievementNotificationAsync(string userId, string message, int badgeId)
    {
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Message = message,
            Type = NotificationType.Achievement,
            RelatedEntityId = badgeId.ToString(),
            RelatedEntityType = "Badge"
        };

        await CreateNotificationAsync(request);
    }

    public async Task CreateTeamNotificationAsync(
        string userId,
        string message,
        NotificationType type,
        int teamId
    )
    {
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Message = message,
            Type = type,
            RelatedEntityId = teamId.ToString(),
            RelatedEntityType = "Team"
        };

        await CreateNotificationAsync(request);
    }
}
