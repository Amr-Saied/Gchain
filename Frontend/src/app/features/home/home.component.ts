import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { GameService } from '../../core/services/game.service';
import { BadgeService } from '../../core/services/badge.service';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="home-container">
      <!-- Hero Section -->
      <section class="hero">
        <div class="hero-content">
          <h1 class="hero-title">Welcome to Gchain</h1>
          <p class="hero-subtitle">
            The ultimate word association game where strategy meets creativity
          </p>
          <div class="hero-actions" *ngIf="auth.isAuthenticated()">
            <a routerLink="/games" class="btn btn-primary btn-large">
              <span class="btn-icon">🎮</span>
              Play Now
            </a>
            <a routerLink="/leaderboard" class="btn btn-secondary btn-large">
              <span class="btn-icon">🏆</span>
              View Leaderboard
            </a>
          </div>
          <div class="hero-actions" *ngIf="!auth.isAuthenticated()">
            <a routerLink="/login" class="btn btn-primary btn-large">
              <span class="btn-icon">🚀</span>
              Get Started
            </a>
          </div>
        </div>
      </section>

      <!-- Stats Section -->
      <section class="stats-section" *ngIf="auth.isAuthenticated()">
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-icon">🎮</div>
            <div class="stat-content">
              <div class="stat-value">
                {{ userStats().totalGamesPlayed || 0 }}
              </div>
              <div class="stat-label">Games Played</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🏆</div>
            <div class="stat-content">
              <div class="stat-value">{{ userStats().totalGamesWon || 0 }}</div>
              <div class="stat-label">Games Won</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🏅</div>
            <div class="stat-content">
              <div class="stat-value">{{ userStats().badgesEarned || 0 }}</div>
              <div class="stat-label">Badges Earned</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon">⭐</div>
            <div class="stat-content">
              <div class="stat-value">{{ userStats().totalScore || 0 }}</div>
              <div class="stat-label">Total Score</div>
            </div>
          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section class="features-section">
        <h2 class="section-title">Game Features</h2>
        <div class="features-grid">
          <div class="feature-card">
            <div class="feature-icon">🧠</div>
            <h3 class="feature-title">Word Association</h3>
            <p class="feature-description">
              Challenge your vocabulary and creativity with AI-powered word
              similarity scoring
            </p>
          </div>
          <div class="feature-card">
            <div class="feature-icon">👥</div>
            <h3 class="feature-title">Team Play</h3>
            <p class="feature-description">
              Join teams and compete against others in real-time multiplayer
              matches
            </p>
          </div>
          <div class="feature-card">
            <div class="feature-icon">🏅</div>
            <h3 class="feature-title">Achievements</h3>
            <p class="feature-description">
              Earn badges and climb the leaderboard as you improve your skills
            </p>
          </div>
          <div class="feature-card">
            <div class="feature-icon">⚡</div>
            <h3 class="feature-title">Real-time</h3>
            <p class="feature-description">
              Experience instant updates and live chat with SignalR technology
            </p>
          </div>
        </div>
      </section>

      <!-- Recent Activity -->
      <section class="recent-activity" *ngIf="auth.isAuthenticated()">
        <h2 class="section-title">Recent Activity</h2>
        <div class="activity-list">
          <div
            class="activity-item"
            *ngFor="let notification of recentNotifications()"
          >
            <div class="activity-icon">
              {{ getNotificationIcon(notification.type) }}
            </div>
            <div class="activity-content">
              <div class="activity-title">{{ notification.title }}</div>
              <div class="activity-message">{{ notification.message }}</div>
              <div class="activity-time">
                {{ formatTime(notification.createdAt) }}
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .home-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .hero {
        text-align: center;
        padding: 4rem 0;
        background: linear-gradient(135deg, #171a21 0%, #2d3748 100%);
        border-radius: 16px;
        margin-bottom: 3rem;
      }

      .hero-content {
        max-width: 600px;
        margin: 0 auto;
      }

      .hero-title {
        font-size: 3rem;
        font-weight: 700;
        color: #e6e6e6;
        margin-bottom: 1rem;
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }

      .hero-subtitle {
        font-size: 1.25rem;
        color: #a0aec0;
        margin-bottom: 2rem;
        line-height: 1.6;
      }

      .hero-actions {
        display: flex;
        gap: 1rem;
        justify-content: center;
        flex-wrap: wrap;
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

      .btn-large {
        padding: 1rem 2rem;
        font-size: 1.125rem;
      }

      .btn-primary {
        background-color: #f2c037;
        color: #0f1115;
      }

      .btn-primary:hover {
        background-color: #e0b22f;
        transform: translateY(-2px);
      }

      .btn-secondary {
        background-color: #1ec8a5;
        color: #0f1115;
      }

      .btn-secondary:hover {
        background-color: #1ab394;
        transform: translateY(-2px);
      }

      .btn-icon {
        font-size: 1.25rem;
      }

      .stats-section {
        margin-bottom: 3rem;
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

      .features-section {
        margin-bottom: 3rem;
      }

      .section-title {
        font-size: 2rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 2rem;
        text-align: center;
      }

      .features-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
        gap: 1.5rem;
      }

      .feature-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 2rem;
        text-align: center;
        transition: transform 0.2s ease;
      }

      .feature-card:hover {
        transform: translateY(-4px);
      }

      .feature-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
      }

      .feature-title {
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.75rem;
      }

      .feature-description {
        color: #a0aec0;
        line-height: 1.6;
      }

      .recent-activity {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 2rem;
      }

      .activity-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .activity-item {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 1rem;
        background-color: #0f1115;
        border-radius: 8px;
        border: 1px solid #2d3748;
      }

      .activity-icon {
        font-size: 1.5rem;
        width: 40px;
        height: 40px;
        background-color: #2d3748;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .activity-content {
        flex: 1;
      }

      .activity-title {
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.25rem;
      }

      .activity-message {
        color: #a0aec0;
        font-size: 0.875rem;
        margin-bottom: 0.25rem;
      }

      .activity-time {
        color: #718096;
        font-size: 0.75rem;
      }

      @media (max-width: 768px) {
        .hero-title {
          font-size: 2rem;
        }

        .hero-actions {
          flex-direction: column;
          align-items: center;
        }

        .stats-grid {
          grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        }

        .features-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class HomeComponent {
  auth = inject(AuthService);
  gameService = inject(GameService);
  badgeService = inject(BadgeService);
  leaderboardService = inject(LeaderboardService);
  notificationService = inject(NotificationService);

  userStats = signal<any>({});
  recentNotifications = signal<any[]>([]);

  ngOnInit() {
    if (this.auth.isAuthenticated()) {
      this.loadUserData();
    }
  }

  loadUserData() {
    // Load user stats
    this.gameService.getMyGames().subscribe((games) => {
      // Process games data for stats
    });

    // Load recent notifications
    this.notificationService
      .getRecentNotifications(5)
      .subscribe((notifications) => {
        this.recentNotifications.set(notifications);
      });
  }

  getNotificationIcon(type: string): string {
    const icons: { [key: string]: string } = {
      BadgeEarned: '🏅',
      GameInvite: '🎮',
      GameStarted: '▶️',
      GameEnded: '🏁',
      TeamInvite: '👥',
      LeaderboardUpdate: '📈',
      System: '⚙️',
      Achievement: '⭐',
      FriendRequest: '👤',
      General: '📢',
    };
    return icons[type] || '📢';
  }

  formatTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMinutes = Math.floor(
      (now.getTime() - date.getTime()) / (1000 * 60)
    );

    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}h ago`;
    return `${Math.floor(diffInMinutes / 1440)}d ago`;
  }
}
