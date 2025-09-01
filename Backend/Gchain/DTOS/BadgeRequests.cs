using Gchain.Models;

namespace Gchain.DTOS;

public class CreateBadgeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty; // JSON string
    public string? IconUrl { get; set; }
    public BadgeType Type { get; set; } = BadgeType.Achievement;
    public int? RequiredValue { get; set; } // For numeric criteria
    public bool IsActive { get; set; } = true;
}

public class UpdateBadgeRequest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Criteria { get; set; }
    public string? IconUrl { get; set; }
    public BadgeType? Type { get; set; }
    public int? RequiredValue { get; set; }
    public bool? IsActive { get; set; }
}

public class GetUserBadgesRequest
{
    public string UserId { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public BadgeType? Type { get; set; }
    public bool? IsEarned { get; set; } // null = all, true = earned only, false = not earned
}

public class AwardBadgeRequest
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public string? Reason { get; set; } // Optional reason for awarding
}

public class CheckBadgeEligibilityRequest
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
}
