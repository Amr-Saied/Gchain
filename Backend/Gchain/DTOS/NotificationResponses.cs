using Gchain.Models;

namespace Gchain.DTOS;

public class NotificationResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class NotificationsListResponse
{
    public List<NotificationResponse> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationStatsResponse
{
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public Dictionary<NotificationType, int> NotificationsByType { get; set; } = new();
    public DateTime LastNotificationAt { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CreateNotificationResponse
{
    public int NotificationId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Delivered { get; set; } = false;
}

public class BulkNotificationResponse
{
    public int TotalSent { get; set; }
    public int SuccessfullySent { get; set; }
    public int FailedToSend { get; set; }
    public List<string> FailedUserIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
