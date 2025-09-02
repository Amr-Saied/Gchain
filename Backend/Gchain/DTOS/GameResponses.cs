namespace Gchain.DTOS
{
    public class CreateGameResponse
    {
        public int GameSessionId { get; set; }
        public string Message { get; set; } = "Game created successfully";
        public GameSessionDto Game { get; set; } = null!;
    }

    public class JoinTeamResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public GameSessionDto? Game { get; set; }
    }

    public class LeaveGameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StartGameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorReason { get; set; }
    }

    public class GameSessionResponse
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CurrentWord { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TurnTimeLimitSeconds { get; set; }
        public int MaxLivesPerPlayer { get; set; }
        public int RoundsToWin { get; set; }
        public int CurrentRound { get; set; }
        public int? WinningTeamId { get; set; }
        public List<TeamDto> Teams { get; set; } = new();
        public List<WordGuessDto> WordGuesses { get; set; } = new();
        public List<RoundResultDto> RoundResults { get; set; } = new();
    }

    public class AvailableGamesResponse
    {
        public List<GameSessionDto> Games { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class GameSessionDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Language { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TurnTimeLimitSeconds { get; set; }
        public int MaxLivesPerPlayer { get; set; }
        public int RoundsToWin { get; set; }
        public int CurrentRound { get; set; }
        public int? WinningTeamId { get; set; }
        public List<TeamDto> Teams { get; set; } = new();
    }

    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int RoundsWon { get; set; }
        public bool RevivalUsed { get; set; }
        public List<TeamMemberDto> TeamMembers { get; set; } = new();
    }

    public class TeamMemberDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int MistakesRemaining { get; set; }
        public bool IsActive { get; set; }
        public int? JoinOrder { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class WordGuessDto
    {
        public int Id { get; set; }
        public string Word { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public DateTime GuessedAt { get; set; }
        public bool IsCorrect { get; set; }
        public int RoundNumber { get; set; }
    }

    public class RoundResultDto
    {
        public int Id { get; set; }
        public int GameSessionId { get; set; }
        public int RoundNumber { get; set; }
        public int? WinningTeamId { get; set; }
        public DateTime CompletedAt { get; set; }
        public string? Notes { get; set; }
    }
}
