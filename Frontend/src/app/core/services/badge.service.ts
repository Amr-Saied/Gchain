import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import {
  BadgeResponse,
  UserBadgesListResponse,
  BadgeStatsResponse,
  CreateBadgeRequest,
  CreateBadgeResponse,
  UpdateBadgeRequest,
  AwardBadgeRequest,
  AwardBadgeResponse,
  BadgeEligibilityResponse,
  BadgeProgressResponse,
  BadgeType,
} from '../../shared/interfaces/badges';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BadgeService {
  private readonly api = inject(ApiService);

  /**
   * Get all badges
   */
  getAllBadges(activeOnly: boolean = true): Observable<BadgeResponse[]> {
    return this.api.get<BadgeResponse[]>(`badge?activeOnly=${activeOnly}`);
  }

  /**
   * Get badge by ID
   */
  getBadge(badgeId: number): Observable<BadgeResponse> {
    return this.api.get<BadgeResponse>(`badge/${badgeId}`);
  }

  /**
   * Get badges by type
   */
  getBadgesByType(type: BadgeType): Observable<BadgeResponse[]> {
    return this.api.get<BadgeResponse[]>(`badge/type/${type}`);
  }

  /**
   * Get user badges
   */
  getUserBadges(
    userId: string,
    page: number = 1,
    pageSize: number = 20,
    type?: BadgeType,
    isEarned?: boolean
  ): Observable<UserBadgesListResponse> {
    const params = new URLSearchParams({
      userId,
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (type) params.append('type', type);
    if (isEarned !== undefined) params.append('isEarned', isEarned.toString());

    return this.api.get<UserBadgesListResponse>(
      `badge/user?${params.toString()}`
    );
  }

  /**
   * Get current user badges
   */
  getMyBadges(
    page: number = 1,
    pageSize: number = 20,
    type?: BadgeType,
    isEarned?: boolean
  ): Observable<UserBadgesListResponse> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (type) params.append('type', type);
    if (isEarned !== undefined) params.append('isEarned', isEarned.toString());

    return this.api.get<UserBadgesListResponse>(
      `badge/my?${params.toString()}`
    );
  }

  /**
   * Get badge statistics
   */
  getBadgeStats(): Observable<BadgeStatsResponse> {
    return this.api.get<BadgeStatsResponse>('badge/stats');
  }

  /**
   * Get user badge statistics
   */
  getUserBadgeStats(userId: string): Observable<BadgeStatsResponse> {
    return this.api.get<BadgeStatsResponse>(`badge/stats/${userId}`);
  }

  /**
   * Get my badge statistics
   */
  getMyBadgeStats(): Observable<BadgeStatsResponse> {
    return this.api.get<BadgeStatsResponse>('badge/my/stats');
  }

  /**
   * Check badge eligibility
   */
  checkBadgeEligibility(
    userId: string,
    badgeId: number
  ): Observable<BadgeEligibilityResponse> {
    return this.api.post<BadgeEligibilityResponse>('badge/check-eligibility', {
      userId,
      badgeId,
    });
  }

  /**
   * Check my badge eligibility
   */
  checkMyBadgeEligibility(
    badgeId: number
  ): Observable<BadgeEligibilityResponse> {
    return this.api.post<BadgeEligibilityResponse>(
      'badge/my/check-eligibility',
      {
        badgeId,
      }
    );
  }

  /**
   * Get badge progress
   */
  getBadgeProgress(badgeId: number): Observable<BadgeProgressResponse> {
    return this.api.get<BadgeProgressResponse>(`badge/progress/${badgeId}`);
  }

  /**
   * Get my badge progress
   */
  getMyBadgeProgress(badgeId: number): Observable<BadgeProgressResponse> {
    return this.api.get<BadgeProgressResponse>(`badge/my/progress/${badgeId}`);
  }

  // Admin methods (require Admin role)

  /**
   * Create badge (Admin only)
   */
  createBadge(request: CreateBadgeRequest): Observable<CreateBadgeResponse> {
    return this.api.post<CreateBadgeResponse>('badge', request);
  }

  /**
   * Update badge (Admin only)
   */
  updateBadge(
    badgeId: number,
    request: UpdateBadgeRequest
  ): Observable<{ success: boolean; message: string }> {
    return this.api.put<{ success: boolean; message: string }>(
      `badge/${badgeId}`,
      request
    );
  }

  /**
   * Delete badge (Admin only)
   */
  deleteBadge(
    badgeId: number
  ): Observable<{ success: boolean; message: string }> {
    return this.api.delete<{ success: boolean; message: string }>(
      `badge/${badgeId}`
    );
  }

  /**
   * Award badge to user (Admin only)
   */
  awardBadge(request: AwardBadgeRequest): Observable<AwardBadgeResponse> {
    return this.api.post<AwardBadgeResponse>('badge/award', request);
  }

  /**
   * Get all user badges (Admin only)
   */
  getAllUserBadges(
    page: number = 1,
    pageSize: number = 20,
    type?: BadgeType,
    isEarned?: boolean
  ): Observable<UserBadgesListResponse> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (type) params.append('type', type);
    if (isEarned !== undefined) params.append('isEarned', isEarned.toString());

    return this.api.get<UserBadgesListResponse>(
      `badge/all-users?${params.toString()}`
    );
  }
}
