using Microsoft.AspNetCore.SignalR;
using Gchain.Interfaces;
using Gchain.DTOS;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gchain.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IGameService _gameService;
        private readonly ILogger<GameHub> _logger;

        public GameHub(IGameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Join a game session group for real-time updates
        /// </summary>
        public async Task JoinGameSession(int gameSessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Check if user is in the game
                var gameSession = await _gameService.GetGameSessionAsync(gameSessionId);
                if (gameSession == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game session not found");
                    return;
                }

                var isInGame = gameSession.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId));
                if (!isInGame)
                {
                    await Clients.Caller.SendAsync("Error", "User not in game");
                    return;
                }

                // Join the game session group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"game_{gameSessionId}");
                
                // Notify other players that user joined
                await Clients.OthersInGroup($"game_{gameSessionId}").SendAsync("PlayerJoined", new
                {
                    UserId = userId,
                    GameSessionId = gameSessionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("User {UserId} joined game session {GameId} group", userId, gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join game session {GameId} group", gameSessionId);
                await Clients.Caller.SendAsync("Error", "Failed to join game session");
            }
        }

        /// <summary>
        /// Leave a game session group
        /// </summary>
        public async Task LeaveGameSession(int gameSessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Remove from group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game_{gameSessionId}");
                
                // Notify other players that user left
                await Clients.OthersInGroup($"game_{gameSessionId}").SendAsync("PlayerLeft", new
                {
                    UserId = userId,
                    GameSessionId = gameSessionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("User {UserId} left game session {GameId} group", userId, gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave game session {GameId} group", gameSessionId);
            }
        }

        /// <summary>
        /// Submit a word guess in real-time
        /// </summary>
        public async Task SubmitWordGuess(int gameSessionId, string word)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Submit the word guess
                var success = await _gameService.SubmitWordGuessAsync(gameSessionId, word, userId);
                
                if (success)
                {
                    // Broadcast the word guess to all players in the game
                    await Clients.Group($"game_{gameSessionId}").SendAsync("WordGuessSubmitted", new
                    {
                        UserId = userId,
                        Word = word,
                        GameSessionId = gameSessionId,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Word guess submitted: {Word} by user {UserId} in game {GameId}", word, userId, gameSessionId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to submit word guess");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit word guess for user {UserId} in game {GameId}", GetCurrentUserId(), gameSessionId);
                await Clients.Caller.SendAsync("Error", "Failed to submit word guess");
            }
        }

        /// <summary>
        /// Request team revival
        /// </summary>
        public async Task RequestTeamRevival(int gameSessionId, int teamId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Process team revival
                var success = await _gameService.ProcessTeamRevivalAsync(gameSessionId, teamId);
                
                // Broadcast revival result to all players
                await Clients.Group($"game_{gameSessionId}").SendAsync("TeamRevivalProcessed", new
                {
                    TeamId = teamId,
                    Success = success,
                    GameSessionId = gameSessionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Team {TeamId} revival {Result} in game {GameId}", teamId, success ? "succeeded" : "failed", gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process team revival for team {TeamId} in game {GameId}", teamId, gameSessionId);
                await Clients.Caller.SendAsync("Error", "Failed to process team revival");
            }
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        private string? GetCurrentUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Client disconnected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
