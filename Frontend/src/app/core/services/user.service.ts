import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import {
  UserProfileResponse,
  UpdateUserProfileRequest,
  UpdateUserProfileResponse,
  UserStatsResponse,
  UserPreferences,
} from '../../shared/interfaces/users';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(ApiService);

  /**
   * Get current user profile
   */
  getProfile(): Observable<UserProfileResponse> {
    return this.api.get<UserProfileResponse>('user/profile');
  }

  /**
   * Update user profile
   */
  updateProfile(
    request: UpdateUserProfileRequest
  ): Observable<UpdateUserProfileResponse> {
    const formData = new FormData();

    if (request.newUserName) {
      formData.append('NewUserName', request.newUserName);
    }

    if (request.profilePicture) {
      formData.append('ProfilePicture', request.profilePicture);
    }

    return this.api.put<UpdateUserProfileResponse>('user/profile', formData);
  }

  /**
   * Get user statistics
   */
  getStats(): Observable<UserStatsResponse> {
    return this.api.get<UserStatsResponse>('user/stats');
  }

  /**
   * Get user preferences
   */
  getPreferences(): Observable<UserPreferences> {
    return this.api.get<UserPreferences>('user/preferences');
  }

  /**
   * Update user preferences
   */
  updatePreferences(
    preferences: UserPreferences
  ): Observable<{ success: boolean; message: string }> {
    return this.api.put<{ success: boolean; message: string }>(
      'user/preferences',
      preferences
    );
  }

  /**
   * Delete user account
   */
  deleteAccount(): Observable<{ success: boolean; message: string }> {
    return this.api.delete<{ success: boolean; message: string }>(
      'user/account'
    );
  }

  /**
   * Get user sessions
   */
  getSessions(): Observable<any[]> {
    return this.api.get<any[]>('user/sessions');
  }

  /**
   * Revoke user session
   */
  revokeSession(
    sessionId: number
  ): Observable<{ success: boolean; message: string }> {
    return this.api.delete<{ success: boolean; message: string }>(
      `user/sessions/${sessionId}`
    );
  }

  /**
   * Revoke all sessions except current
   */
  revokeAllSessions(): Observable<{
    success: boolean;
    message: string;
    revokedCount: number;
  }> {
    return this.api.delete<{
      success: boolean;
      message: string;
      revokedCount: number;
    }>('user/sessions/all');
  }
}
