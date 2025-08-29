using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for caching game state in Redis
/// </summary>
public class GameStateCacheService : IGameStateCacheService
{
    private readonly IRedisService _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<GameStateCacheService> _logger;

    public GameStateCacheService(
        IRedisService redis,
        IOptions<RedisSettings> settings,
        ILogger<GameStateCacheService> logger
    )
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetGameStateKey(int gameSessionId) => $"game:{gameSessionId}:state";

    private string GetRoundGuessesKey(int gameSessionId, int roundNumber) =>
        $"game:{gameSessionId}:round:{roundNumber}:guesses";

    private string GetUsedWordsKey(int gameSessionId, int roundNumber) =>
        $"game:{gameSessionId}:round:{roundNumber}:words";

    private string GetActiveGamesKey() => "games:active";

    public async Task<bool> CacheGameStateAsync(int gameSessionId, GameStateCache gameState)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            var expiry = TimeSpan.FromSeconds(_settings.GameStateTtlSeconds);

            gameState.LastUpdated = DateTime.UtcNow;

            var success = await _redis.SetAsync(key, gameState, expiry);

            if (success)
            {
                // Add to active games set
                await _redis.SetAddAsync(GetActiveGamesKey(), gameSessionId.ToString());
                _logger.LogInformation(
                    "Cached game state for session {GameSessionId}",
                    gameSessionId
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cache game state for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<GameStateCache?> GetGameStateAsync(int gameSessionId)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            return await _redis.GetAsync<GameStateCache>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get game state for session {GameSessionId}",
                gameSessionId
            );
            return null;
        }
    }

    public async Task<bool> UpdateCurrentPlayerAsync(int gameSessionId, string userId)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            var success = await _redis.HashSetAsync(key, "currentPlayerId", userId);

            if (success)
            {
                await _redis.HashSetAsync(key, "lastUpdated", DateTime.UtcNow.ToString("O"));
                _logger.LogDebug(
                    "Updated current player for session {GameSessionId} to {UserId}",
                    gameSessionId,
                    userId
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update current player for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<bool> UpdateCurrentWordAsync(int gameSessionId, string word)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            var fields = new Dictionary<string, string>
            {
                ["currentWord"] = word,
                ["lastUpdated"] = DateTime.UtcNow.ToString("O")
            };

            await _redis.HashSetMultipleAsync(key, fields);
            _logger.LogDebug(
                "Updated current word for session {GameSessionId} to {Word}",
                gameSessionId,
                word
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update current word for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<bool> UpdateTeamLivesAsync(int gameSessionId, int teamId, int livesRemaining)
    {
        try
        {
            // Get current game state
            var gameState = await GetGameStateAsync(gameSessionId);
            if (gameState == null)
                return false;

            // Update team lives
            var team = gameState.Teams.FirstOrDefault(t => t.TeamId == teamId);
            if (team == null)
                return false;

            // Update members' lives (simplified - in real implementation you'd track individual member lives)
            foreach (var member in team.Members)
            {
                member.MistakesRemaining = livesRemaining;
                member.IsActive = livesRemaining > 0;
            }

            // Cache updated state
            return await CacheGameStateAsync(gameSessionId, gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update team lives for session {GameSessionId}, team {TeamId}",
                gameSessionId,
                teamId
            );
            return false;
        }
    }

    public async Task<bool> AddWordGuessAsync(
        int gameSessionId,
        int roundNumber,
        WordGuessCache guess
    )
    {
        try
        {
            var key = GetRoundGuessesKey(gameSessionId, roundNumber);
            var usedWordsKey = GetUsedWordsKey(gameSessionId, roundNumber);

            // Add guess to round guesses list
            var guessJson = System.Text.Json.JsonSerializer.Serialize(guess);
            var listKey = $"{key}:list";
            await _redis.SetAddAsync(listKey, guessJson);

            // Add word to used words set
            await _redis.SetAddAsync(usedWordsKey, guess.Word.ToLowerInvariant());

            // Set expiration
            var expiry = TimeSpan.FromSeconds(_settings.GameStateTtlSeconds);
            await _redis.ExpireAsync(listKey, expiry);
            await _redis.ExpireAsync(usedWordsKey, expiry);

            _logger.LogDebug(
                "Added word guess {Word} for session {GameSessionId}, round {RoundNumber}",
                guess.Word,
                gameSessionId,
                roundNumber
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add word guess for session {GameSessionId}, round {RoundNumber}",
                gameSessionId,
                roundNumber
            );
            return false;
        }
    }

    public async Task<List<WordGuessCache>> GetRoundGuessesAsync(int gameSessionId, int roundNumber)
    {
        try
        {
            var key = GetRoundGuessesKey(gameSessionId, roundNumber);
            var listKey = $"{key}:list";
            var guessesJson = await _redis.SetGetAllAsync(listKey);

            var guesses = new List<WordGuessCache>();
            foreach (var json in guessesJson)
            {
                try
                {
                    var guess = System.Text.Json.JsonSerializer.Deserialize<WordGuessCache>(json);
                    if (guess != null)
                        guesses.Add(guess);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize word guess JSON: {Json}", json);
                }
            }

            return guesses.OrderBy(g => g.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get round guesses for session {GameSessionId}, round {RoundNumber}",
                gameSessionId,
                roundNumber
            );
            return new List<WordGuessCache>();
        }
    }

    public async Task<bool> IsWordUsedInRoundAsync(int gameSessionId, int roundNumber, string word)
    {
        try
        {
            var key = GetUsedWordsKey(gameSessionId, roundNumber);
            return await _redis.SetContainsAsync(key, word.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check if word {Word} is used in session {GameSessionId}, round {RoundNumber}",
                word,
                gameSessionId,
                roundNumber
            );
            return false;
        }
    }

    public async Task<bool> UpdateGameRoundAsync(int gameSessionId, int roundNumber)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            var fields = new Dictionary<string, string>
            {
                ["currentRound"] = roundNumber.ToString(),
                ["lastUpdated"] = DateTime.UtcNow.ToString("O")
            };

            await _redis.HashSetMultipleAsync(key, fields);
            _logger.LogInformation(
                "Updated game round for session {GameSessionId} to {RoundNumber}",
                gameSessionId,
                roundNumber
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update game round for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<bool> MarkGameCompletedAsync(int gameSessionId, int winningTeamId)
    {
        try
        {
            var key = GetGameStateKey(gameSessionId);
            var fields = new Dictionary<string, string>
            {
                ["isActive"] = "false",
                ["winningTeamId"] = winningTeamId.ToString(),
                ["lastUpdated"] = DateTime.UtcNow.ToString("O")
            };

            await _redis.HashSetMultipleAsync(key, fields);

            // Remove from active games
            await _redis.SetRemoveAsync(GetActiveGamesKey(), gameSessionId.ToString());

            _logger.LogInformation(
                "Marked game session {GameSessionId} as completed with winner {WinningTeamId}",
                gameSessionId,
                winningTeamId
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark game completed for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<bool> DeleteGameStateAsync(int gameSessionId)
    {
        try
        {
            var gameStateKey = GetGameStateKey(gameSessionId);
            var activeGamesKey = GetActiveGamesKey();

            // Delete main game state
            var deleted = await _redis.DeleteAsync(gameStateKey);

            // Remove from active games set
            await _redis.SetRemoveAsync(activeGamesKey, gameSessionId.ToString());

            // Clean up round-specific data (example for rounds 1-3)
            for (int round = 1; round <= 3; round++)
            {
                await _redis.DeleteAsync(GetRoundGuessesKey(gameSessionId, round) + ":list");
                await _redis.DeleteAsync(GetUsedWordsKey(gameSessionId, round));
            }

            _logger.LogInformation("Deleted game state for session {GameSessionId}", gameSessionId);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete game state for session {GameSessionId}",
                gameSessionId
            );
            return false;
        }
    }

    public async Task<List<int>> GetActiveGameSessionsAsync()
    {
        try
        {
            var activeGamesKey = GetActiveGamesKey();
            var gameIds = await _redis.SetGetAllAsync(activeGamesKey);

            return gameIds.Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active game sessions");
            return new List<int>();
        }
    }
}
