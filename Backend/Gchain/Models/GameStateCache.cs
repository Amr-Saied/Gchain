namespace Gchain.Models;

/// <summary>
/// Cached game state for Redis storage
/// </summary>
public class GameStateCache
{
    public int GameSessionId { get; set; }
    public string? CurrentWord { get; set; }
    public GameLanguage Language { get; set; } = GameLanguage.EN;
    public bool IsActive { get; set; } = true;
    public int TurnTimeLimitSeconds { get; set; } = 60;
    public int MaxLivesPerPlayer { get; set; } = 3;
    public int RoundsToWin { get; set; } = 2;
    public int CurrentRound { get; set; } = 1;
    public int? WinningTeamId { get; set; }
    public string? CurrentPlayerId { get; set; }
    public DateTime? TurnStartTime { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Team information
    public List<TeamCache> Teams { get; set; } = new List<TeamCache>();

    // Current round guesses
    public List<WordGuessCache> CurrentRoundGuesses { get; set; } = new List<WordGuessCache>();
}

/// <summary>
/// Cached team information
/// </summary>
public class TeamCache
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoundsWon { get; set; } = 0;
    public bool RevivalUsed { get; set; } = false;
    public List<TeamMemberCache> Members { get; set; } = new List<TeamMemberCache>();
}

/// <summary>
/// Cached team member information
/// </summary>
public class TeamMemberCache
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int MistakesRemaining { get; set; }
    public bool IsActive { get; set; } = true;
    public int JoinOrder { get; set; }
}

/// <summary>
/// Cached word guess information
/// </summary>
public class WordGuessCache
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string Word { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public double? SimilarityScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RoundNumber { get; set; }
    public string? RejectionReason { get; set; }
}
