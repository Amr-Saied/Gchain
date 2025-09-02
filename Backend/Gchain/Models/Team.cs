namespace Gchain.Models;

public class Team
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public int RoundsWon { get; set; } = 0;
    public bool RevivalUsed { get; set; } = false;

    // Navigation properties
    public GameSession GameSession { get; set; } = null!;
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<TeamChatMessage> ChatMessages { get; set; } = new List<TeamChatMessage>();
    public ICollection<RoundResult> WonRounds { get; set; } = new List<RoundResult>();
    public ICollection<WordGuess> WordGuesses { get; set; } = new List<WordGuess>();
}
