import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import {
  TeamDetailResponse,
  TeamResponse,
  CreateTeamRequest,
  CreateTeamResponse,
  UpdateTeamRequest,
  UpdateTeamResponse,
  CanJoinTeamResponse,
  TeamStatsResponse,
  TeamMemberResponse,
} from '../../shared/interfaces/teams';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly api = inject(ApiService);

  /**
   * Get team details
   */
  getTeam(teamId: number): Observable<TeamDetailResponse> {
    return this.api.get<TeamDetailResponse>(`team/${teamId}`);
  }

  /**
   * Get team members
   */
  getTeamMembers(teamId: number): Observable<TeamMemberResponse[]> {
    return this.api.get<TeamMemberResponse[]>(`team/${teamId}/members`);
  }

  /**
   * Check if user can join team
   */
  canJoinTeam(teamId: number): Observable<CanJoinTeamResponse> {
    return this.api.get<CanJoinTeamResponse>(`team/${teamId}/can-join`);
  }

  /**
   * Join team
   */
  joinTeam(
    gameSessionId: number,
    teamId: number
  ): Observable<{ success: boolean; message: string; team: TeamResponse }> {
    return this.api.post<{
      success: boolean;
      message: string;
      team: TeamResponse;
    }>('team/join', {
      gameSessionId,
      teamId,
    });
  }

  /**
   * Leave team
   */
  leaveTeam(teamId: number): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>('team/leave', {
      teamId,
    });
  }

  /**
   * Create team
   */
  createTeam(request: CreateTeamRequest): Observable<CreateTeamResponse> {
    return this.api.post<CreateTeamResponse>('team', request);
  }

  /**
   * Update team
   */
  updateTeam(
    teamId: number,
    request: UpdateTeamRequest
  ): Observable<UpdateTeamResponse> {
    return this.api.put<UpdateTeamResponse>(`team/${teamId}`, request);
  }

  /**
   * Delete team
   */
  deleteTeam(
    teamId: number
  ): Observable<{ success: boolean; message: string }> {
    return this.api.delete<{ success: boolean; message: string }>(
      `team/${teamId}`
    );
  }

  /**
   * Get team statistics
   */
  getTeamStats(teamId: number): Observable<TeamStatsResponse> {
    return this.api.get<TeamStatsResponse>(`team/${teamId}/stats`);
  }

  /**
   * Get my teams
   */
  getMyTeams(): Observable<TeamResponse[]> {
    return this.api.get<TeamResponse[]>('team/my');
  }

  /**
   * Get teams for game session
   */
  getTeamsForGame(gameSessionId: number): Observable<TeamResponse[]> {
    return this.api.get<TeamResponse[]>(`team/game/${gameSessionId}`);
  }

  /**
   * Promote team member to leader
   */
  promoteToLeader(
    teamId: number,
    userId: string
  ): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `team/${teamId}/promote`,
      {
        userId,
      }
    );
  }

  /**
   * Remove team member
   */
  removeMember(
    teamId: number,
    userId: string
  ): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `team/${teamId}/remove-member`,
      {
        userId,
      }
    );
  }

  /**
   * Transfer team leadership
   */
  transferLeadership(
    teamId: number,
    newLeaderId: string
  ): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `team/${teamId}/transfer-leadership`,
      {
        newLeaderId,
      }
    );
  }

  /**
   * Get team chat messages
   */
  getTeamChatMessages(
    teamId: number,
    page: number = 1,
    pageSize: number = 50
  ): Observable<any> {
    return this.api.get<any>(
      `team/${teamId}/chat?page=${page}&pageSize=${pageSize}`
    );
  }

  /**
   * Send team chat message
   */
  sendTeamChatMessage(
    teamId: number,
    message: string
  ): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `team/${teamId}/chat`,
      {
        message,
      }
    );
  }

  /**
   * Get team word guesses
   */
  getTeamWordGuesses(
    teamId: number,
    gameSessionId?: number
  ): Observable<any[]> {
    const params = gameSessionId ? `?gameSessionId=${gameSessionId}` : '';
    return this.api.get<any[]>(`team/${teamId}/word-guesses${params}`);
  }

  /**
   * Get team round results
   */
  getTeamRoundResults(
    teamId: number,
    gameSessionId?: number
  ): Observable<any[]> {
    const params = gameSessionId ? `?gameSessionId=${gameSessionId}` : '';
    return this.api.get<any[]>(`team/${teamId}/round-results${params}`);
  }
}
