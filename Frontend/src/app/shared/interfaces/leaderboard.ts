export type LeaderboardType =
  | 'Overall'
  | 'Weekly'
  | 'Monthly'
  | 'AllTime'
  | 'Language'
  | 'Badges'
  | 'WinRate'
  | 'Experience';

export interface GetLeaderboardRequest {
  type: LeaderboardType;
  page: number;
  pageSize: number;
  language?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  totalGamesPlayed: number;
  totalGamesWon: number;
  totalScore: number;
  badgesEarned: number;
  experiencePoints: number;
  winRate: number;
  level?: number;
  gamesPlayed?: number;
  score?: number;
  currentRank: string;
  lastGamePlayed: string;
  accountCreated: string;
  isCurrentUser: boolean;
}

export interface LeaderboardResponse {
  entries: LeaderboardEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  type: LeaderboardType;
  language?: string | null;
  generatedAt: string;
}

export interface GetUserRankRequest {
  userId: string;
  type: LeaderboardType;
  language?: string | null;
}

export interface UserRankResponse {
  userEntry: LeaderboardEntry;
  nearbyEntries: LeaderboardEntry[];
  type: LeaderboardType;
  language?: string | null;
  generatedAt: string;
  rank?: number;
  score?: number;
}

export interface LeaderboardStatsResponse {
  totalPlayers: number;
  activePlayersThisWeek: number;
  activePlayersThisMonth: number;
  totalGamesPlayed: number;
  totalGamesThisWeek: number;
  totalGamesThisMonth: number;
  averageWinRate: number;
  mostPopularLanguage: string;
  generatedAt: string;
}
