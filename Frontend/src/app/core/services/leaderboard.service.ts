import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import {
  GetLeaderboardRequest,
  LeaderboardResponse,
  GetUserRankRequest,
  UserRankResponse,
  LeaderboardStatsResponse,
  LeaderboardType,
} from '../../shared/interfaces/leaderboard';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private readonly api = inject(ApiService);

  /**
   * Get leaderboard
   */
  getLeaderboard(
    request: GetLeaderboardRequest
  ): Observable<LeaderboardResponse> {
    const params = new URLSearchParams({
      type: request.type,
      page: request.page.toString(),
      pageSize: request.pageSize.toString(),
    });

    if (request.language) params.append('language', request.language);
    if (request.fromDate) params.append('fromDate', request.fromDate);
    if (request.toDate) params.append('toDate', request.toDate);

    return this.api.get<LeaderboardResponse>(
      `leaderboard?${params.toString()}`
    );
  }

  /**
   * Get overall leaderboard
   */
  getOverallLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Overall',
      page,
      pageSize,
    });
  }

  /**
   * Get weekly leaderboard
   */
  getWeeklyLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Weekly',
      page,
      pageSize,
    });
  }

  /**
   * Get monthly leaderboard
   */
  getMonthlyLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Monthly',
      page,
      pageSize,
    });
  }

  /**
   * Get all-time leaderboard
   */
  getAllTimeLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'AllTime',
      page,
      pageSize,
    });
  }

  /**
   * Get language-specific leaderboard
   */
  getLanguageLeaderboard(
    language: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Language',
      page,
      pageSize,
      language,
    });
  }

  /**
   * Get badges leaderboard
   */
  getBadgesLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Badges',
      page,
      pageSize,
    });
  }

  /**
   * Get win rate leaderboard
   */
  getWinRateLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'WinRate',
      page,
      pageSize,
    });
  }

  /**
   * Get experience leaderboard
   */
  getExperienceLeaderboard(
    page: number = 1,
    pageSize: number = 20
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type: 'Experience',
      page,
      pageSize,
    });
  }

  /**
   * Get user rank
   */
  getUserRank(request: GetUserRankRequest): Observable<UserRankResponse> {
    const params = new URLSearchParams({
      type: request.type,
    });

    if (request.language) params.append('language', request.language);

    return this.api.get<UserRankResponse>(
      `leaderboard/user/${request.userId}/rank?${params.toString()}`
    );
  }

  /**
   * Get my rank
   */
  getMyRank(
    type: LeaderboardType,
    language?: string
  ): Observable<UserRankResponse> {
    const params = new URLSearchParams({
      type,
    });

    if (language) params.append('language', language);

    return this.api.get<UserRankResponse>(
      `leaderboard/my-rank?${params.toString()}`
    );
  }

  /**
   * Calculate my rank
   */
  calculateMyRank(
    type: LeaderboardType,
    language?: string
  ): Observable<UserRankResponse> {
    const params = new URLSearchParams({
      type,
    });

    if (language) params.append('language', language);

    return this.api.post<UserRankResponse>(
      `leaderboard/my-rank/calculate?${params.toString()}`,
      {}
    );
  }

  /**
   * Get leaderboard statistics
   */
  getStats(): Observable<LeaderboardStatsResponse> {
    return this.api.get<LeaderboardStatsResponse>('leaderboard/stats');
  }

  /**
   * Get top players
   */
  getTopPlayers(
    count: number = 10,
    type: LeaderboardType = 'Overall'
  ): Observable<LeaderboardResponse> {
    return this.api.get<LeaderboardResponse>(
      `leaderboard/top?count=${count}&type=${type}`
    );
  }

  /**
   * Get leaderboard for specific date range
   */
  getLeaderboardForDateRange(
    type: LeaderboardType,
    fromDate: string,
    toDate: string,
    page: number = 1,
    pageSize: number = 20,
    language?: string
  ): Observable<LeaderboardResponse> {
    return this.getLeaderboard({
      type,
      page,
      pageSize,
      language,
      fromDate,
      toDate,
    });
  }
}
