namespace Gchain.DTOS;

public class GetLeaderboardRequest
{
    public LeaderboardType Type { get; set; } = LeaderboardType.Overall;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Language { get; set; } // Filter by language (Arabic/English)
    public DateTime? FromDate { get; set; } // Filter by date range
    public DateTime? ToDate { get; set; }
}

public class GetUserRankRequest
{
    public string UserId { get; set; } = string.Empty;
    public LeaderboardType Type { get; set; } = LeaderboardType.Overall;
    public string? Language { get; set; }
}

public enum LeaderboardType
{
    Overall,        // Total games played, wins, score
    Weekly,         // Last 7 days
    Monthly,        // Last 30 days
    AllTime,        // Since account creation
    Language,       // Filtered by specific language
    Badges,         // Most badges earned
    WinRate,        // Win percentage
    Experience      // Experience points
}
