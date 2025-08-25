namespace Gchain.Models;

public class RoundResult
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public int RoundNumber { get; set; }
    public int WinningTeamId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public GameSession GameSession { get; set; } = null!;
    public Team WinningTeam { get; set; } = null!;
}
