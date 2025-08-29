using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Interface for game state caching operations
/// </summary>
public interface IGameStateCacheService
{
    /// <summary>
    /// Cache complete game state
    /// </summary>
    Task<bool> CacheGameStateAsync(int gameSessionId, GameStateCache gameState);

    /// <summary>
    /// Get cached game state
    /// </summary>
    Task<GameStateCache?> GetGameStateAsync(int gameSessionId);

    /// <summary>
    /// Update current player turn
    /// </summary>
    Task<bool> UpdateCurrentPlayerAsync(int gameSessionId, string userId);

    /// <summary>
    /// Update current word being played
    /// </summary>
    Task<bool> UpdateCurrentWordAsync(int gameSessionId, string word);

    /// <summary>
    /// Update team lives remaining
    /// </summary>
    Task<bool> UpdateTeamLivesAsync(int gameSessionId, int teamId, int livesRemaining);

    /// <summary>
    /// Add word guess to current round
    /// </summary>
    Task<bool> AddWordGuessAsync(int gameSessionId, int roundNumber, WordGuessCache guess);

    /// <summary>
    /// Get all word guesses for current round
    /// </summary>
    Task<List<WordGuessCache>> GetRoundGuessesAsync(int gameSessionId, int roundNumber);

    /// <summary>
    /// Check if word has been used in current round
    /// </summary>
    Task<bool> IsWordUsedInRoundAsync(int gameSessionId, int roundNumber, string word);

    /// <summary>
    /// Update game round
    /// </summary>
    Task<bool> UpdateGameRoundAsync(int gameSessionId, int roundNumber);

    /// <summary>
    /// Mark game as completed
    /// </summary>
    Task<bool> MarkGameCompletedAsync(int gameSessionId, int winningTeamId);

    /// <summary>
    /// Delete game state cache when game ends
    /// </summary>
    Task<bool> DeleteGameStateAsync(int gameSessionId);

    /// <summary>
    /// Get all active game sessions
    /// </summary>
    Task<List<int>> GetActiveGameSessionsAsync();
}
