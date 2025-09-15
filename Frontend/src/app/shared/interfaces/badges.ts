export type BadgeType = 'Achievement' | 'Progress' | 'Milestone' | 'Other';

export interface BadgeResponse {
  id: number;
  name: string;
  description: string;
  criteria: string;
  iconUrl?: string | null;
  type: BadgeType;
  requiredValue?: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UserBadgeResponse {
  badgeId: number;
  badgeName: string;
  badgeDescription: string;
  badgeIconUrl?: string | null;
  badgeType: BadgeType;
  earnedAt: string;
  reason?: string | null;
  isRecentlyEarned: boolean;
}

export interface UserBadgesListResponse {
  earnedBadges: UserBadgeResponse[];
  availableBadges: BadgeResponse[];
  totalEarned: number;
  totalAvailable: number;
  page: number;
  pageSize: number;
  totalPages: number;
  generatedAt: string;
}

export interface BadgeStatsResponse {
  totalBadges: number;
  earnedBadges: number;
  availableBadges: number;
  badgesByType: Record<BadgeType, number>;
  recentlyEarned: UserBadgeResponse[];
  generatedAt: string;
}

export interface CreateBadgeRequest {
  name: string;
  description: string;
  criteria: string;
  iconUrl?: string | null;
  type: BadgeType;
  requiredValue?: number | null;
  isActive: boolean;
}

export interface UpdateBadgeRequest {
  id: number;
  name?: string;
  description?: string;
  criteria?: string;
  iconUrl?: string | null;
  type?: BadgeType;
  requiredValue?: number | null;
  isActive?: boolean;
}

export interface CreateBadgeResponse {
  badgeId: number;
  success: boolean;
  message: string;
}

export interface AwardBadgeRequest {
  userId: string;
  badgeId: number;
  reason?: string | null;
}

export interface AwardBadgeResponse {
  success: boolean;
  message: string;
  isNewBadge: boolean;
  badge?: UserBadgeResponse | null;
}

export interface BadgeEligibilityResponse {
  isEligible: boolean;
  message: string;
  currentProgress: number;
  requiredProgress: number;
  progressPercentage: number;
}

export interface BadgeProgressResponse {
  badgeId: number;
  badgeName: string;
  badgeDescription: string;
  badgeType: BadgeType;
  currentProgress: number;
  requiredProgress: number;
  progressPercentage: number;
  isEarned: boolean;
  earnedAt?: string | null;
}
