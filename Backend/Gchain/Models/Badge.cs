namespace Gchain.Models;

public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty; // JSON string
    public string? IconUrl { get; set; }
    public BadgeType Type { get; set; } = BadgeType.Achievement;
    public int? RequiredValue { get; set; } // For numeric criteria
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}

public enum BadgeType
{
    Achievement, // General achievements
    Milestone, // Game milestones (first win, 10 games, etc.)
    Skill, // Skill-based (perfect game, streak, etc.)
    Social, // Social achievements (team player, etc.)
    Special, // Special events, seasonal, etc.
    Collection // Collection-based badges
}
