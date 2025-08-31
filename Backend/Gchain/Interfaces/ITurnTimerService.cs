namespace Gchain.Interfaces;

/// <summary>
/// Interface for managing turn timers using Redis
/// </summary>
public interface ITurnTimerService
{
    /// <summary>
    /// Start a turn timer for a player
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <param name="userId">User ID of the current player</param>
    /// <param name="turnDurationSeconds">Turn duration in seconds</param>
    /// <returns>Turn deadline timestamp</returns>
    Task<DateTime?> StartTurnAsync(int gameSessionId, string userId, int turnDurationSeconds);

    /// <summary>
    /// Get remaining time for current turn
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>Remaining time, null if no active turn</returns>
    Task<TimeSpan?> GetRemainingTimeAsync(int gameSessionId);

    /// <summary>
    /// Get turn deadline
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>Turn deadline, null if no active turn</returns>
    Task<DateTime?> GetTurnDeadlineAsync(int gameSessionId);

    /// <summary>
    /// Check if turn has expired
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>True if turn has expired</returns>
    Task<bool> IsTurnExpiredAsync(int gameSessionId);

    /// <summary>
    /// End current turn (clear timer)
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>True if turn was ended successfully</returns>
    Task<bool> EndTurnAsync(int gameSessionId);

    /// <summary>
    /// Get current player for active turn
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <returns>User ID of current player, null if no active turn</returns>
    Task<string?> GetCurrentPlayerAsync(int gameSessionId);

    /// <summary>
    /// Extend current turn by additional seconds
    /// </summary>
    /// <param name="gameSessionId">Game session ID</param>
    /// <param name="additionalSeconds">Additional time to add</param>
    /// <returns>New deadline if successful</returns>
    Task<DateTime?> ExtendTurnAsync(int gameSessionId, int additionalSeconds);

    /// <summary>
    /// Get all active turn timers
    /// </summary>
    /// <returns>Dictionary of game session ID to deadline</returns>
    Task<Dictionary<int, DateTime>> GetActiveTimersAsync();

    /// <summary>
    /// Clean up expired timers
    /// </summary>
    /// <returns>Number of timers cleaned up</returns>
    Task<int> CleanupExpiredTimersAsync();
}
