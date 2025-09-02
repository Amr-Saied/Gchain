namespace Gchain.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public string? RelatedEntityId { get; set; } // Game ID, Badge ID, etc.
    public string? RelatedEntityType { get; set; } // "Game", "Badge", "Team", etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsDelivered { get; set; } = false; // For real-time delivery tracking
    public DateTime? ReadAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}

public enum NotificationType
{
    System,         // General system notifications
    GameInvite,     // Game invitation
    GameEvent,      // Game started, ended, turn changes, etc.
    Achievement,    // Badge earned, milestone reached
    TeamEvent,      // Player joined/left team, team chat
    Friend,         // Friend requests, friend activities
    Security        // Security alerts, login notifications
}
