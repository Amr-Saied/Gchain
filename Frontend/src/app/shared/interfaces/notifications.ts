export type NotificationType =
  | 'BadgeEarned'
  | 'GameInvite'
  | 'GameStarted'
  | 'GameEnded'
  | 'TeamInvite'
  | 'LeaderboardUpdate'
  | 'System'
  | 'Achievement'
  | 'FriendRequest'
  | 'General';

export type NotificationPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface NotificationResponse {
  id: number;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  data?: any; // Additional data specific to notification type
  isRead: boolean;
  priority: NotificationPriority;
  createdAt: string;
  readAt?: string | null;
  expiresAt?: string | null;
}

export interface NotificationListResponse {
  notifications: NotificationResponse[];
  totalCount: number;
  unreadCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  generatedAt: string;
}

export interface CreateNotificationRequest {
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  data?: any;
  priority?: NotificationPriority;
  expiresAt?: string | null;
}

export interface CreateNotificationResponse {
  success: boolean;
  message: string;
  notification: NotificationResponse;
}

export interface MarkNotificationReadRequest {
  notificationId: number;
}

export interface MarkNotificationReadResponse {
  success: boolean;
  message: string;
}

export interface MarkAllNotificationsReadResponse {
  success: boolean;
  message: string;
  markedCount: number;
}

export interface DeleteNotificationRequest {
  notificationId: number;
}

export interface DeleteNotificationResponse {
  success: boolean;
  message: string;
}

export interface NotificationStatsResponse {
  totalNotifications: number;
  unreadNotifications: number;
  notificationsByType: Record<NotificationType, number>;
  recentNotifications: NotificationResponse[];
  generatedAt: string;
}

// Specific notification data types
export interface BadgeEarnedNotificationData {
  badgeId: number;
  badgeName: string;
  badgeIconUrl?: string | null;
  badgeType: string;
}

export interface GameInviteNotificationData {
  gameSessionId: number;
  inviterUserId: string;
  inviterUserName: string;
  gameLanguage: string;
  maxLives: number;
  roundsToWin: number;
}

export interface GameStartedNotificationData {
  gameSessionId: number;
  gameLanguage: string;
  teamId: number;
  teamName: string;
  currentRound: number;
  roundsToWin: number;
}

export interface GameEndedNotificationData {
  gameSessionId: number;
  gameLanguage: string;
  teamId: number;
  teamName: string;
  finalScore: number;
  isWinner: boolean;
  roundsPlayed: number;
}

export interface TeamInviteNotificationData {
  teamId: number;
  teamName: string;
  inviterUserId: string;
  inviterUserName: string;
  gameSessionId: number;
  gameLanguage: string;
}

export interface LeaderboardUpdateNotificationData {
  leaderboardType: string;
  previousRank: number;
  currentRank: number;
  rankChange: number;
  language?: string | null;
}

export interface AchievementNotificationData {
  achievementId: string;
  achievementName: string;
  achievementDescription: string;
  pointsEarned: number;
  totalPoints: number;
}
