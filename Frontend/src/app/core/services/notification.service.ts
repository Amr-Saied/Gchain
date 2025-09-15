import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import {
  NotificationResponse,
  NotificationListResponse,
  CreateNotificationRequest,
  CreateNotificationResponse,
  MarkNotificationReadRequest,
  MarkNotificationReadResponse,
  MarkAllNotificationsReadResponse,
  DeleteNotificationRequest,
  DeleteNotificationResponse,
  NotificationStatsResponse,
  NotificationType,
} from '../../shared/interfaces/notifications';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);

  /**
   * Get notifications
   */
  getNotifications(
    page: number = 1,
    pageSize: number = 20,
    type?: NotificationType,
    isRead?: boolean
  ): Observable<NotificationListResponse> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (type) params.append('type', type);
    if (isRead !== undefined) params.append('isRead', isRead.toString());

    return this.api.get<NotificationListResponse>(
      `notification?${params.toString()}`
    );
  }

  /**
   * Get unread notifications
   */
  getUnreadNotifications(
    page: number = 1,
    pageSize: number = 20
  ): Observable<NotificationListResponse> {
    return this.getNotifications(page, pageSize, undefined, false);
  }

  /**
   * Get notifications by type
   */
  getNotificationsByType(
    type: NotificationType,
    page: number = 1,
    pageSize: number = 20
  ): Observable<NotificationListResponse> {
    return this.getNotifications(page, pageSize, type);
  }

  /**
   * Get notification by ID
   */
  getNotification(notificationId: number): Observable<NotificationResponse> {
    return this.api.get<NotificationResponse>(`notification/${notificationId}`);
  }

  /**
   * Mark notification as read
   */
  markAsRead(notificationId: number): Observable<MarkNotificationReadResponse> {
    return this.api.post<MarkNotificationReadResponse>('notification/read', {
      notificationId,
    });
  }

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): Observable<MarkAllNotificationsReadResponse> {
    return this.api.post<MarkAllNotificationsReadResponse>(
      'notification/read-all',
      {}
    );
  }

  /**
   * Delete notification
   */
  deleteNotification(
    notificationId: number
  ): Observable<DeleteNotificationResponse> {
    return this.api.delete<DeleteNotificationResponse>(
      `notification/${notificationId}`
    );
  }

  /**
   * Delete all notifications
   */
  deleteAllNotifications(): Observable<{
    success: boolean;
    message: string;
    deletedCount: number;
  }> {
    return this.api.delete<{
      success: boolean;
      message: string;
      deletedCount: number;
    }>('notification/all');
  }

  /**
   * Delete read notifications
   */
  deleteReadNotifications(): Observable<{
    success: boolean;
    message: string;
    deletedCount: number;
  }> {
    return this.api.delete<{
      success: boolean;
      message: string;
      deletedCount: number;
    }>('notification/read');
  }

  /**
   * Get notification statistics
   */
  getStats(): Observable<NotificationStatsResponse> {
    return this.api.get<NotificationStatsResponse>('notification/stats');
  }

  /**
   * Create notification (Admin only)
   */
  createNotification(
    request: CreateNotificationRequest
  ): Observable<CreateNotificationResponse> {
    return this.api.post<CreateNotificationResponse>('notification', request);
  }

  /**
   * Get recent notifications
   */
  getRecentNotifications(
    count: number = 5
  ): Observable<NotificationResponse[]> {
    return this.api.get<NotificationResponse[]>(
      `notification/recent?count=${count}`
    );
  }

  /**
   * Get notification count
   */
  getNotificationCount(): Observable<{ total: number; unread: number }> {
    return this.api.get<{ total: number; unread: number }>(
      'notification/count'
    );
  }

  /**
   * Update notification preferences
   */
  updatePreferences(preferences: {
    email: boolean;
    push: boolean;
    gameInvites: boolean;
    badgeEarned: boolean;
    leaderboardUpdates: boolean;
  }): Observable<{ success: boolean; message: string }> {
    return this.api.put<{ success: boolean; message: string }>(
      'notification/preferences',
      preferences
    );
  }

  /**
   * Get notification preferences
   */
  getPreferences(): Observable<{
    email: boolean;
    push: boolean;
    gameInvites: boolean;
    badgeEarned: boolean;
    leaderboardUpdates: boolean;
  }> {
    return this.api.get<{
      email: boolean;
      push: boolean;
      gameInvites: boolean;
      badgeEarned: boolean;
      leaderboardUpdates: boolean;
    }>('notification/preferences');
  }
}
