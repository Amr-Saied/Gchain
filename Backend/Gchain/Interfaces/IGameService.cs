using Gchain.DTOS;

namespace Gchain.Interfaces
{
    /// <summary>
    /// Interface for game management operations
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Create a new game session
        /// </summary>
        Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, string userId);

        /// <summary>
        /// Get available games for joining
        /// </summary>
        Task<AvailableGamesResponse> GetAvailableGamesAsync();

        /// <summary>
        /// Get game session details by ID
        /// </summary>
        Task<GameSessionResponse?> GetGameSessionAsync(int gameSessionId);

        /// <summary>
        /// Join a team in a game session
        /// </summary>
        Task<JoinTeamResponse> JoinTeamAsync(JoinTeamRequest request, string userId);

        /// <summary>
        /// Leave a game session
        /// </summary>
        Task<LeaveGameResponse> LeaveGameAsync(LeaveGameRequest request, string userId);

        /// <summary>
        /// Start a game session
        /// </summary>
        Task<bool> StartGameAsync(int gameSessionId, string userId);

        /// <summary>
        /// End a game session early
        /// </summary>
        Task<bool> EndGameAsync(int gameSessionId, string userId);

        /// <summary>
        /// Get active games for a specific user
        /// </summary>
        Task<List<GameSessionResponse>> GetUserActiveGamesAsync(string userId);

        /// <summary>
        /// Check if a user can join a specific team
        /// </summary>
        Task<bool> CanUserJoinTeamAsync(int gameSessionId, int teamId, string userId);

        /// <summary>
        /// Submit a word guess for the current round
        /// </summary>
        Task<bool> SubmitWordGuessAsync(int gameSessionId, string word, string userId);

        /// <summary>
        /// Get current game state for a session
        /// </summary>
        Task<GameSessionResponse?> GetCurrentGameStateAsync(int gameSessionId);

        /// <summary>
        /// Process turn timeout and penalties
        /// </summary>
        Task<bool> ProcessTurnTimeoutAsync(int gameSessionId);

        /// <summary>
        /// Advance to the next round
        /// </summary>
        Task<bool> AdvanceToNextRoundAsync(int gameSessionId);

        /// <summary>
        /// Process dice-based revival for a team
        /// </summary>
        Task<bool> ProcessTeamRevivalAsync(int gameSessionId, int teamId);
    }
}
