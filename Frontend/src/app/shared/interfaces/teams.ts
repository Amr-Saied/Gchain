export interface TeamDetailResponse {
  id: number;
  name: string;
  color: string;
  score: number;
  lives: number;
  isActive: boolean;
  gameSessionId: number;
  gameSession: {
    id: number;
    language: string;
    status: string;
    currentRound: number;
    roundsToWin: number;
  };
  members: TeamMemberResponse[];
  chatMessages: TeamChatMessageResponse[];
  wordGuesses: WordGuessResponse[];
  wonRounds: RoundResultResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface TeamMemberResponse {
  id: number;
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  isLeader: boolean;
  joinedAt: string;
  teamId: number;
  totalScore: number;
  gamesPlayed: number;
  gamesWon: number;
  winRate: number;
}

export interface TeamChatMessageResponse {
  id: number;
  message: string;
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  teamId: number;
  createdAt: string;
  isFromCurrentUser: boolean;
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

export interface CreateTeamRequest {
  name: string;
  color: string;
  gameSessionId: number;
}

export interface CreateTeamResponse {
  success: boolean;
  message: string;
  team: TeamResponse;
}

export interface UpdateTeamRequest {
  id: number;
  name?: string;
  color?: string;
}

export interface UpdateTeamResponse {
  success: boolean;
  message: string;
  team: TeamResponse;
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
  createdAt: string;
  updatedAt: string;
}

export interface CanJoinTeamResponse {
  canJoin: boolean;
  reason?: string | null;
  availableSlots: number;
  maxMembers: number;
  currentMembers: number;
}

export interface TeamStatsResponse {
  teamId: number;
  teamName: string;
  totalGames: number;
  totalWins: number;
  winRate: number;
  averageScore: number;
  bestScore: number;
  totalPlayTime: number; // in minutes
  members: TeamMemberStatsResponse[];
  generatedAt: string;
}

export interface TeamMemberStatsResponse {
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  totalScore: number;
  gamesPlayed: number;
  gamesWon: number;
  winRate: number;
  averageScore: number;
  bestScore: number;
  joinedAt: string;
  isLeader: boolean;
}
