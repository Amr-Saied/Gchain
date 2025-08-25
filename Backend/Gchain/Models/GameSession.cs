namespace Gchain.Models;

public class GameSession
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CurrentWord { get; set; }
    public GameLanguage Language { get; set; } = GameLanguage.AR;
    public bool IsActive { get; set; } = true;
    public int TurnTimeLimitSeconds { get; set; } = 60;
    public int MaxLivesPerPlayer { get; set; } = 3;
    public int RoundsToWin { get; set; } = 2;
    public int CurrentRound { get; set; } = 1;
    public int? WinningTeamId { get; set; }

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<WordGuess> WordGuesses { get; set; } = new List<WordGuess>();
    public ICollection<RoundResult> RoundResults { get; set; } = new List<RoundResult>();
}

public enum GameLanguage
{
    AR, // Arabic
    EN // English
}
