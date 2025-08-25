namespace Gchain.Models;

public class UserBadge
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
