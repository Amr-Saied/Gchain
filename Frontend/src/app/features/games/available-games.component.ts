import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  GameService,
  AvailableGameItem,
} from '../../core/services/game.service';

@Component({
  selector: 'app-available-games',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page container">
      <h2>Available Games</h2>
      <div class="grid" *ngIf="games().length; else empty">
        <a
          class="card"
          *ngFor="let g of games()"
          [routerLink]="['/games', g.gameSessionId]"
        >
          <h3>{{ g.name || 'Game #' + g.gameSessionId }}</h3>
          <div class="muted">{{ g.language || 'Any' }}</div>
        </a>
      </div>
      <ng-template #empty>
        <div class="card">No open games at the moment.</div>
      </ng-template>
    </div>
  `,
  styles: [
    `
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 1rem;
      }
    `,
    `
      .muted {
        color: var(--color-muted);
      }
    `,
  ],
})
export class AvailableGamesComponent {
  private readonly game = inject(GameService);
  games = signal<AvailableGameItem[]>([]);

  constructor() {
    this.game.getAvailableGames().subscribe((res: any) => {
      const list = (res.games ?? res ?? []) as AvailableGameItem[];
      this.games.set(list);
    });
  }
}
