import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { AuthService } from '../../core/services/auth.service';
import {
  LeaderboardResponse,
  LeaderboardType,
  LeaderboardEntry,
  UserRankResponse,
} from '../../shared/interfaces/leaderboard';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="leaderboard-container">
      <!-- Header -->
      <div class="leaderboard-header">
        <h1 class="page-title">Leaderboard</h1>
        <p class="page-subtitle">See how you rank against other players</p>
      </div>

      <!-- Stats Overview -->
      <div class="stats-overview">
        <div class="stat-card">
          <div class="stat-icon">🏆</div>
          <div class="stat-content">
            <div class="stat-value">{{ totalPlayers() }}</div>
            <div class="stat-label">Total Players</div>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">📈</div>
          <div class="stat-content">
            <div class="stat-value">{{ myRank()?.rank || 'N/A' }}</div>
            <div class="stat-label">Your Rank</div>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">⭐</div>
          <div class="stat-content">
            <div class="stat-value">{{ myRank()?.score || 0 }}</div>
            <div class="stat-label">Your Score</div>
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <div class="filter-group">
          <label class="filter-label">Leaderboard Type:</label>
          <select
            [(ngModel)]="selectedType"
            (change)="loadLeaderboard()"
            class="filter-select"
          >
            <option value="Overall">Overall</option>
            <option value="Weekly">Weekly</option>
            <option value="Monthly">Monthly</option>
            <option value="AllTime">All Time</option>
            <option value="Badges">Badges</option>
            <option value="WinRate">Win Rate</option>
            <option value="Experience">Experience</option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">Language:</label>
          <select
            [(ngModel)]="selectedLanguage"
            (change)="loadLeaderboard()"
            class="filter-select"
          >
            <option value="">All Languages</option>
            <option value="English">English</option>
            <option value="Spanish">Spanish</option>
            <option value="French">French</option>
          </select>
        </div>
        <div class="filter-group">
          <label class="filter-label">Page Size:</label>
          <select
            [(ngModel)]="pageSize"
            (change)="loadLeaderboard()"
            class="filter-select"
          >
            <option value="10">10 per page</option>
            <option value="20">20 per page</option>
            <option value="50">50 per page</option>
          </select>
        </div>
      </div>

      <!-- Leaderboard Table -->
      <div class="leaderboard-table-container">
        <div class="table-header">
          <h2 class="table-title">{{ getTableTitle() }}</h2>
          <div class="table-stats">
            <span class="total-entries">{{ totalEntries() }} players</span>
            <span class="current-page"
              >Page {{ currentPage() }} of {{ totalPages() }}</span
            >
          </div>
        </div>

        <div class="table-wrapper">
          <table class="leaderboard-table">
            <thead>
              <tr>
                <th class="rank-col">Rank</th>
                <th class="player-col">Player</th>
                <th class="score-col">Score</th>
                <th class="games-col">Games</th>
                <th class="win-rate-col">Win Rate</th>
                <th class="badges-col">Badges</th>
                <th class="level-col">Level</th>
              </tr>
            </thead>
            <tbody>
              <tr
                *ngFor="let entry of leaderboardEntries(); let i = index"
                [class.current-user]="isCurrentUser(entry.userId)"
                [class.top-three]="i < 3"
                [class.rank-1]="i === 0"
                [class.rank-2]="i === 1"
                [class.rank-3]="i === 2"
              >
                <td class="rank-cell">
                  <div class="rank-display">
                    <span *ngIf="i < 3" class="medal">{{
                      getMedalIcon(i)
                    }}</span>
                    <span class="rank-number">{{ entry.rank }}</span>
                  </div>
                </td>
                <td class="player-cell">
                  <div class="player-info">
                    <img
                      [src]="
                        entry.profilePictureUrl || '/assets/default-avatar.png'
                      "
                      [alt]="entry.userName"
                      class="player-avatar"
                    />
                    <div class="player-details">
                      <div class="player-name">{{ entry.userName }}</div>
                      <div class="player-level">
                        Level {{ entry.level || 1 }}
                      </div>
                    </div>
                  </div>
                </td>
                <td class="score-cell">
                  <div class="score-value">
                    {{ formatNumber(entry.score || entry.totalScore) }}
                  </div>
                </td>
                <td class="games-cell">
                  <div class="games-value">
                    {{ entry.gamesPlayed || entry.totalGamesPlayed }}
                  </div>
                </td>
                <td class="win-rate-cell">
                  <div class="win-rate-value">
                    {{ formatPercentage(entry.winRate) }}
                  </div>
                </td>
                <td class="badges-cell">
                  <div class="badges-value">{{ entry.badgesEarned || 0 }}</div>
                </td>
                <td class="level-cell">
                  <div class="level-value">{{ entry.level || 1 }}</div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination" *ngIf="totalPages() > 1">
          <button
            class="pagination-btn"
            [disabled]="currentPage() <= 1"
            (click)="goToPage(currentPage() - 1)"
          >
            ← Previous
          </button>
          <div class="pagination-pages">
            <button
              *ngFor="let page of getPageNumbers()"
              class="pagination-page"
              [class.active]="page === currentPage()"
              (click)="goToPage(page)"
            >
              {{ page }}
            </button>
          </div>
          <button
            class="pagination-btn"
            [disabled]="currentPage() >= totalPages()"
            (click)="goToPage(currentPage() + 1)"
          >
            Next →
          </button>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="leaderboardEntries().length === 0">
        <div class="empty-icon">🏆</div>
        <h3 class="empty-title">No players found</h3>
        <p class="empty-message">
          Try adjusting your filters to see more results
        </p>
      </div>
    </div>
  `,
  styles: [
    `
      .leaderboard-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .leaderboard-header {
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

      .leaderboard-table-container {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        overflow: hidden;
      }

      .table-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.5rem 2rem;
        border-bottom: 1px solid #2d3748;
      }

      .table-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0;
      }

      .table-stats {
        display: flex;
        gap: 1rem;
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .table-wrapper {
        overflow-x: auto;
      }

      .leaderboard-table {
        width: 100%;
        border-collapse: collapse;
      }

      .leaderboard-table th {
        background-color: #2d3748;
        color: #e6e6e6;
        font-weight: 600;
        padding: 1rem;
        text-align: left;
        border-bottom: 1px solid #4a5568;
      }

      .leaderboard-table td {
        padding: 1rem;
        border-bottom: 1px solid #2d3748;
        vertical-align: middle;
      }

      .leaderboard-table tr:hover {
        background-color: #1a202c;
      }

      .leaderboard-table tr.current-user {
        background-color: rgba(242, 192, 55, 0.1);
        border-left: 4px solid #f2c037;
      }

      .leaderboard-table tr.top-three {
        background-color: rgba(242, 192, 55, 0.05);
      }

      .leaderboard-table tr.rank-1 {
        background-color: rgba(255, 215, 0, 0.1);
        border-left: 4px solid #ffd700;
      }

      .leaderboard-table tr.rank-2 {
        background-color: rgba(192, 192, 192, 0.1);
        border-left: 4px solid #c0c0c0;
      }

      .leaderboard-table tr.rank-3 {
        background-color: rgba(205, 127, 50, 0.1);
        border-left: 4px solid #cd7f32;
      }

      .rank-cell {
        width: 80px;
      }

      .rank-display {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .medal {
        font-size: 1.25rem;
      }

      .rank-number {
        font-weight: 600;
        color: #e6e6e6;
      }

      .player-cell {
        min-width: 200px;
      }

      .player-info {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }

      .player-avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        object-fit: cover;
      }

      .player-details {
        flex: 1;
      }

      .player-name {
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 0.25rem;
      }

      .player-level {
        color: #a0aec0;
        font-size: 0.75rem;
      }

      .score-cell,
      .games-cell,
      .win-rate-cell,
      .badges-cell,
      .level-cell {
        text-align: center;
        min-width: 100px;
      }

      .score-value,
      .games-value,
      .win-rate-value,
      .badges-value,
      .level-value {
        font-weight: 600;
        color: #e6e6e6;
      }

      .pagination {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 1rem;
        padding: 1.5rem 2rem;
        border-top: 1px solid #2d3748;
      }

      .pagination-btn {
        background-color: #2d3748;
        border: 1px solid #4a5568;
        color: #e6e6e6;
        padding: 0.5rem 1rem;
        border-radius: 8px;
        cursor: pointer;
        transition: all 0.2s ease;
      }

      .pagination-btn:hover:not(:disabled) {
        background-color: #4a5568;
      }

      .pagination-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .pagination-pages {
        display: flex;
        gap: 0.5rem;
      }

      .pagination-page {
        background-color: #2d3748;
        border: 1px solid #4a5568;
        color: #e6e6e6;
        padding: 0.5rem 0.75rem;
        border-radius: 8px;
        cursor: pointer;
        transition: all 0.2s ease;
        min-width: 40px;
      }

      .pagination-page:hover {
        background-color: #4a5568;
      }

      .pagination-page.active {
        background-color: #f2c037;
        color: #0f1115;
        border-color: #f2c037;
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
        .filters-section {
          flex-direction: column;
          gap: 1rem;
        }

        .filter-select {
          min-width: auto;
        }

        .table-header {
          flex-direction: column;
          gap: 1rem;
          align-items: flex-start;
        }

        .table-stats {
          flex-direction: column;
          gap: 0.5rem;
        }

        .leaderboard-table th,
        .leaderboard-table td {
          padding: 0.75rem 0.5rem;
        }

        .player-cell {
          min-width: 150px;
        }

        .pagination {
          flex-wrap: wrap;
        }
      }
    `,
  ],
})
export class LeaderboardComponent implements OnInit {
  private readonly leaderboardService = inject(LeaderboardService);
  private readonly auth = inject(AuthService);

  leaderboardEntries = signal<LeaderboardEntry[]>([]);
  myRank = signal<UserRankResponse | null>(null);
  totalPlayers = signal(0);
  totalEntries = signal(0);
  totalPages = signal(0);
  currentPage = signal(1);

  selectedType: LeaderboardType = 'Overall';
  selectedLanguage = '';
  pageSize = 20;

  ngOnInit() {
    this.loadLeaderboard();
    this.loadMyRank();
  }

  loadLeaderboard() {
    const request = {
      type: this.selectedType,
      page: this.currentPage(),
      pageSize: this.pageSize,
      language: this.selectedLanguage || undefined,
    };

    this.leaderboardService.getLeaderboard(request).subscribe((response) => {
      this.leaderboardEntries.set(response.entries);
      this.totalPlayers.set(response.totalCount);
      this.totalEntries.set(response.totalCount);
      this.totalPages.set(response.totalPages);
    });
  }

  loadMyRank() {
    this.leaderboardService
      .getMyRank(this.selectedType, this.selectedLanguage || undefined)
      .subscribe((rank) => {
        this.myRank.set(rank);
      });
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadLeaderboard();
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const total = this.totalPages();
    const current = this.currentPage();
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  getTableTitle(): string {
    const typeNames: { [key in LeaderboardType]: string } = {
      Overall: 'Overall Leaderboard',
      Weekly: 'Weekly Leaderboard',
      Monthly: 'Monthly Leaderboard',
      AllTime: 'All-Time Leaderboard',
      Language: 'Language Leaderboard',
      Badges: 'Badges Leaderboard',
      WinRate: 'Win Rate Leaderboard',
      Experience: 'Experience Leaderboard',
    };
    return typeNames[this.selectedType];
  }

  isCurrentUser(userId: string): boolean {
    return this.auth.currentUser()?.id === userId;
  }

  getMedalIcon(rank: number): string {
    const medals = ['🥇', '🥈', '🥉'];
    return medals[rank] || '';
  }

  formatNumber(value: number): string {
    return value.toLocaleString();
  }

  formatPercentage(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  }
}
