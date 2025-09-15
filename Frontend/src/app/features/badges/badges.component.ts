import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BadgeService } from '../../core/services/badge.service';
import {
  BadgeResponse,
  BadgeType,
  UserBadgesListResponse,
} from '../../shared/interfaces/badges';

@Component({
  selector: 'app-badges',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="badges-container">
      <!-- Header -->
      <div class="badges-header">
        <h1 class="page-title">Badges</h1>
        <p class="page-subtitle">Earn achievements and track your progress</p>
      </div>

      <!-- Stats Overview -->
      <div class="stats-overview" *ngIf="userBadges()">
        <div class="stat-card">
          <div class="stat-icon">🏅</div>
          <div class="stat-content">
            <div class="stat-value">{{ userBadges()?.totalEarned || 0 }}</div>
            <div class="stat-label">Earned</div>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">🎯</div>
          <div class="stat-content">
            <div class="stat-value">
              {{ userBadges()?.totalAvailable || 0 }}
            </div>
            <div class="stat-label">Available</div>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">📊</div>
          <div class="stat-content">
            <div class="stat-value">{{ getCompletionPercentage() }}%</div>
            <div class="stat-label">Complete</div>
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <div class="filter-group">
          <label class="filter-label">Type:</label>
          <select
            [(ngModel)]="selectedType"
            (change)="filterBadges()"
            class="filter-select"
          >
            <option value="">All Types</option>
            <option value="Achievement">Achievement</option>
            <option value="Progress">Progress</option>
            <option value="Milestone">Milestone</option>
            <option value="Other">Other</option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">Status:</label>
          <select
            [(ngModel)]="selectedStatus"
            (change)="filterBadges()"
            class="filter-select"
          >
            <option value="">All Badges</option>
            <option value="earned">Earned Only</option>
            <option value="available">Available Only</option>
          </select>
        </div>
      </div>

      <!-- Badges Grid -->
      <div class="badges-grid">
        <div
          class="badge-card"
          *ngFor="let badge of filteredBadges()"
          [class.earned]="isBadgeEarned(badge.id)"
          [class.recent]="isRecentlyEarned(badge.id)"
        >
          <div class="badge-header">
            <div class="badge-icon">
              <img
                *ngIf="badge.iconUrl"
                [src]="badge.iconUrl"
                [alt]="badge.name"
                class="icon-image"
              />
              <div *ngIf="!badge.iconUrl" class="icon-placeholder">
                {{ getBadgeIcon(badge.type) }}
              </div>
            </div>
            <div class="badge-type">{{ badge.type }}</div>
          </div>

          <div class="badge-content">
            <h3 class="badge-name">{{ badge.name }}</h3>
            <p class="badge-description">{{ badge.description }}</p>

            <div
              class="badge-progress"
              *ngIf="!isBadgeEarned(badge.id) && badge.requiredValue"
            >
              <div class="progress-bar">
                <div
                  class="progress-fill"
                  [style.width.%]="getBadgeProgress(badge.id)"
                ></div>
              </div>
              <div class="progress-text">
                {{ getBadgeProgress(badge.id) }}% Complete
              </div>
            </div>

            <div class="badge-earned" *ngIf="isBadgeEarned(badge.id)">
              <span class="earned-icon">✓</span>
              <span class="earned-text">Earned</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="filteredBadges().length === 0">
        <div class="empty-icon">🏅</div>
        <h3 class="empty-title">No badges found</h3>
        <p class="empty-message">
          Try adjusting your filters to see more badges
        </p>
      </div>
    </div>
  `,
  styles: [
    `
      .badges-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .badges-header {
        text-align: center;
        margin-bottom: 3rem;
      }

      .page-title {
        font-size: 2.5rem;
        font-weight: 700;
        color: #e6e6e6;
        margin-bottom: 0.5rem;
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }

      .page-subtitle {
        color: #a0aec0;
        font-size: 1.125rem;
      }

      .stats-overview {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1.5rem;
        margin-bottom: 3rem;
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

      .filters-section {
        display: flex;
        gap: 2rem;
        margin-bottom: 2rem;
        flex-wrap: wrap;
      }

      .filter-group {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .filter-label {
        color: #e6e6e6;
        font-weight: 500;
        font-size: 0.875rem;
      }

      .filter-select {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        font-size: 0.875rem;
        min-width: 150px;
      }

      .filter-select:focus {
        outline: none;
        border-color: #f2c037;
      }

      .badges-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        gap: 1.5rem;
      }

      .badge-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 1.5rem;
        transition: all 0.2s ease;
        position: relative;
        overflow: hidden;
      }

      .badge-card:hover {
        transform: translateY(-4px);
        border-color: #4a5568;
      }

      .badge-card.earned {
        border-color: #1ec8a5;
        background: linear-gradient(135deg, #171a21 0%, #1a2d2a 100%);
      }

      .badge-card.recent {
        border-color: #f2c037;
        box-shadow: 0 0 20px rgba(242, 192, 55, 0.2);
      }

      .badge-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
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

      .icon-image {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      .icon-placeholder {
        font-size: 1.5rem;
        color: #a0aec0;
      }

      .badge-type {
        background-color: #2d3748;
        color: #a0aec0;
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-size: 0.75rem;
        font-weight: 500;
      }

      .badge-content {
        flex: 1;
      }

      .badge-name {
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.5rem;
      }

      .badge-description {
        color: #a0aec0;
        line-height: 1.5;
        margin-bottom: 1rem;
      }

      .badge-progress {
        margin-top: 1rem;
      }

      .progress-bar {
        width: 100%;
        height: 8px;
        background-color: #2d3748;
        border-radius: 4px;
        overflow: hidden;
        margin-bottom: 0.5rem;
      }

      .progress-fill {
        height: 100%;
        background: linear-gradient(90deg, #f2c037, #e0b22f);
        transition: width 0.3s ease;
      }

      .progress-text {
        color: #a0aec0;
        font-size: 0.75rem;
        text-align: center;
      }

      .badge-earned {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: #1ec8a5;
        font-weight: 600;
        font-size: 0.875rem;
      }

      .earned-icon {
        width: 20px;
        height: 20px;
        background-color: #1ec8a5;
        color: #0f1115;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 0.75rem;
      }

      .empty-state {
        text-align: center;
        padding: 4rem 2rem;
      }

      .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
      }

      .empty-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.5rem;
      }

      .empty-message {
        color: #a0aec0;
      }

      @media (max-width: 768px) {
        .badges-grid {
          grid-template-columns: 1fr;
        }

        .filters-section {
          flex-direction: column;
          gap: 1rem;
        }

        .filter-select {
          min-width: auto;
        }
      }
    `,
  ],
})
export class BadgesComponent implements OnInit {
  private readonly badgeService = inject(BadgeService);

  badges = signal<BadgeResponse[]>([]);
  userBadges = signal<UserBadgesListResponse | null>(null);
  filteredBadges = signal<BadgeResponse[]>([]);

  selectedType = '';
  selectedStatus = '';

  ngOnInit() {
    this.loadBadges();
    this.loadUserBadges();
  }

  loadBadges() {
    this.badgeService.getAllBadges().subscribe((badges) => {
      this.badges.set(badges);
      this.filteredBadges.set(badges);
    });
  }

  loadUserBadges() {
    this.badgeService.getMyBadges().subscribe((userBadges) => {
      this.userBadges.set(userBadges);
    });
  }

  filterBadges() {
    let filtered = this.badges();

    if (this.selectedType) {
      filtered = filtered.filter((badge) => badge.type === this.selectedType);
    }

    if (this.selectedStatus === 'earned') {
      const earnedIds =
        this.userBadges()?.earnedBadges.map((ub) => ub.badgeId) || [];
      filtered = filtered.filter((badge) => earnedIds.includes(badge.id));
    } else if (this.selectedStatus === 'available') {
      const earnedIds =
        this.userBadges()?.earnedBadges.map((ub) => ub.badgeId) || [];
      filtered = filtered.filter((badge) => !earnedIds.includes(badge.id));
    }

    this.filteredBadges.set(filtered);
  }

  isBadgeEarned(badgeId: number): boolean {
    return (
      this.userBadges()?.earnedBadges.some((ub) => ub.badgeId === badgeId) ||
      false
    );
  }

  isRecentlyEarned(badgeId: number): boolean {
    return (
      this.userBadges()?.earnedBadges.some(
        (ub) => ub.badgeId === badgeId && ub.isRecentlyEarned
      ) || false
    );
  }

  getBadgeProgress(badgeId: number): number {
    // This would need to be implemented with actual progress data
    return Math.floor(Math.random() * 100);
  }

  getBadgeIcon(type: BadgeType): string {
    const icons: { [key in BadgeType]: string } = {
      Achievement: '🏆',
      Progress: '📈',
      Milestone: '🎯',
      Other: '🏅',
    };
    return icons[type] || '🏅';
  }

  getCompletionPercentage(): number {
    const earned = this.userBadges()?.totalEarned || 0;
    const available = this.userBadges()?.totalAvailable || 0;
    return available > 0 ? Math.round((earned / available) * 100) : 0;
  }
}
