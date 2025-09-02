using Gchain.DTOS;

namespace Gchain.Interfaces;

public interface ITeamService
{
    Task<TeamDto?> GetTeamAsync(int teamId);
    Task<List<TeamMemberDto>> GetTeamMembersAsync(int teamId);
    Task<bool> CanJoinTeamAsync(int teamId, string userId);
    Task<bool> IsTeamFullAsync(int teamId);
    Task<int> GetTeamMemberCountAsync(int teamId);
}
