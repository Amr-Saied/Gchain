import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { AuthService } from '../../core/services/auth.service';
import {
  GameSessionResponse,
  GameStatus,
  CreateGameRequest,
  AvailableGamesResponse,
  MyGamesResponse,
} from '../../shared/interfaces/games';

@Component({
  selector: 'app-games',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="games-container">
      <!-- Header -->
      <div class="games-header">
        <h1 class="page-title">Games</h1>
        <p class="page-subtitle">Join existing games or create your own</p>
        <button
          class="btn btn-primary btn-large"
          (click)="showCreateGameModal = true"
        >
          <span class="btn-icon">➕</span>
          Create New Game
        </button>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button
          class="tab-btn"
          [class.active]="activeTab === 'available'"
          (click)="setActiveTab('available')"
        >
          Available Games
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab === 'my-games'"
          (click)="setActiveTab('my-games')"
        >
          My Games
        </button>
      </div>

      <!-- Available Games Tab -->
      <div class="tab-content" *ngIf="activeTab === 'available'">
        <!-- Filters -->
        <div class="filters-section">
          <div class="filter-group">
            <label class="filter-label">Status:</label>
            <select
              [(ngModel)]="statusFilter"
              (change)="loadAvailableGames()"
              class="filter-select"
            >
              <option value="">All Status</option>
              <option value="Waiting">Waiting</option>
              <option value="InProgress">In Progress</option>
            </select>
          </div>
          <div class="filter-group">
            <label class="filter-label">Language:</label>
            <select
              [(ngModel)]="languageFilter"
              (change)="loadAvailableGames()"
              class="filter-select"
            >
              <option value="">All Languages</option>
              <option value="English">English</option>
              <option value="Spanish">Spanish</option>
              <option value="French">French</option>
            </select>
          </div>
          <div class="filter-group">
            <label class="filter-label">Search:</label>
            <input
              type="text"
              [(ngModel)]="searchQuery"
              (input)="filterGames()"
              placeholder="Search games..."
              class="filter-input"
            />
          </div>
        </div>

        <!-- Available Games Grid -->
        <div class="games-grid">
          <div
            class="game-card"
            *ngFor="let game of filteredAvailableGames()"
            [class.waiting]="game.status === 'Waiting'"
            [class.in-progress]="game.status === 'InProgress'"
          >
            <div class="game-header">
              <h3 class="game-title">{{ game.name }}</h3>
              <div class="game-status" [class]="game.status.toLowerCase()">
                {{ game.status }}
              </div>
            </div>

            <div class="game-info">
              <div class="info-row">
                <span class="info-label">Language:</span>
                <span class="info-value">{{ game.language }}</span>
              </div>
              <div class="info-row">
                <span class="info-label">Players:</span>
                <span class="info-value"
                  >{{ game.playersCount }}/{{ game.maxPlayers }}</span
                >
              </div>
              <div class="info-row">
                <span class="info-label">Rounds:</span>
                <span class="info-value"
                  >{{ game.currentRound }}/{{ game.maxRounds }}</span
                >
              </div>
              <div class="info-row">
                <span class="info-label">Lives:</span>
                <span class="info-value">{{ game.maxLives }}</span>
              </div>
              <div class="info-row">
                <span class="info-label">Time Limit:</span>
                <span class="info-value">{{ game.turnTimeLimit }}s</span>
              </div>
            </div>

            <div class="game-teams">
              <div class="teams-header">Teams ({{ game.teams.length }})</div>
              <div class="teams-list">
                <div
                  class="team-item"
                  *ngFor="let team of game.teams"
                  [style.border-color]="team.color"
                >
                  <div
                    class="team-color"
                    [style.background-color]="team.color"
                  ></div>
                  <div class="team-name">{{ team.name }}</div>
                  <div class="team-score">{{ team.score }}</div>
                  <div class="team-members">
                    {{ team.members.length }} members
                  </div>
                </div>
              </div>
            </div>

            <div class="game-actions">
              <button
                class="btn btn-primary"
                (click)="joinGame(game.id)"
                [disabled]="
                  game.status !== 'Waiting' ||
                  game.playersCount >= game.maxPlayers
                "
              >
                {{ game.status === 'Waiting' ? 'Join Game' : 'View Game' }}
              </button>
              <a [routerLink]="['/games', game.id]" class="btn btn-secondary">
                View Details
              </a>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div class="empty-state" *ngIf="filteredAvailableGames().length === 0">
          <div class="empty-icon">🎮</div>
          <h3 class="empty-title">No games found</h3>
          <p class="empty-message">
            Try adjusting your filters or create a new game
          </p>
        </div>
      </div>

      <!-- My Games Tab -->
      <div class="tab-content" *ngIf="activeTab === 'my-games'">
        <!-- Active Games -->
        <div
          class="games-section"
          *ngIf="myGames()?.activeGames && myGames()!.activeGames.length > 0"
        >
          <h2 class="section-title">Active Games</h2>
          <div class="games-grid">
            <div
              class="game-card"
              *ngFor="let game of myGames()!.activeGames"
              [class.waiting]="game.status === 'Waiting'"
              [class.in-progress]="game.status === 'InProgress'"
            >
              <div class="game-header">
                <h3 class="game-title">{{ game.name }}</h3>
                <div class="game-status" [class]="game.status.toLowerCase()">
                  {{ game.status }}
                </div>
              </div>

              <div class="game-info">
                <div class="info-row">
                  <span class="info-label">Language:</span>
                  <span class="info-value">{{ game.language }}</span>
                </div>
                <div class="info-row">
                  <span class="info-label">Round:</span>
                  <span class="info-value"
                    >{{ game.currentRound }}/{{ game.maxRounds }}</span
                  >
                </div>
                <div class="info-row">
                  <span class="info-label">Time Remaining:</span>
                  <span class="info-value">{{ game.timeRemaining || 0 }}s</span>
                </div>
              </div>

              <div class="game-actions">
                <a [routerLink]="['/games', game.id]" class="btn btn-primary">
                  {{
                    game.status === 'InProgress' ? 'Continue Game' : 'View Game'
                  }}
                </a>
                <button class="btn btn-danger" (click)="leaveGame(game.id)">
                  Leave Game
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Completed Games -->
        <div
          class="games-section"
          *ngIf="
            myGames()?.completedGames && myGames()!.completedGames.length > 0
          "
        >
          <h2 class="section-title">Completed Games</h2>
          <div class="games-grid">
            <div
              class="game-card completed"
              *ngFor="let game of myGames()!.completedGames"
            >
              <div class="game-header">
                <h3 class="game-title">{{ game.name }}</h3>
                <div class="game-status completed">Completed</div>
              </div>

              <div class="game-info">
                <div class="info-row">
                  <span class="info-label">Language:</span>
                  <span class="info-value">{{ game.language }}</span>
                </div>
                <div class="info-row">
                  <span class="info-label">Final Round:</span>
                  <span class="info-value"
                    >{{ game.currentRound }}/{{ game.maxRounds }}</span
                  >
                </div>
                <div class="info-row">
                  <span class="info-label">Ended:</span>
                  <span class="info-value">{{
                    formatDate(game.endedAt || null)
                  }}</span>
                </div>
              </div>

              <div class="game-actions">
                <a [routerLink]="['/games', game.id]" class="btn btn-secondary">
                  View Results
                </a>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div
          class="empty-state"
          *ngIf="
            !myGames()?.activeGames?.length &&
            !myGames()?.completedGames?.length
          "
        >
          <div class="empty-icon">🎮</div>
          <h3 class="empty-title">No games yet</h3>
          <p class="empty-message">
            Join an existing game or create your own to get started
          </p>
        </div>
      </div>

      <!-- Create Game Modal -->
      <div
        class="modal-overlay"
        *ngIf="showCreateGameModal"
        (click)="closeCreateGameModal()"
      >
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title">Create New Game</h2>
            <button class="modal-close" (click)="closeCreateGameModal()">
              ×
            </button>
          </div>

          <form class="modal-body" (ngSubmit)="createGame()" #gameForm="ngForm">
            <div class="form-group">
              <label class="form-label">Game Name</label>
              <input
                type="text"
                [(ngModel)]="newGame.name"
                name="name"
                required
                class="form-input"
                placeholder="Enter game name"
              />
            </div>

            <div class="form-group">
              <label class="form-label">Language</label>
              <select
                [(ngModel)]="newGame.language"
                name="language"
                required
                class="form-select"
              >
                <option value="English">English</option>
                <option value="Spanish">Spanish</option>
                <option value="French">French</option>
              </select>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Max Lives</label>
                <input
                  type="number"
                  [(ngModel)]="newGame.maxLives"
                  name="maxLives"
                  required
                  min="1"
                  max="10"
                  class="form-input"
                />
              </div>
              <div class="form-group">
                <label class="form-label">Turn Time Limit (seconds)</label>
                <input
                  type="number"
                  [(ngModel)]="newGame.turnTimeLimit"
                  name="turnTimeLimit"
                  required
                  min="10"
                  max="300"
                  class="form-input"
                />
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Rounds to Win</label>
              <input
                type="number"
                [(ngModel)]="newGame.roundsToWin"
                name="roundsToWin"
                required
                min="1"
                max="20"
                class="form-input"
              />
            </div>

            <div class="modal-actions">
              <button
                type="button"
                class="btn btn-secondary"
                (click)="closeCreateGameModal()"
              >
                Cancel
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="!gameForm.valid || isCreating"
              >
                <span *ngIf="isCreating" class="btn-loading">⏳</span>
                {{ isCreating ? 'Creating...' : 'Create Game' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .games-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .games-header {
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
        margin-bottom: 2rem;
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

      .btn-danger {
        background-color: #ef476f;
        color: #ffffff;
      }

      .btn-danger:hover {
        background-color: #e63946;
      }

      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
        transform: none;
      }

      .btn-icon {
        font-size: 1.25rem;
      }

      .btn-loading {
        animation: spin 1s linear infinite;
      }

      .tabs {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 2rem;
        border-bottom: 1px solid #2d3748;
      }

      .tab-btn {
        background: none;
        border: none;
        color: #a0aec0;
        padding: 1rem 1.5rem;
        cursor: pointer;
        font-weight: 500;
        transition: all 0.2s ease;
        border-bottom: 2px solid transparent;
      }

      .tab-btn:hover {
        color: #e6e6e6;
      }

      .tab-btn.active {
        color: #f2c037;
        border-bottom-color: #f2c037;
      }

      .tab-content {
        min-height: 400px;
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

      .filter-select,
      .filter-input {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        font-size: 0.875rem;
        min-width: 150px;
      }

      .filter-input {
        min-width: 200px;
      }

      .filter-select:focus,
      .filter-input:focus {
        outline: none;
        border-color: #f2c037;
      }

      .games-section {
        margin-bottom: 3rem;
      }

      .section-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin-bottom: 1.5rem;
      }

      .games-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
        gap: 1.5rem;
      }

      .game-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 1.5rem;
        transition: all 0.2s ease;
      }

      .game-card:hover {
        transform: translateY(-4px);
        border-color: #4a5568;
      }

      .game-card.waiting {
        border-color: #1ec8a5;
      }

      .game-card.in-progress {
        border-color: #f2c037;
      }

      .game-card.completed {
        border-color: #718096;
        opacity: 0.8;
      }

      .game-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1rem;
      }

      .game-title {
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0;
      }

      .game-status {
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-size: 0.75rem;
        font-weight: 500;
        text-transform: uppercase;
      }

      .game-status.waiting {
        background-color: rgba(30, 200, 165, 0.2);
        color: #1ec8a5;
      }

      .game-status.inprogress {
        background-color: rgba(242, 192, 55, 0.2);
        color: #f2c037;
      }

      .game-status.completed {
        background-color: rgba(113, 128, 150, 0.2);
        color: #718096;
      }

      .game-info {
        margin-bottom: 1rem;
      }

      .info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 0.5rem;
      }

      .info-label {
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .info-value {
        color: #e6e6e6;
        font-weight: 500;
        font-size: 0.875rem;
      }

      .game-teams {
        margin-bottom: 1rem;
      }

      .teams-header {
        color: #e6e6e6;
        font-weight: 500;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
      }

      .teams-list {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .team-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem;
        background-color: #0f1115;
        border-radius: 8px;
        border: 1px solid #2d3748;
      }

      .team-color {
        width: 12px;
        height: 12px;
        border-radius: 50%;
      }

      .team-name {
        flex: 1;
        color: #e6e6e6;
        font-weight: 500;
        font-size: 0.875rem;
      }

      .team-score {
        color: #f2c037;
        font-weight: 600;
        font-size: 0.875rem;
      }

      .team-members {
        color: #a0aec0;
        font-size: 0.75rem;
      }

      .game-actions {
        display: flex;
        gap: 0.75rem;
      }

      .game-actions .btn {
        flex: 1;
        justify-content: center;
        font-size: 0.875rem;
        padding: 0.5rem 1rem;
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

      .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(0, 0, 0, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
      }

      .modal-content {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        width: 90%;
        max-width: 500px;
        max-height: 90vh;
        overflow-y: auto;
      }

      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.5rem 2rem;
        border-bottom: 1px solid #2d3748;
      }

      .modal-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0;
      }

      .modal-close {
        background: none;
        border: none;
        color: #a0aec0;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0;
        width: 30px;
        height: 30px;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .modal-close:hover {
        color: #e6e6e6;
      }

      .modal-body {
        padding: 2rem;
      }

      .form-group {
        margin-bottom: 1.5rem;
      }

      .form-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }

      .form-label {
        display: block;
        color: #e6e6e6;
        font-weight: 500;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
      }

      .form-input,
      .form-select {
        width: 100%;
        background-color: #0f1115;
        border: 1px solid #2d3748;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        font-size: 0.875rem;
      }

      .form-input:focus,
      .form-select:focus {
        outline: none;
        border-color: #f2c037;
      }

      .modal-actions {
        display: flex;
        gap: 1rem;
        justify-content: flex-end;
        margin-top: 2rem;
      }

      .modal-actions .btn {
        min-width: 100px;
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
        .games-grid {
          grid-template-columns: 1fr;
        }

        .filters-section {
          flex-direction: column;
          gap: 1rem;
        }

        .filter-select,
        .filter-input {
          min-width: auto;
        }

        .form-row {
          grid-template-columns: 1fr;
        }

        .game-actions {
          flex-direction: column;
        }

        .modal-content {
          width: 95%;
          margin: 1rem;
        }

        .modal-body {
          padding: 1.5rem;
        }
      }
    `,
  ],
})
export class GamesComponent implements OnInit {
  private readonly gameService = inject(GameService);
  private readonly auth = inject(AuthService);

  availableGames = signal<GameSessionResponse[]>([]);
  myGames = signal<MyGamesResponse | null>(null);
  filteredAvailableGames = signal<GameSessionResponse[]>([]);

  activeTab = 'available';
  statusFilter = '';
  languageFilter = '';
  searchQuery = '';

  showCreateGameModal = false;
  isCreating = false;

  newGame: CreateGameRequest = {
    name: '',
    language: 'English',
    maxLives: 3,
    turnTimeLimit: 30,
    roundsToWin: 5,
  };

  ngOnInit() {
    this.loadAvailableGames();
    this.loadMyGames();
  }

  setActiveTab(tab: string) {
    this.activeTab = tab;
    if (tab === 'my-games') {
      this.loadMyGames();
    }
  }

  loadAvailableGames() {
    this.gameService.getAvailableGames().subscribe((response) => {
      this.availableGames.set(response.games);
      this.filterGames();
    });
  }

  loadMyGames() {
    this.gameService.getMyGames().subscribe((games) => {
      this.myGames.set(games);
    });
  }

  filterGames() {
    let filtered = this.availableGames();

    if (this.statusFilter) {
      filtered = filtered.filter((game) => game.status === this.statusFilter);
    }

    if (this.languageFilter) {
      filtered = filtered.filter(
        (game) => game.language === this.languageFilter
      );
    }

    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter((game) =>
        game.name.toLowerCase().includes(query)
      );
    }

    this.filteredAvailableGames.set(filtered);
  }

  joinGame(gameId: number) {
    // For now, we'll need to implement team selection
    // This is a temporary fix - the UI should show team selection
    const game = this.availableGames().find((g) => g.id === gameId);
    if (game && game.teams.length > 0) {
      const firstTeamId = game.teams[0].id;
      this.gameService
        .joinGame({ gameSessionId: gameId, teamId: firstTeamId })
        .subscribe({
          next: (response) => {
            if (response.success) {
              // Redirect to game page
              window.location.href = `/games/${gameId}`;
            }
          },
          error: (error) => {
            console.error('Failed to join game:', error);
          },
        });
    } else {
      console.error('No teams available for this game');
    }
  }

  leaveGame(gameId: number) {
    this.gameService.leaveGame({ gameSessionId: gameId }).subscribe({
      next: (response) => {
        if (response.success) {
          this.loadMyGames();
        }
      },
      error: (error) => {
        console.error('Failed to leave game:', error);
      },
    });
  }

  createGame() {
    this.isCreating = true;
    this.gameService.createGame(this.newGame).subscribe({
      next: (response) => {
        if (response.success) {
          this.closeCreateGameModal();
          this.loadAvailableGames();
          // Redirect to the new game
          window.location.href = `/games/${response.gameSessionId}`;
        }
        this.isCreating = false;
      },
      error: (error) => {
        console.error('Failed to create game:', error);
        this.isCreating = false;
      },
    });
  }

  closeCreateGameModal() {
    this.showCreateGameModal = false;
    this.newGame = {
      name: '',
      language: 'English',
      maxLives: 3,
      turnTimeLimit: 30,
      roundsToWin: 5,
    };
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString();
  }
}
