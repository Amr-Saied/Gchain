import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { BadgeService } from '../../core/services/badge.service';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import {
  UserProfileResponse,
  UpdateUserProfileRequest,
  UserStatsResponse,
  UserPreferences,
} from '../../shared/interfaces/users';
import { BadgeResponse } from '../../shared/interfaces/badges';
import { UserRankResponse } from '../../shared/interfaces/leaderboard';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="profile-container">
      <!-- Header -->
      <div class="profile-header">
        <div class="profile-avatar-section">
          <div class="avatar-container">
            <img
              [src]="
                userProfile()?.profilePictureUrl || '/assets/default-avatar.png'
              "
              [alt]="userProfile()?.userName || 'User'"
              class="profile-avatar"
            />
            <button class="avatar-edit-btn" (click)="triggerFileInput()">
              <span class="edit-icon">📷</span>
            </button>
            <input
              #fileInput
              type="file"
              accept="image/*"
              (change)="onFileSelected($event)"
              style="display: none"
            />
          </div>
          <div class="profile-info">
            <h1 class="profile-name">
              {{ userProfile()?.userName || 'User' }}
            </h1>
            <p class="profile-email">
              {{ userProfile()?.email || 'No email' }}
            </p>
            <div class="profile-level">
              <span class="level-badge"
                >Level {{ userProfile()?.level || 1 }}</span
              >
              <span class="experience"
                >{{ userProfile()?.experiencePoints || 0 }} XP</span
              >
            </div>
          </div>
        </div>
        <div class="profile-actions">
          <button class="btn btn-primary" (click)="toggleEditMode()">
            {{ isEditMode ? 'Save Changes' : 'Edit Profile' }}
          </button>
          <button class="btn btn-secondary" (click)="logout()">Logout</button>
        </div>
      </div>

      <!-- Stats Overview -->
      <div class="stats-section">
        <h2 class="section-title">Statistics</h2>
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-icon">🎮</div>
            <div class="stat-content">
              <div class="stat-value">
                {{ userStats()?.totalGamesPlayed || 0 }}
              </div>
              <div class="stat-label">Games Played</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🏆</div>
            <div class="stat-content">
              <div class="stat-value">
                {{ userStats()?.totalGamesWon || 0 }}
              </div>
              <div class="stat-label">Games Won</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">📊</div>
            <div class="stat-content">
              <div class="stat-value">
                {{ formatPercentage(userStats()?.winRate || 0) }}
              </div>
              <div class="stat-label">Win Rate</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">⭐</div>
            <div class="stat-content">
              <div class="stat-value">{{ userStats()?.totalScore || 0 }}</div>
              <div class="stat-label">Total Score</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🏅</div>
            <div class="stat-content">
              <div class="stat-value">{{ userStats()?.badgesEarned || 0 }}</div>
              <div class="stat-label">Badges Earned</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">📈</div>
            <div class="stat-content">
              <div class="stat-value">{{ myRank()?.rank || 'N/A' }}</div>
              <div class="stat-label">Global Rank</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Profile Form -->
      <div class="profile-form-section" *ngIf="isEditMode">
        <h2 class="section-title">Edit Profile</h2>
        <form
          class="profile-form"
          (ngSubmit)="updateProfile()"
          #profileForm="ngForm"
        >
          <div class="form-group">
            <label class="form-label">Username</label>
            <input
              type="text"
              [(ngModel)]="editProfile.userName"
              name="userName"
              required
              class="form-input"
              placeholder="Enter username"
            />
          </div>

          <div class="form-group">
            <label class="form-label">Email</label>
            <input
              type="email"
              [(ngModel)]="editProfile.email"
              name="email"
              required
              class="form-input"
              placeholder="Enter email"
            />
          </div>

          <div class="form-actions">
            <button
              type="button"
              class="btn btn-secondary"
              (click)="cancelEdit()"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="btn btn-primary"
              [disabled]="!profileForm.valid || isUpdating"
            >
              <span *ngIf="isUpdating" class="btn-loading">⏳</span>
              {{ isUpdating ? 'Updating...' : 'Update Profile' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Recent Badges -->
      <div class="badges-section">
        <h2 class="section-title">Recent Badges</h2>
        <div
          class="badges-grid"
          *ngIf="recentBadges().length > 0; else noBadges"
        >
          <div class="badge-card" *ngFor="let badge of recentBadges()">
            <div class="badge-icon">
              <img
                *ngIf="badge.iconUrl"
                [src]="badge.iconUrl"
                [alt]="badge.name"
                class="badge-image"
              />
              <div *ngIf="!badge.iconUrl" class="badge-placeholder">
                {{ getBadgeIcon(badge.type) }}
              </div>
            </div>
            <div class="badge-info">
              <h4 class="badge-name">{{ badge.name }}</h4>
              <p class="badge-description">{{ badge.description }}</p>
            </div>
          </div>
        </div>
        <ng-template #noBadges>
          <div class="empty-state">
            <div class="empty-icon">🏅</div>
            <h3 class="empty-title">No badges yet</h3>
            <p class="empty-message">
              Start playing games to earn your first badge!
            </p>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [
    `
      .profile-container {
        max-width: 1000px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .profile-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 3rem;
        padding: 2rem;
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
      }

      .profile-avatar-section {
        display: flex;
        align-items: center;
        gap: 1.5rem;
      }

      .avatar-container {
        position: relative;
      }

      .profile-avatar {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        object-fit: cover;
        border: 3px solid #f2c037;
      }

      .avatar-edit-btn {
        position: absolute;
        bottom: 0;
        right: 0;
        width: 28px;
        height: 28px;
        background-color: #f2c037;
        border: none;
        border-radius: 50%;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.2s ease;
      }

      .avatar-edit-btn:hover {
        background-color: #e0b22f;
        transform: scale(1.1);
      }

      .edit-icon {
        font-size: 0.875rem;
      }

      .profile-info {
        flex: 1;
      }

      .profile-name {
        font-size: 2rem;
        font-weight: 700;
        color: #e6e6e6;
        margin: 0 0 0.5rem 0;
      }

      .profile-email {
        color: #a0aec0;
        margin: 0 0 1rem 0;
      }

      .profile-level {
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .level-badge {
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        color: #0f1115;
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-weight: 600;
        font-size: 0.875rem;
      }

      .experience {
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .profile-actions {
        display: flex;
        gap: 1rem;
      }

      .btn {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem 1.5rem;
        border-radius: 12px;
        font-weight: 600;
        text-decoration: none;
        transition: all 0.2s ease;
        border: none;
        cursor: pointer;
        font-size: 1rem;
      }

      .btn-primary {
        background-color: #f2c037;
        color: #0f1115;
      }

      .btn-primary:hover:not(:disabled) {
        background-color: #e0b22f;
        transform: translateY(-2px);
      }

      .btn-secondary {
        background-color: #2d3748;
        color: #e6e6e6;
        border: 1px solid #4a5568;
      }

      .btn-secondary:hover {
        background-color: #4a5568;
      }

      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
        transform: none;
      }

      .btn-loading {
        animation: spin 1s linear infinite;
      }

      .stats-section,
      .profile-form-section,
      .badges-section {
        margin-bottom: 3rem;
      }

      .section-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 1.5rem;
      }

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1.5rem;
      }

      .stat-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 1.5rem;
        display: flex;
        align-items: center;
        gap: 1rem;
        transition: transform 0.2s ease;
      }

      .stat-card:hover {
        transform: translateY(-2px);
      }

      .stat-icon {
        font-size: 2rem;
        width: 60px;
        height: 60px;
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .stat-content {
        flex: 1;
      }

      .stat-value {
        font-size: 2rem;
        font-weight: 700;
        color: #e6e6e6;
        line-height: 1;
      }

      .stat-label {
        color: #a0aec0;
        font-size: 0.875rem;
        margin-top: 0.25rem;
      }

      .profile-form {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 2rem;
      }

      .form-group {
        margin-bottom: 1.5rem;
      }

      .form-label {
        display: block;
        color: #e6e6e6;
        font-weight: 500;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
      }

      .form-input {
        width: 100%;
        background-color: #0f1115;
        border: 1px solid #2d3748;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        font-size: 0.875rem;
      }

      .form-input:focus {
        outline: none;
        border-color: #f2c037;
      }

      .form-actions {
        display: flex;
        gap: 1rem;
        justify-content: flex-end;
        margin-top: 2rem;
      }

      .badges-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1.5rem;
      }

      .badge-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 1.5rem;
        display: flex;
        align-items: center;
        gap: 1rem;
        transition: transform 0.2s ease;
      }

      .badge-card:hover {
        transform: translateY(-2px);
      }

      .badge-icon {
        width: 48px;
        height: 48px;
        border-radius: 12px;
        background: linear-gradient(135deg, #2d3748, #4a5568);
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
      }

      .badge-image {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      .badge-placeholder {
        font-size: 1.5rem;
        color: #a0aec0;
      }

      .badge-info {
        flex: 1;
      }

      .badge-name {
        font-size: 1rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0 0 0.25rem 0;
      }

      .badge-description {
        color: #a0aec0;
        font-size: 0.875rem;
        margin: 0;
        line-height: 1.4;
      }

      .empty-state {
        text-align: center;
        padding: 3rem 2rem;
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
      }

      .empty-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
      }

      .empty-title {
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.5rem;
      }

      .empty-message {
        color: #a0aec0;
      }

      @keyframes spin {
        0% {
          transform: rotate(0deg);
        }
        100% {
          transform: rotate(360deg);
        }
      }

      @media (max-width: 768px) {
        .profile-header {
          flex-direction: column;
          gap: 2rem;
          text-align: center;
        }

        .profile-avatar-section {
          flex-direction: column;
          text-align: center;
        }

        .profile-actions {
          width: 100%;
          justify-content: center;
        }

        .stats-grid {
          grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        }

        .form-actions {
          flex-direction: column;
        }
      }
    `,
  ],
})
export class ProfileComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly auth = inject(AuthService);
  private readonly badgeService = inject(BadgeService);
  private readonly leaderboardService = inject(LeaderboardService);

  userProfile = signal<UserProfileResponse | null>(null);
  userStats = signal<UserStatsResponse | null>(null);
  recentBadges = signal<BadgeResponse[]>([]);
  myRank = signal<UserRankResponse | null>(null);

  isEditMode = false;
  isUpdating = false;

  editProfile = {
    userName: '',
    email: '',
  };

  ngOnInit() {
    this.loadProfile();
    this.loadStats();
    this.loadRecentBadges();
    this.loadMyRank();
  }

  loadProfile() {
    this.userService.getProfile().subscribe((profile) => {
      this.userProfile.set(profile);
      this.editProfile = {
        userName: profile.userName,
        email: profile.email,
      };
    });
  }

  loadStats() {
    this.userService.getStats().subscribe((stats) => {
      this.userStats.set(stats);
    });
  }

  loadRecentBadges() {
    this.badgeService.getMyBadges(1, 6).subscribe((response) => {
      this.recentBadges.set(
        response.earnedBadges.map((ub) => ({
          id: ub.badgeId,
          name: ub.badgeName,
          description: ub.badgeDescription,
          criteria: '', // UserBadgeResponse doesn't include criteria
          iconUrl: ub.badgeIconUrl,
          type: ub.badgeType,
          requiredValue: null,
          isActive: true,
          createdAt: ub.earnedAt,
          updatedAt: null,
        }))
      );
    });
  }

  loadMyRank() {
    this.leaderboardService.getMyRank('Overall').subscribe((rank) => {
      this.myRank.set(rank);
    });
  }

  toggleEditMode() {
    if (this.isEditMode) {
      this.updateProfile();
    } else {
      this.isEditMode = true;
    }
  }

  updateProfile() {
    this.isUpdating = true;
    const request: UpdateUserProfileRequest = {
      newUserName: this.editProfile.userName,
      profilePicture: null,
    };

    this.userService.updateProfile(request).subscribe({
      next: (response) => {
        if (response.success && response.user) {
          this.userProfile.set(response.user);
          this.isEditMode = false;
        }
        this.isUpdating = false;
      },
      error: (error) => {
        console.error('Failed to update profile:', error);
        this.isUpdating = false;
      },
    });
  }

  cancelEdit() {
    this.isEditMode = false;
    this.editProfile = {
      userName: this.userProfile()?.userName || '',
      email: this.userProfile()?.email || '',
    };
  }

  triggerFileInput() {
    const fileInput = document.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    fileInput?.click();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      console.log('File selected:', file);
    }
  }

  logout() {
    this.auth.logout();
  }

  formatPercentage(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  }

  getBadgeIcon(type: string): string {
    const icons: { [key: string]: string } = {
      Achievement: '🏆',
      Progress: '📈',
      Milestone: '🎯',
      Other: '🏅',
    };
    return icons[type] || '🏅';
  }
}
