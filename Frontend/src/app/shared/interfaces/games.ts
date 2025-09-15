export interface CreateGameRequest {
  name: string;
  language: string;
  maxLives: number;
  turnTimeLimit: number; // seconds
  roundsToWin: number;
}

export interface CreateGameResponse {
  gameSessionId: number;
  success: boolean;
  message: string;
  gameSession: GameSessionResponse;
}

export interface GameSessionResponse {
  id: number;
  name: string;
  language: string;
  maxLives: number;
  turnTimeLimit: number;
  roundsToWin: number;
  maxRounds: number;
  currentRound: number;
  status: GameStatus;
  createdAt: string;
  startedAt?: string | null;
  endedAt?: string | null;
  teams: TeamResponse[];
  currentPlayerId?: string | null;
  timeRemaining?: number | null;
  playersCount: number;
  maxPlayers: number;
}

export interface TeamResponse {
  id: number;
  name: string;
  color: string;
  score: number;
  lives: number;
  isActive: boolean;
  members: TeamMemberResponse[];
  gameSessionId: number;
}

export interface TeamMemberResponse {
  id: number;
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  isLeader: boolean;
  joinedAt: string;
  teamId: number;
}

export interface JoinTeamRequest {
  gameSessionId: number;
  teamId: number;
}

export interface JoinTeamResponse {
  success: boolean;
  message: string;
  team: TeamResponse;
}

export interface JoinGameRequest {
  gameSessionId: number;
  teamId: number;
}

export interface JoinGameResponse {
  success: boolean;
  message: string;
  team: TeamResponse;
}

export interface LeaveGameRequest {
  gameSessionId: number;
}

export interface LeaveGameResponse {
  success: boolean;
  message: string;
}

export interface AvailableGamesResponse {
  games: GameSessionResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface MyGamesResponse {
  activeGames: GameSessionResponse[];
  completedGames: GameSessionResponse[];
  totalActive: number;
  totalCompleted: number;
}

export interface CanJoinGameResponse {
  canJoin: boolean;
  reason?: string | null;
  availableTeams: TeamResponse[];
}

export interface SubmitWordGuessRequest {
  gameSessionId: number;
  word: string;
}

export interface SubmitWordGuessResponse {
  success: boolean;
  message: string;
  similarity: number;
  isCorrect: boolean;
  nextPlayerId?: string | null;
  timeRemaining?: number | null;
}

export interface WordGuessResponse {
  id: number;
  word: string;
  similarity: number;
  isCorrect: boolean;
  submittedAt: string;
  userId: string;
  userName: string;
  teamId: number;
  teamName: string;
  gameSessionId: number;
  roundNumber: number;
}

export interface RoundResultResponse {
  id: number;
  roundNumber: number;
  winningTeamId: number;
  winningTeamName: string;
  winningWord: string;
  similarity: number;
  completedAt: string;
  gameSessionId: number;
}

export type GameStatus = 'Waiting' | 'InProgress' | 'Completed' | 'Cancelled';

export interface GameUpdateEvent {
  gameSessionId: number;
  type:
    | 'GameStateChange'
    | 'PlayerJoined'
    | 'PlayerLeft'
    | 'WordGuessSubmitted'
    | 'RoundCompleted'
    | 'GameEnded';
  data: any;
  timestamp: string;
}

export interface PlayerJoinedEvent {
  gameSessionId: number;
  player: TeamMemberResponse;
  team: TeamResponse;
}

export interface PlayerLeftEvent {
  gameSessionId: number;
  userId: string;
  teamId: number;
}

export interface WordGuessSubmittedEvent {
  gameSessionId: number;
  wordGuess: WordGuessResponse;
  nextPlayerId?: string | null;
  timeRemaining?: number | null;
}

export interface TeamRevivalProcessedEvent {
  gameSessionId: number;
  teamId: number;
  success: boolean;
  message: string;
}
