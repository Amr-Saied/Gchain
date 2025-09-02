using Gchain.Models;

namespace Gchain.DTOS;

public class BadgeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public BadgeType Type { get; set; }
    public int? RequiredValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserBadgeResponse
{
    public int BadgeId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeDescription { get; set; } = string.Empty;
    public string? BadgeIconUrl { get; set; }
    public BadgeType BadgeType { get; set; }
    public DateTime EarnedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsRecentlyEarned { get; set; } = false; // Within last 24 hours
}

public class UserBadgesListResponse
{
    public List<UserBadgeResponse> EarnedBadges { get; set; } = new();
    public List<BadgeResponse> AvailableBadges { get; set; } = new();
    public int TotalEarned { get; set; }
    public int TotalAvailable { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class BadgeStatsResponse
{
    public int TotalBadges { get; set; }
    public int EarnedBadges { get; set; }
    public int AvailableBadges { get; set; }
    public Dictionary<BadgeType, int> BadgesByType { get; set; } = new();
    public List<UserBadgeResponse> RecentlyEarned { get; set; } = new(); // Last 7 days
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CreateBadgeResponse
{
    public int BadgeId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AwardBadgeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsNewBadge { get; set; } = false;
    public UserBadgeResponse? Badge { get; set; }
}

public class BadgeEligibilityResponse
{
    public bool IsEligible { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CurrentProgress { get; set; }
    public int RequiredProgress { get; set; }
    public double ProgressPercentage { get; set; }
}

public class BadgeProgressResponse
{
    public int BadgeId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeDescription { get; set; } = string.Empty;
    public BadgeType BadgeType { get; set; }
    public int CurrentProgress { get; set; }
    public int RequiredProgress { get; set; }
    public double ProgressPercentage { get; set; }
    public bool IsEarned { get; set; }
    public DateTime? EarnedAt { get; set; }
}
