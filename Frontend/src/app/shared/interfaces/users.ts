export interface UserProfileResponse {
  id: string;
  userName: string;
  email: string;
  profilePictureUrl?: string | null;
  preferences?: string | null;
  createdAt: string;
  updatedAt: string;
  authProvider: AuthProvider;
  totalGamesPlayed: number;
  totalGamesWon: number;
  totalScore: number;
  badgesEarned: number;
  experiencePoints: number;
  winRate: number;
  currentRank: string;
  lastGamePlayed?: string | null;
  level?: number;
  bio?: string;
}

export interface UpdateUserProfileRequest {
  newUserName?: string | null;
  profilePicture?: File | null;
}

export interface UpdateUserProfileResponse {
  success: boolean;
  message: string;
  user: UserProfileResponse;
}

export interface UserStatsResponse {
  totalGamesPlayed: number;
  totalGamesWon: number;
  totalScore: number;
  badgesEarned: number;
  experiencePoints: number;
  winRate: number;
  currentRank: string;
  averageScorePerGame: number;
  bestScore: number;
  longestWinStreak: number;
  currentWinStreak: number;
  favoriteLanguage: string;
  totalPlayTime: number; // in minutes
  lastGamePlayed?: string | null;
  accountCreated: string;
  generatedAt: string;
}

export type AuthProvider = 'Google' | 'Guest';

export interface UserSessionResponse {
  id: number;
  userId: string;
  refreshToken: string;
  expiresAt: string;
  createdAt: string;
  lastUsedAt: string;
  isActive: boolean;
  userAgent?: string | null;
  ipAddress?: string | null;
}

export interface UserPreferences {
  language: string;
  theme: 'light' | 'dark' | 'auto';
  notifications: {
    email: boolean;
    push: boolean;
    gameInvites: boolean;
    badgeEarned: boolean;
    leaderboardUpdates: boolean;
  };
  privacy: {
    showProfile: boolean;
    showStats: boolean;
    showGames: boolean;
  };
  game: {
    defaultLanguage: string;
    defaultMaxLives: number;
    defaultTurnTimeLimit: number;
    defaultRoundsToWin: number;
  };
}
