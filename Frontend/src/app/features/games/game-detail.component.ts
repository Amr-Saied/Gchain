import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService } from '../../core/services/signalr.service';
import {
  GameSessionResponse,
  SubmitWordGuessRequest,
  WordGuessResponse,
  RoundResultResponse,
} from '../../shared/interfaces/games';

@Component({
  selector: 'app-game-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="game-detail-container">
      <!-- Game Header -->
      <div class="game-header">
        <div class="game-info">
          <h1 class="game-title">{{ game()?.name || 'Loading...' }}</h1>
          <div class="game-meta">
            <span class="game-status" [class]="game()?.status?.toLowerCase()">
              {{ game()?.status }}
            </span>
            <span class="game-language">{{ game()?.language }}</span>
            <span class="game-round"
              >Round {{ game()?.currentRound }}/{{ game()?.maxRounds }}</span
            >
          </div>
        </div>
        <div class="game-timer" *ngIf="game()?.timeRemaining">
          <div class="timer-circle">
            <span class="timer-text">{{ game()?.timeRemaining }}s</span>
          </div>
        </div>
      </div>

      <!-- Game Content -->
      <div class="game-content" *ngIf="game()">
        <!-- Teams Section -->
        <div class="teams-section">
          <h2 class="section-title">Teams</h2>
          <div class="teams-grid">
            <div
              class="team-card"
              *ngFor="let team of game()!.teams"
              [style.border-color]="team.color"
            >
              <div class="team-header">
                <div
                  class="team-color"
                  [style.background-color]="team.color"
                ></div>
                <h3 class="team-name">{{ team.name }}</h3>
                <div class="team-score">{{ team.score }}</div>
              </div>
              <div class="team-stats">
                <div class="stat">
                  <span class="stat-label">Lives:</span>
                  <span class="stat-value">{{ team.lives }}</span>
                </div>
                <div class="stat">
                  <span class="stat-label">Members:</span>
                  <span class="stat-value">{{ team.members.length }}</span>
                </div>
              </div>
              <div class="team-members">
                <div
                  class="member"
                  *ngFor="let member of team.members"
                  [class.current-player]="
                    member.userId === game()?.currentPlayerId
                  "
                >
                  <img
                    [src]="
                      member.profilePictureUrl || '/assets/default-avatar.png'
                    "
                    [alt]="member.userName"
                    class="member-avatar"
                  />
                  <span class="member-name">{{ member.userName }}</span>
                  <span class="member-role" *ngIf="member.isLeader">👑</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Game Actions -->
        <div
          class="game-actions"
          *ngIf="game()?.status === 'InProgress' && isMyTurn()"
        >
          <div class="word-input-section">
            <h3 class="input-title">Submit Your Word Guess</h3>
            <div class="input-group">
              <input
                type="text"
                [(ngModel)]="wordGuess"
                (keyup.enter)="submitGuess()"
                placeholder="Enter your word..."
                class="word-input"
                [disabled]="isSubmitting"
              />
              <button
                class="btn btn-primary"
                (click)="submitGuess()"
                [disabled]="!wordGuess.trim() || isSubmitting"
              >
                <span *ngIf="isSubmitting" class="btn-loading">⏳</span>
                {{ isSubmitting ? 'Submitting...' : 'Submit' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Word Guesses History -->
        <div class="guesses-section" *ngIf="wordGuesses().length > 0">
          <h2 class="section-title">Recent Guesses</h2>
          <div class="guesses-list">
            <div
              class="guess-item"
              *ngFor="let guess of wordGuesses()"
              [class.correct]="guess.isCorrect"
            >
              <div class="guess-player">
                <img
                  [src]="'/assets/default-avatar.png'"
                  [alt]="guess.userName"
                  class="guess-avatar"
                />
                <span class="guess-player-name">{{ guess.userName }}</span>
              </div>
              <div class="guess-content">
                <span class="guess-word">{{ guess.word }}</span>
                <span class="guess-similarity"
                  >{{ (guess.similarity * 100).toFixed(1) }}%</span
                >
              </div>
              <div class="guess-time">{{ formatTime(guess.submittedAt) }}</div>
            </div>
          </div>
        </div>

        <!-- Round Results -->
        <div class="results-section" *ngIf="roundResults().length > 0">
          <h2 class="section-title">Round Results</h2>
          <div class="results-list">
            <div class="result-item" *ngFor="let result of roundResults()">
              <div class="result-round">Round {{ result.roundNumber }}</div>
              <div class="result-winner">
                <span class="winner-team">{{ result.winningTeamName }}</span>
                <span class="winner-word">{{ result.winningWord }}</span>
                <span class="winner-similarity"
                  >{{ (result.similarity * 100).toFixed(1) }}%</span
                >
              </div>
              <div class="result-time">
                {{ formatTime(result.completedAt) }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="!game()">
        <div class="loading-spinner">
          <div class="spinner"></div>
          <span class="loading-text">Loading game...</span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .game-detail-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 2rem 1rem;
      }

      .game-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 2rem;
        padding: 2rem;
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
      }

      .game-title {
        font-size: 2rem;
        font-weight: 700;
        color: #e6e6e6;
        margin: 0 0 1rem 0;
      }

      .game-meta {
        display: flex;
        gap: 1rem;
        align-items: center;
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

      .game-language,
      .game-round {
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .game-timer {
        display: flex;
        align-items: center;
      }

      .timer-circle {
        width: 80px;
        height: 80px;
        border: 4px solid #f2c037;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: rgba(242, 192, 55, 0.1);
      }

      .timer-text {
        font-size: 1.5rem;
        font-weight: 700;
        color: #f2c037;
      }

      .game-content {
        display: grid;
        gap: 2rem;
      }

      .teams-section,
      .guesses-section,
      .results-section {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 2rem;
      }

      .section-title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0 0 1.5rem 0;
      }

      .teams-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
        gap: 1.5rem;
      }

      .team-card {
        background-color: #0f1115;
        border: 2px solid #2d3748;
        border-radius: 12px;
        padding: 1.5rem;
        transition: transform 0.2s ease;
      }

      .team-card:hover {
        transform: translateY(-2px);
      }

      .team-header {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
      }

      .team-color {
        width: 20px;
        height: 20px;
        border-radius: 50%;
      }

      .team-name {
        flex: 1;
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0;
      }

      .team-score {
        font-size: 1.5rem;
        font-weight: 700;
        color: #f2c037;
      }

      .team-stats {
        display: flex;
        gap: 1rem;
        margin-bottom: 1rem;
      }

      .stat {
        display: flex;
        flex-direction: column;
        align-items: center;
      }

      .stat-label {
        color: #a0aec0;
        font-size: 0.75rem;
        margin-bottom: 0.25rem;
      }

      .stat-value {
        color: #e6e6e6;
        font-weight: 600;
        font-size: 1.125rem;
      }

      .team-members {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .member {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.5rem;
        background-color: #171a21;
        border-radius: 8px;
        transition: background-color 0.2s ease;
      }

      .member.current-player {
        background-color: rgba(242, 192, 55, 0.1);
        border: 1px solid #f2c037;
      }

      .member-avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        object-fit: cover;
      }

      .member-name {
        flex: 1;
        color: #e6e6e6;
        font-weight: 500;
      }

      .member-role {
        font-size: 1rem;
      }

      .game-actions {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 12px;
        padding: 2rem;
      }

      .input-title {
        font-size: 1.25rem;
        font-weight: 600;
        color: #e6e6e6;
        margin: 0 0 1rem 0;
      }

      .input-group {
        display: flex;
        gap: 1rem;
        align-items: center;
      }

      .word-input {
        flex: 1;
        background-color: #0f1115;
        border: 1px solid #2d3748;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        font-size: 1rem;
      }

      .word-input:focus {
        outline: none;
        border-color: #f2c037;
      }

      .btn {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem 1.5rem;
        border-radius: 8px;
        font-weight: 600;
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
      }

      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .btn-loading {
        animation: spin 1s linear infinite;
      }

      .guesses-list,
      .results-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .guess-item,
      .result-item {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 1rem;
        background-color: #0f1115;
        border-radius: 8px;
        border: 1px solid #2d3748;
      }

      .guess-item.correct {
        border-color: #1ec8a5;
        background-color: rgba(30, 200, 165, 0.05);
      }

      .guess-player {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .guess-avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        object-fit: cover;
      }

      .guess-player-name {
        color: #e6e6e6;
        font-weight: 500;
      }

      .guess-content {
        flex: 1;
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .guess-word {
        font-size: 1.125rem;
        font-weight: 600;
        color: #e6e6e6;
      }

      .guess-similarity {
        color: #f2c037;
        font-weight: 600;
      }

      .guess-time {
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .result-round {
        color: #a0aec0;
        font-size: 0.875rem;
        min-width: 80px;
      }

      .result-winner {
        flex: 1;
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .winner-team {
        color: #e6e6e6;
        font-weight: 600;
      }

      .winner-word {
        color: #f2c037;
        font-weight: 600;
      }

      .winner-similarity {
        color: #1ec8a5;
        font-weight: 600;
      }

      .result-time {
        color: #a0aec0;
        font-size: 0.875rem;
      }

      .loading-state {
        display: flex;
        justify-content: center;
        align-items: center;
        min-height: 400px;
      }

      .loading-spinner {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
      }

      .spinner {
        width: 40px;
        height: 40px;
        border: 3px solid #2d3748;
        border-top: 3px solid #f2c037;
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }

      .loading-text {
        color: #a0aec0;
        font-weight: 500;
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
        .game-header {
          flex-direction: column;
          gap: 1rem;
          text-align: center;
        }

        .game-meta {
          flex-wrap: wrap;
          justify-content: center;
        }

        .teams-grid {
          grid-template-columns: 1fr;
        }

        .input-group {
          flex-direction: column;
        }

        .word-input {
          width: 100%;
        }
      }
    `,
  ],
})
export class GameDetailComponent implements OnInit {
  private readonly gameService = inject(GameService);
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  game = signal<GameSessionResponse | null>(null);
  wordGuesses = signal<WordGuessResponse[]>([]);
  roundResults = signal<RoundResultResponse[]>([]);

  wordGuess = '';
  isSubmitting = false;

  ngOnInit() {
    const gameId = this.route.snapshot.paramMap.get('id');
    if (gameId) {
      this.loadGame(parseInt(gameId));
      this.loadWordGuesses(parseInt(gameId));
      this.loadRoundResults(parseInt(gameId));
    }
  }

  loadGame(gameId: number) {
    this.gameService.getGameById(gameId).subscribe((game) => {
      this.game.set(game);
    });
  }

  loadWordGuesses(gameId: number) {
    this.gameService.getWordGuesses(gameId).subscribe((guesses) => {
      this.wordGuesses.set(guesses);
    });
  }

  loadRoundResults(gameId: number) {
    this.gameService.getRoundResults(gameId).subscribe((results) => {
      this.roundResults.set(results);
    });
  }

  submitGuess() {
    if (!this.wordGuess.trim() || !this.game()) return;

    this.isSubmitting = true;
    const request: SubmitWordGuessRequest = {
      gameSessionId: this.game()!.id,
      word: this.wordGuess.trim(),
    };

    this.gameService.submitWordGuess(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.wordGuess = '';
          this.loadGame(this.game()!.id);
          this.loadWordGuesses(this.game()!.id);
        }
        this.isSubmitting = false;
      },
      error: (error) => {
        console.error('Failed to submit guess:', error);
        this.isSubmitting = false;
      },
    });
  }

  isMyTurn(): boolean {
    const currentUser = this.auth.currentUser();
    return currentUser?.id === this.game()?.currentPlayerId;
  }

  formatTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleTimeString();
  }
}
