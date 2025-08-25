namespace Gchain.Models;

public class WordGuess
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int GameSessionId { get; set; }
    public string Word { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RoundNumber { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public GameSession GameSession { get; set; } = null!;
}
