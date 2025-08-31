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

        public GameService(
            ApplicationDbContext context,
            ILogger<GameService> logger,
            IWordCacheService wordCacheService,
            ITurnTimerService turnTimerService
        )
        {
            _context = context;
            _logger = logger;
            _wordCacheService = wordCacheService;
            _turnTimerService = turnTimerService;
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
                    Language = request.Language == "English" ? GameLanguage.EN : GameLanguage.AR,
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

                // Check if user is already in the game
                if (gameSession.Teams.Any(t => t.TeamMembers.Any(m => m.UserId == userId)))
                {
                    return new JoinTeamResponse
                    {
                        Success = false,
                        Message = "User already in game"
                    };
                }

                // Check if game is full
                var totalPlayers = gameSession.Teams.Sum(t => t.TeamMembers.Count);
                if (totalPlayers >= 6)
                {
                    return new JoinTeamResponse { Success = false, Message = "Game is full" };
                }

                // Find the team
                var team = gameSession.Teams.FirstOrDefault(t => t.Id == request.TeamId);
                if (team == null)
                {
                    return new JoinTeamResponse { Success = false, Message = "Invalid team" };
                }

                if (team.TeamMembers.Count >= 3)
                {
                    return new JoinTeamResponse { Success = false, Message = "Team is full" };
                }

                // Add user to team
                var teamMember = new TeamMember
                {
                    UserId = userId,
                    TeamId = team.Id,
                    MistakesRemaining = gameSession.MaxLivesPerPlayer,
                    IsActive = true,
                    JoinOrder = team.TeamMembers.Count + 1
                };

                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "User {UserId} joined team {TeamId} in game {GameId}",
                    userId,
                    team.Id,
                    gameSession.Id
                );

                return new JoinTeamResponse
                {
                    Success = true,
                    Message = "Successfully joined team",
                    TeamId = team.Id,
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

                // Remove user from team
                var team = gameSession.Teams.First(t => t.Id == teamMember.TeamId);
                team.TeamMembers.Remove(teamMember);
                _context.TeamMembers.Remove(teamMember);

                // If team is empty, remove it
                if (team.TeamMembers.Count == 0)
                {
                    gameSession.Teams.Remove(team);
                    _context.Teams.Remove(team);
                }

                // If no teams left, delete the game
                if (gameSession.Teams.Count == 0)
                {
                    _context.GameSessions.Remove(gameSession);
                    _logger.LogInformation(
                        "Game {GameId} deleted - no players left",
                        gameSession.Id
                    );
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} left game {GameId}", userId, gameSession.Id);

                return new LeaveGameResponse { Success = true, Message = "Successfully left game" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave game for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> StartGameAsync(int gameSessionId, string userId)
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

                // Check if both teams have at least one member
                var team1 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 1");
                var team2 = gameSession.Teams.FirstOrDefault(t => t.Name == "Team 2");

                if (team1?.TeamMembers.Count == 0 || team2?.TeamMembers.Count == 0)
                {
                    _logger.LogWarning(
                        "Cannot start game {GameId} - teams are empty",
                        gameSessionId
                    );
                    return false;
                }

                // Get a random word for the first round
                var word = await _wordCacheService.GetRandomWordAsync(gameSession.Language);
                gameSession.CurrentWord = word;

                // Start turn timer
                await _turnTimerService.StartTurnTimerAsync(
                    gameSessionId,
                    gameSession.TurnTimeLimitSeconds
                );

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Game {GameId} started by user {UserId}",
                    gameSessionId,
                    userId
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start game {GameId}", gameSessionId);
                return false;
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

                // Find user's team
                var teamMember = gameSession
                    .Teams.SelectMany(t => t.TeamMembers)
                    .FirstOrDefault(m => m.UserId == userId);

                if (teamMember == null)
                    return false;

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
                // This would typically be handled by the turn timer service
                // For now, we'll just log the timeout
                _logger.LogInformation("Turn timeout processed for game {GameId}", gameSessionId);

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
