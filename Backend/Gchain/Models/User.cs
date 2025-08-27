using Microsoft.AspNetCore.Identity;

namespace Gchain.Models;

public class User : IdentityUser
{
    public string? Preferences { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Google;

    // Navigation properties
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<WordGuess> WordGuesses { get; set; } = new List<WordGuess>();
    public ICollection<TeamChatMessage> ChatMessages { get; set; } = new List<TeamChatMessage>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
