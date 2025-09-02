namespace Gchain.DTOS;

public class LeaderboardResponse
{
    public List<LeaderboardEntry> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public LeaderboardType Type { get; set; }
    public string? Language { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int TotalGamesWon { get; set; }
    public int TotalScore { get; set; }
    public int BadgesEarned { get; set; }
    public int ExperiencePoints { get; set; }
    public double WinRate { get; set; }
    public string CurrentRank { get; set; } = string.Empty;
    public DateTime LastGamePlayed { get; set; }
    public DateTime AccountCreated { get; set; }
    public bool IsCurrentUser { get; set; } = false;
}

public class UserRankResponse
{
    public LeaderboardEntry UserEntry { get; set; } = null!;
    public List<LeaderboardEntry> NearbyEntries { get; set; } = new(); // 5 entries above and below
    public LeaderboardType Type { get; set; }
    public string? Language { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LeaderboardStatsResponse
{
    public int TotalPlayers { get; set; }
    public int ActivePlayersThisWeek { get; set; }
    public int ActivePlayersThisMonth { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int TotalGamesThisWeek { get; set; }
    public int TotalGamesThisMonth { get; set; }
    public double AverageWinRate { get; set; }
    public string MostPopularLanguage { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
