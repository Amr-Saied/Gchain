using Gchain.Models;

namespace Gchain.DTOS;

public class CreateNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public class GetNotificationsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool UnreadOnly { get; set; } = false;
    public NotificationType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class MarkNotificationReadRequest
{
    public int NotificationId { get; set; }
}

public class MarkAllNotificationsReadRequest
{
    public NotificationType? Type { get; set; } // Optional: mark only specific type as read
}

public class BulkNotificationRequest
{
    public List<string> UserIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
