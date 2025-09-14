using Gchain.Data;
using Gchain.DTOS;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gchain.Services
{
    public class GameService : IGameService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameService> _logger;
        private readonly IWordCacheService _wordCacheService;
        private readonly ITurnTimerService _turnTimerService;
        private readonly IGameStateCacheService _gameStateCacheService;

        public GameService(
            ApplicationDbContext context,
            ILogger<GameService> logger,
            IWordCacheService wordCacheService,
            ITurnTimerService turnTimerService,
            IGameStateCacheService gameStateCacheService
        )
        {
            _context = context;
            _logger = logger;
            _wordCacheService = wordCacheService;
            _turnTimerService = turnTimerService;
            _gameStateCacheService = gameStateCacheService;
        }

        public async Task<CreateGameResponse> CreateGameAsync(
            CreateGameRequest request,
            string userId
        )
        {
            try
            {
                // Validate request
                if (request.MaxLives < 1 || request.MaxLives > 5)
                    throw new ArgumentException("Max lives must be between 1 and 5");

                if (request.TurnTimeLimit < 10 || request.TurnTimeLimit > 120)
                    throw new ArgumentException(
                        "Turn time limit must be between 10 and 120 seconds"
                    );

                // Create game session
                var gameSession = new GameSession
                {
                    CreatedAt = DateTime.UtcNow,
                    Language = request.Language == "English" ? GameLanguage.EN : GameLanguage.EN,
                    TurnTimeLimitSeconds = request.TurnTimeLimit,
                    MaxLivesPerPlayer = request.MaxLives,
                    RoundsToWin = request.RoundsToWin,
                    IsActive = true,
                    CurrentRound = 1
                };

                // Create default teams
                var team1 = new Team
                {
                    Name = "Team 1",
                    Color = "#FF6B6B",
                    GameSessionId = 0, // Will be set after save
                    RoundsWon = 0,
                    RevivalUsed = false
                };

                var team2 = new Team
                {
                    Name = "Team 2",
                    Color = "#4ECDC4",
                    GameSessionId = 0, // Will be set after save
                    RoundsWon = 0,
                    RevivalUsed = false
                };

                _context.GameSessions.Add(gameSession);
                await _context.SaveChangesAsync();

                // Set team IDs and add to context
                team1.GameSessionId = gameSession.Id;
                team2.GameSessionId = gameSession.Id;
                _context.Teams.AddRange(team1, team2);
                await _context.SaveChangesAsync();

                // Add creator to team 1
                var teamMember = new TeamMember
                {
                    UserId = userId,
                    TeamId = team1.Id,
                    MistakesRemaining = request.MaxLives,
                    IsActive = true,
                    JoinOrder = 1
                };

                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Game session {GameId} created by user {UserId}",
                    gameSession.Id,
                    userId
                );

                var response = new CreateGameResponse();
                response.GameSessionId = gameSession.Id;
                response.Message = "Game created successfully";
                response.Game = MapToGameSessionDto(gameSession);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create game for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AvailableGamesResponse> GetAvailableGamesAsync()
        {
            try
            {
                var availableGames = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .Where(gs => gs.IsActive && gs.Teams.Sum(t => t.TeamMembers.Count) < 6)
                    .OrderByDescending(gs => gs.CreatedAt)
                    .ToListAsync();

                var gameDtos = availableGames.Select(MapToGameSessionDto).ToList();

                return new AvailableGamesResponse { Games = gameDtos, TotalCount = gameDtos.Count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available games");
                throw;
            }
        }

        public async Task<GameSessionResponse?> GetGameSessionAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .ThenInclude(tm => tm.User)
                    .Include(gs => gs.WordGuesses)
                    .Include(gs => gs.RoundResults)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null)
                    return null;

                return MapToGameSessionResponse(gameSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get game session {GameSessionId}", gameSessionId);
                throw;
            }
        }

        public async Task<JoinTeamResponse> JoinTeamAsync(JoinTeamRequest request, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == request.GameSessionId);

                if (gameSession == null)
                {
                    return new JoinTeamResponse
                    {
                        Success = false,
                        Message = "Game session not found"
                    };
                }

                if (!gameSession.IsActive)
                {
                    return new JoinTeamResponse
                    {
                        Success = false,
                        Message = "Game is not accepting players"
                    };
                }

                if (gameSession.Language != GameLanguage.EN)
                {
                    return new JoinTeamResponse
                    {
                        Success = false,
                        Message = "Only English language is supported."
                    };
                }

                // Find the team user wants to join
                var targetTeam = gameSession.Teams.FirstOrDefault(t => t.Id == request.TeamId);
                if (targetTeam == null)
                {
                    return new JoinTeamResponse { Success = false, Message = "Invalid team" };
                }

                // Check if target team is full (max 5 members per team)
                if (targetTeam.TeamMembers.Count >= 5)
                {
                    return new JoinTeamResponse
                    {
                        Success = false,
                        Message = "Team is full (maximum 5 players)"
                    };
                }

                // Check if user is already in the game
                var existingTeamMember = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .FirstOrDefault(m => m.UserId == userId);

                if (existingTeamMember != null)
                {
                    // User is already in a team - handle team swapping
                    var currentTeam = gameSession.Teams.First(t =>
                        t.Id == existingTeamMember.TeamId
                    );

                    // Check if user is trying to join the same team
                    if (currentTeam.Id == targetTeam.Id)
                    {
                        return new JoinTeamResponse
                        {
                            Success = false,
                            Message = "You are already in this team"
                        };
                    }

                    // Remove user from current team
                    currentTeam.TeamMembers.Remove(existingTeamMember);
                    _context.TeamMembers.Remove(existingTeamMember);

                    _logger.LogInformation(
                        "User {UserId} left team {CurrentTeamId} to join team {TargetTeamId}",
                        userId,
                        currentTeam.Id,
                        targetTeam.Id
                    );
                }

                // Add user to target team
                var teamMember = new TeamMember
                {
                    UserId = userId,
                    TeamId = targetTeam.Id,
                    MistakesRemaining = gameSession.MaxLivesPerPlayer,
                    IsActive = true,
                    JoinOrder = targetTeam.TeamMembers.Count + 1
                };

                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();

                var actionMessage =
                    existingTeamMember != null
                        ? "Successfully switched teams"
                        : "Successfully joined team";

                _logger.LogInformation(
                    "User {UserId} joined team {TeamId} in game {GameId}",
                    userId,
                    targetTeam.Id,
                    gameSession.Id
                );

                return new JoinTeamResponse
                {
                    Success = true,
                    Message = actionMessage,
                    TeamId = targetTeam.Id,
                    Game = MapToGameSessionDto(gameSession)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join team for user {UserId}", userId);
                throw;
            }
        }

        public async Task<LeaveGameResponse> LeaveGameAsync(LeaveGameRequest request, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == request.GameSessionId);

                if (gameSession == null)
                {
                    return new LeaveGameResponse
                    {
                        Success = false,
                        Message = "Game session not found"
                    };
                }

                var teamMember = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .FirstOrDefault(m => m.UserId == userId);

                if (teamMember == null)
                {
                    return new LeaveGameResponse { Success = false, Message = "User not in game" };
                }

                var team = gameSession.Teams.First(t => t.Id == teamMember.TeamId);
                var isGameActive = await IsGameActiveAsync(request.GameSessionId);

                // Handle different scenarios based on game state
                if (isGameActive)
                {
                    // ACTIVE GAME: Handle player leaving during gameplay
                    await HandlePlayerLeavingActiveGameAsync(
                        request.GameSessionId,
                        userId,
                        teamMember,
                        team
                    );
                }
                else
                {
                    // WAITING GAME: Handle player leaving during lobby phase
                    await HandlePlayerLeavingWaitingGameAsync(
                        request.GameSessionId,
                        userId,
                        teamMember,
                        team
                    );
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "User {UserId} left game {GameId} (Active: {IsActive})",
                    userId,
                    gameSession.Id,
                    isGameActive
                );

                return new LeaveGameResponse { Success = true, Message = "Successfully left game" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave game for user {UserId}", userId);
                throw;
            }
        }

        private async Task HandlePlayerLeavingActiveGameAsync(
            int gameSessionId,
            string userId,
            TeamMember teamMember,
            Team team
        )
        {
            try
            {
                _logger.LogInformation(
                    "Handling player {UserId} leaving active game {GameId}",
                    userId,
                    gameSessionId
                );

                // 1. Clean up turn timer for the leaving player
                await CleanupPlayerTurnTimerAsync(gameSessionId, userId);

                // 2. Mark player as inactive instead of removing them
                teamMember.IsActive = false;
                teamMember.MistakesRemaining = 0;

                // 3. Check if game can continue
                var canContinue = await CanGameContinueAsync(gameSessionId);

                if (!canContinue)
                {
                    // 4. End game if team has no active players
                    await EndGameGracefullyAsync(
                        gameSessionId,
                        "Team has no active players remaining"
                    );
                }
                else
                {
                    // 5. Recalculate turn order with remaining active players
                    await RecalculateTurnOrderAsync(gameSessionId);

                    // 6. Continue game with remaining players
                    await NotifyPlayersGameUpdateAsync(
                        gameSessionId,
                        $"Player {userId} left the game. Game continues with remaining players."
                    );
                }

                // 7. Update game state cache - mark player as inactive
                var gameState = await _gameStateCacheService.GetGameStateAsync(gameSessionId);
                if (gameState != null)
                {
                    // Update the game state to reflect player leaving
                    await _gameStateCacheService.CacheGameStateAsync(gameSessionId, gameState);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to handle player leaving active game {GameId}",
                    gameSessionId
                );
                throw;
            }
        }

        private async Task HandlePlayerLeavingWaitingGameAsync(
            int gameSessionId,
            string userId,
            TeamMember teamMember,
            Team team
        )
        {
            try
            {
                _logger.LogInformation(
                    "Handling player {UserId} leaving waiting game {GameId}",
                    userId,
                    gameSessionId
                );

                // 1. Remove user from team completely (safe in lobby phase)
                team.TeamMembers.Remove(teamMember);
                _context.TeamMembers.Remove(teamMember);

                // 2. FIXED: Don't delete empty teams - preserve them for new players
                if (team.TeamMembers.Count == 0)
                {
                    _logger.LogInformation(
                        "Team {TeamId} is now empty but preserved for new players",
                        team.Id
                    );
                    // Team is kept in the game for new players to join
                }

                // 3. Check if game has any players left (teams are preserved even when empty)
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession != null)
                {
                    // Count total active players across all teams
                    var totalPlayers = gameSession.Teams.Sum(t => t.TeamMembers.Count);
                    if (totalPlayers == 0)
                    {
                        // No players left - delete the entire game session
                        _context.GameSessions.Remove(gameSession);
                        _logger.LogInformation(
                            "Game {GameId} deleted - no players left",
                            gameSessionId
                        );
                    }
                    else
                    {
                        // 4. Notify remaining players
                        await NotifyPlayersGameUpdateAsync(
                            gameSessionId,
                            $"Player {userId} left the lobby. Waiting for more players..."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to handle player leaving waiting game {GameId}",
                    gameSessionId
                );
                throw;
            }
        }

        public async Task<StartGameResponse> StartGameAsync(int gameSessionId, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null)
                {
                    return new StartGameResponse
                    {
                        Success = false,
                        Message = "Game session not found",
                        ErrorReason = "GameNotFound"
                    };
                }

                // Check if user is in the game
                if (!gameSession.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId)))
                {
                    return new StartGameResponse
                    {
                        Success = false,
                        Message = "User not in game",
                        ErrorReason = "UserNotInGame"
                    };
                }

                // Check if both teams have at least one member
                var team1 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 1");
                var team2 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 2");

                if (team1?.TeamMembers.Count == 0 || team2?.TeamMembers.Count == 0)
                {
                    var emptyTeams = new List<string>();
                    if (team1?.TeamMembers.Count == 0)
                        emptyTeams.Add("Team 1");
                    if (team2?.TeamMembers.Count == 0)
                        emptyTeams.Add("Team 2");

                    _logger.LogWarning(
                        "Cannot start game {GameId} - empty teams: {EmptyTeams}",
                        gameSessionId,
                        string.Join(", ", emptyTeams)
                    );
                    return new StartGameResponse
                    {
                        Success = false,
                        Message =
                            $"Cannot start game - empty teams: {string.Join(", ", emptyTeams)}",
                        ErrorReason = "EmptyTeams"
                    };
                }

                // Additional validation: Check if teams have reasonable player distribution
                var team1Count = team1?.TeamMembers.Count ?? 0;
                var team2Count = team2?.TeamMembers.Count ?? 0;
                var totalPlayers = team1Count + team2Count;

                if (totalPlayers < 2)
                {
                    _logger.LogWarning(
                        "Cannot start game {GameId} - not enough players (minimum 2 required, current: {TotalPlayers})",
                        gameSessionId,
                        totalPlayers
                    );
                    return new StartGameResponse
                    {
                        Success = false,
                        Message =
                            $"Not enough players to start game (minimum 2 required, current: {totalPlayers})",
                        ErrorReason = "NotEnoughPlayers"
                    };
                }

                // Get a random word for the first round
                var word = await _wordCacheService.GetRandomWordAsync(gameSession.Language);
                gameSession.CurrentWord = word;

                // Determine and set the initial current player, then start timer for that player
                var initialPlayer = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.TeamId)
                    .ThenBy(m => m.Id)
                    .FirstOrDefault();

                if (initialPlayer != null)
                {
                    await _gameStateCacheService.UpdateCurrentPlayerAsync(
                        gameSessionId,
                        initialPlayer.UserId
                    );

                    await _turnTimerService.StartTurnAsync(
                        gameSessionId,
                        initialPlayer.UserId,
                        gameSession.TurnTimeLimitSeconds
                    );
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Game {GameId} started by user {UserId}",
                    gameSessionId,
                    userId
                );
                return new StartGameResponse
                {
                    Success = true,
                    Message = "Game started successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start game {GameId}", gameSessionId);
                return new StartGameResponse
                {
                    Success = false,
                    Message = "An error occurred while starting the game",
                    ErrorReason = "InternalError"
                };
            }
        }

        public async Task<bool> EndGameAsync(int gameSessionId, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null)
                    return false;

                // Check if user is in the game
                if (!gameSession.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId)))
                    return false;

                gameSession.IsActive = false;
                gameSession.WinningTeamId = null; // No winner if ended early

                // Stop turn timer
                await _turnTimerService.StopTurnTimerAsync(gameSessionId);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Game {GameId} ended early by user {UserId}",
                    gameSessionId,
                    userId
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end game {GameId}", gameSessionId);
                return false;
            }
        }

        public async Task<List<GameSessionResponse>> GetUserActiveGamesAsync(string userId)
        {
            try
            {
                var userGames = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .Where(gs =>
                        gs.IsActive && gs.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId))
                    )
                    .OrderByDescending(gs => gs.CreatedAt)
                    .ToListAsync();

                return userGames.Select(MapToGameSessionResponse).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active games for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CanUserJoinTeamAsync(int gameSessionId, int teamId, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                // Check if user is already in the game
                if (gameSession.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId)))
                    return false;

                // Check if game is full
                var totalPlayers = gameSession.Teams.Sum(t => t.TeamMembers.Count);
                if (totalPlayers >= 6)
                    return false;

                // Check if team exists and has space
                var team = gameSession.Teams.FirstOrDefault(t => t.Id == teamId);
                if (team == null || team.TeamMembers.Count >= 3)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to check if user {UserId} can join team {TeamId}",
                    userId,
                    teamId
                );
                return false;
            }
        }

        public async Task<bool> SubmitWordGuessAsync(int gameSessionId, string word, string userId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                // Enforce turn ownership
                var currentPlayerId = await _turnTimerService.GetCurrentPlayerAsync(gameSessionId);
                if (string.IsNullOrEmpty(currentPlayerId) || currentPlayerId != userId)
                {
                    _logger.LogWarning(
                        "Submit guess rejected: not user's turn. User {UserId}, current {Current}",
                        userId,
                        currentPlayerId
                    );
                    return false;
                }

                // Enforce active timer (turn not expired)
                var isExpired = await _turnTimerService.IsTurnExpiredAsync(gameSessionId);
                if (isExpired)
                {
                    _logger.LogWarning(
                        "Submit guess rejected: turn expired for game {GameId}",
                        gameSessionId
                    );
                    return false;
                }

                // Find user's team
                var teamMember = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .FirstOrDefault(m => m.UserId == userId);

                if (teamMember == null)
                    return false;

                if (!teamMember.IsActive)
                {
                    _logger.LogWarning(
                        "Submit guess rejected: inactive player {UserId} in game {GameId}",
                        userId,
                        gameSessionId
                    );
                    return false;
                }

                // Check if word is valid (semantic similarity to current word)
                var (similarityScore, isValid) =
                    await _wordCacheService.ValidateWordAssociationAsync(
                        gameSession.CurrentWord!,
                        word,
                        gameSession.Language
                    );

                // Create word guess
                var wordGuess = new WordGuess
                {
                    Word = word,
                    UserId = userId,
                    TeamId = teamMember.TeamId,
                    GuessedAt = DateTime.UtcNow,
                    IsCorrect = isValid,
                    RoundNumber = gameSession.CurrentRound,
                    GameSessionId = gameSessionId
                };

                _context.WordGuesses.Add(wordGuess);

                // Update player lives if word is incorrect
                if (!isValid)
                {
                    teamMember.MistakesRemaining--;
                    if (teamMember.MistakesRemaining <= 0)
                    {
                        teamMember.IsActive = false;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Word guess submitted: {Word} by user {UserId} in game {GameId}",
                    word,
                    userId,
                    gameSessionId
                );

                // Advance to next active player and restart timer
                await AdvanceToNextPlayerAsync(gameSession, gameSessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to submit word guess for user {UserId} in game {GameId}",
                    userId,
                    gameSessionId
                );
                return false;
            }
        }

        private async Task AdvanceToNextPlayerAsync(GameSession gameSession, int gameSessionId)
        {
            try
            {
                // Determine current player and team
                var currentPlayerId = await _turnTimerService.GetCurrentPlayerAsync(gameSessionId);

                // Build team -> active members ordered by joinOrder
                var teamToMembers = gameSession.Teams.ToDictionary(
                    t => t,
                    t => t.TeamMembers.Where(m => m.IsActive).OrderBy(m => m.JoinOrder).ToList()
                );

                var anyActive = teamToMembers.Values.Any(list => list.Count > 0);
                if (!anyActive)
                {
                    await _turnTimerService.EndTurnAsync(gameSessionId);
                    return;
                }

                // Identify current team
                var currentTeam = gameSession.Teams.FirstOrDefault(t =>
                    t.TeamMembers.Any(m => m.UserId == currentPlayerId)
                );

                // Choose next team: alternate to the other team if it has active players; otherwise keep same team
                Team nextTeam;
                if (currentTeam != null)
                {
                    var otherTeam = gameSession.Teams.First(t => t.Id != currentTeam.Id);
                    var otherHasActive = teamToMembers[otherTeam].Count > 0;
                    nextTeam = otherHasActive ? otherTeam : currentTeam;
                }
                else
                {
                    // If no current team, pick Team 1 if it has players, else Team 2
                    nextTeam = gameSession
                        .Teams.OrderBy(t => t.Id)
                        .First(t => teamToMembers[t].Count > 0);
                }

                // Within chosen team, rotate to next active member by joinOrder
                var members = teamToMembers[nextTeam];
                TeamMember nextPlayer;
                if (currentTeam != null && currentTeam.Id == nextTeam.Id)
                {
                    // Rotate within same team
                    var idx = members.FindIndex(m => m.UserId == currentPlayerId);
                    var nextIdx = idx >= 0 ? (idx + 1) % members.Count : 0;
                    nextPlayer = members[nextIdx];
                }
                else
                {
                    // Switched team: start from the first active member by joinOrder
                    nextPlayer = members.First();
                }

                await _gameStateCacheService.UpdateCurrentPlayerAsync(
                    gameSessionId,
                    nextPlayer.UserId
                );

                // Restart timer for next player
                await _turnTimerService.StartTurnAsync(
                    gameSessionId,
                    nextPlayer.UserId,
                    gameSession.TurnTimeLimitSeconds
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to advance to next player for game {GameId}",
                    gameSessionId
                );
            }
        }

        public async Task<GameSessionResponse?> GetCurrentGameStateAsync(int gameSessionId)
        {
            return await GetGameSessionAsync(gameSessionId);
        }

        public async Task<bool> ProcessTurnTimeoutAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                // Find current player and apply timeout penalty
                var currentPlayerId = await _turnTimerService.GetCurrentPlayerAsync(gameSessionId);
                if (!string.IsNullOrEmpty(currentPlayerId))
                {
                    var member = gameSession
                        .Teams.SelectMany(t => t.TeamMembers)
                        .FirstOrDefault(m => m.UserId == currentPlayerId);
                    if (member != null && member.IsActive)
                    {
                        member.MistakesRemaining = Math.Max(0, member.MistakesRemaining - 1);
                        if (member.MistakesRemaining == 0)
                        {
                            member.IsActive = false;
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Advance to next player/team and restart timer
                await NextTurnAsync(gameSessionId);

                _logger.LogInformation(
                    "Turn timeout processed and advanced for game {GameId}",
                    gameSessionId
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process turn timeout for game {GameId}",
                    gameSessionId
                );
                return false;
            }
        }

        public async Task<bool> AdvanceToNextRoundAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                // Check if a team has won the current round
                var team1 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 1");
                var team2 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 2");

                if (team1 == null || team2 == null)
                    return false;

                // Determine round winner based on remaining active players
                var team1ActivePlayers = team1.TeamMembers.Count(m => m.IsActive);
                var team2ActivePlayers = team2.TeamMembers.Count(m => m.IsActive);

                int? roundWinnerId = null;
                if (team1ActivePlayers == 0)
                {
                    roundWinnerId = team2.Id;
                    team2.RoundsWon++;
                }
                else if (team2ActivePlayers == 0)
                {
                    roundWinnerId = team1.Id;
                    team1.RoundsWon++;
                }

                // Record round result
                if (roundWinnerId.HasValue)
                {
                    var roundResult = new RoundResult
                    {
                        GameSessionId = gameSessionId,
                        RoundNumber = gameSession.CurrentRound,
                        WinningTeamId = roundWinnerId.Value,
                        CompletedAt = DateTime.UtcNow,
                        Notes = $"Round {gameSession.CurrentRound} completed"
                    };

                    _context.RoundResults.Add(roundResult);
                }

                // Check if match is won
                if (
                    team1.RoundsWon >= gameSession.RoundsToWin
                    || team2.RoundsWon >= gameSession.RoundsToWin
                )
                {
                    gameSession.IsActive = false;
                    gameSession.WinningTeamId =
                        team1.RoundsWon >= gameSession.RoundsToWin ? team1.Id : team2.Id;
                }
                else
                {
                    // Advance to next round and reset player states
                    gameSession.CurrentRound++;
                    foreach (var team in gameSession.Teams)
                    {
                        foreach (var member in team.TeamMembers)
                        {
                            member.MistakesRemaining = gameSession.MaxLivesPerPlayer;
                            member.IsActive = true;
                        }
                    }

                    // Get new word for next round
                    var word = await _wordCacheService.GetRandomWordAsync(gameSession.Language);
                    gameSession.CurrentWord = word;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Advanced to round {Round} for game {GameId}",
                    gameSession.CurrentRound,
                    gameSessionId
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to advance to next round for game {GameId}",
                    gameSessionId
                );
                return false;
            }
        }

        public async Task<bool> NextTurnAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                await AdvanceToNextPlayerAsync(gameSession, gameSessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to advance next turn for game {GameId}",
                    gameSessionId
                );
                return false;
            }
        }

        // Helper methods for game state management
        private async Task<bool> IsGameActiveAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context.GameSessions.FirstOrDefaultAsync(gs =>
                    gs.Id == gameSessionId
                );

                return gameSession != null
                    && gameSession.IsActive
                    && !string.IsNullOrEmpty(gameSession.CurrentWord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if game {GameId} is active", gameSessionId);
                return false;
            }
        }

        private async Task<bool> CanGameContinueAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null)
                    return false;

                // Check if both teams have at least one active player
                var team1 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 1");
                var team2 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 2");

                var team1HasPlayers = team1?.TeamMembers.Any(m => m.IsActive) ?? false;
                var team2HasPlayers = team2?.TeamMembers.Any(m => m.IsActive) ?? false;

                return team1HasPlayers && team2HasPlayers;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to check if game {GameId} can continue",
                    gameSessionId
                );
                return false;
            }
        }

        private async Task CleanupPlayerTurnTimerAsync(int gameSessionId, string userId)
        {
            try
            {
                // Check if the leaving player is the current player
                var currentPlayer = await _turnTimerService.GetCurrentPlayerAsync(gameSessionId);
                if (currentPlayer == userId)
                {
                    // Stop the current turn timer
                    await _turnTimerService.EndTurnAsync(gameSessionId);
                    _logger.LogInformation(
                        "Stopped turn timer for leaving player {UserId} in game {GameId}",
                        userId,
                        gameSessionId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to cleanup turn timer for player {UserId} in game {GameId}",
                    userId,
                    gameSessionId
                );
            }
        }

        private Task NotifyPlayersGameUpdateAsync(
            int gameSessionId,
            string message,
            object? data = null
        )
        {
            try
            {
                _logger.LogInformation(
                    "Game update notification for game {GameId}: {Message}",
                    gameSessionId,
                    message
                );
                // SignalR notifications will be handled by the GameHub when called from controllers
                // This method provides a placeholder for future direct SignalR integration
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to notify players of game update for game {GameId}",
                    gameSessionId
                );
            }

            return Task.CompletedTask;
        }

        private async Task RecalculateTurnOrderAsync(int gameSessionId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null)
                    return;

                // Get all active players from both teams
                var activePlayers = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.TeamId) // Team 1 first, then Team 2
                    .ThenBy(m => m.Id) // Then by member ID for consistent ordering
                    .ToList();

                if (activePlayers.Any())
                {
                    // Update turn order in cache
                    var currentPlayer = activePlayers.First();
                    await _gameStateCacheService.UpdateCurrentPlayerAsync(
                        gameSessionId,
                        currentPlayer.UserId
                    );

                    _logger.LogInformation(
                        "Recalculated turn order for game {GameId}. Current player: {UserId}",
                        gameSessionId,
                        currentPlayer.UserId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to recalculate turn order for game {GameId}",
                    gameSessionId
                );
            }
        }

        private async Task EndGameGracefullyAsync(int gameSessionId, string reason)
        {
            try
            {
                var gameSession = await _context.GameSessions.FirstOrDefaultAsync(gs =>
                    gs.Id == gameSessionId
                );

                if (gameSession != null)
                {
                    gameSession.IsActive = false;
                    gameSession.WinningTeamId = null; // No winner if ended due to player leaving

                    // Stop turn timer
                    await _turnTimerService.StopTurnTimerAsync(gameSessionId);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Game {GameId} ended gracefully due to: {Reason}",
                        gameSessionId,
                        reason
                    );

                    // Notify players
                    await NotifyPlayersGameUpdateAsync(gameSessionId, $"Game ended: {reason}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end game {GameId} gracefully", gameSessionId);
            }
        }

        public async Task<bool> ProcessTeamRevivalAsync(int gameSessionId, int teamId)
        {
            try
            {
                var gameSession = await _context
                    .GameSessions.Include(gs => gs.Teams)
                    .ThenInclude(t => t.TeamMembers)
                    .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

                if (gameSession == null || !gameSession.IsActive)
                    return false;

                var team = gameSession.Teams.FirstOrDefault(t => t.Id == teamId);
                if (team == null || team.RevivalUsed)
                    return false;

                // Simulate dice roll (1-6, success on 4-6)
                var random = new Random();
                var diceRoll = random.Next(1, 7);
                var revivalSuccess = diceRoll >= 4;

                if (revivalSuccess)
                {
                    // Grant shared life to team
                    var activeMembers = team.TeamMembers.Where(m => m.IsActive).ToList();
                    if (activeMembers.Any())
                    {
                        var sharedLife = gameSession.MaxLivesPerPlayer / 2;
                        foreach (var member in activeMembers)
                        {
                            member.MistakesRemaining = Math.Min(
                                member.MistakesRemaining + sharedLife,
                                gameSession.MaxLivesPerPlayer
                            );
                        }
                    }

                    team.RevivalUsed = true;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Team {TeamId} revival successful in game {GameId}",
                        teamId,
                        gameSessionId
                    );
                    return true;
                }

                team.RevivalUsed = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Team {TeamId} revival failed in game {GameId}",
                    teamId,
                    gameSessionId
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process team revival for team {TeamId} in game {GameId}",
                    teamId,
                    gameSessionId
                );
                return false;
            }
        }

        private GameSessionDto MapToGameSessionDto(GameSession gameSession)
        {
            return new GameSessionDto
            {
                Id = gameSession.Id,
                CreatedAt = gameSession.CreatedAt,
                Language = gameSession.Language.ToString(),
                IsActive = gameSession.IsActive,
                TurnTimeLimitSeconds = gameSession.TurnTimeLimitSeconds,
                MaxLivesPerPlayer = gameSession.MaxLivesPerPlayer,
                RoundsToWin = gameSession.RoundsToWin,
                CurrentRound = gameSession.CurrentRound,
                WinningTeamId = gameSession.WinningTeamId,
                Teams = gameSession.Teams.Select(MapToTeamDto).ToList()
            };
        }

        private GameSessionResponse MapToGameSessionResponse(GameSession gameSession)
        {
            return new GameSessionResponse
            {
                Id = gameSession.Id,
                CreatedAt = gameSession.CreatedAt,
                CurrentWord = gameSession.CurrentWord,
                Language = gameSession.Language.ToString(),
                IsActive = gameSession.IsActive,
                TurnTimeLimitSeconds = gameSession.TurnTimeLimitSeconds,
                MaxLivesPerPlayer = gameSession.MaxLivesPerPlayer,
                RoundsToWin = gameSession.RoundsToWin,
                CurrentRound = gameSession.CurrentRound,
                WinningTeamId = gameSession.WinningTeamId,
                Teams = gameSession.Teams.Select(MapToTeamDto).ToList(),
                WordGuesses = gameSession.WordGuesses.Select(MapToWordGuessDto).ToList(),
                RoundResults = gameSession.RoundResults.Select(MapToRoundResultDto).ToList()
            };
        }

        private TeamDto MapToTeamDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Color = team.Color,
                RoundsWon = team.RoundsWon,
                RevivalUsed = team.RevivalUsed,
                TeamMembers = team.TeamMembers.Select(MapToTeamMemberDto).ToList()
            };
        }

        private TeamMemberDto MapToTeamMemberDto(TeamMember teamMember)
        {
            return new TeamMemberDto
            {
                Id = teamMember.Id,
                UserId = teamMember.UserId,
                MistakesRemaining = teamMember.MistakesRemaining,
                IsActive = teamMember.IsActive,
                JoinOrder = teamMember.JoinOrder,
                UserName = teamMember.User?.UserName ?? "Unknown"
            };
        }

        private WordGuessDto MapToWordGuessDto(WordGuess wordGuess)
        {
            return new WordGuessDto
            {
                Id = wordGuess.Id,
                Word = wordGuess.Word,
                UserId = wordGuess.UserId,
                TeamId = wordGuess.TeamId,
                GuessedAt = wordGuess.GuessedAt,
                IsCorrect = wordGuess.IsCorrect,
                RoundNumber = wordGuess.RoundNumber
            };
        }

        private RoundResultDto MapToRoundResultDto(RoundResult roundResult)
        {
            return new RoundResultDto
            {
                Id = roundResult.Id,
                GameSessionId = roundResult.GameSessionId,
                RoundNumber = roundResult.RoundNumber,
                WinningTeamId = roundResult.WinningTeamId,
                CompletedAt = roundResult.CompletedAt,
                Notes = roundResult.Notes
            };
        }
    }
}
