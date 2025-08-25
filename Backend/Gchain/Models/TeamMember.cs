namespace Gchain.Models;

public class TeamMember
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int MistakesRemaining { get; set; }
    public bool IsActive { get; set; } = true;
    public int? JoinOrder { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
