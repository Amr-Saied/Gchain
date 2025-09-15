import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { SignalRService } from '../../core/services/signalr.service';
import { GameService } from '../../core/services/game.service';

@Component({
  selector: 'app-game-session',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>Game Session #{{ gameId }}</h2>
      <div class="card" *ngIf="session(); else loading">
        <pre style="white-space:pre-wrap">{{ session() | json }}</pre>
      </div>
      <ng-template #loading>Loading...</ng-template>
      <div class="card" style="margin-top:1rem">
        <h3>Live Events</h3>
        <div *ngFor="let e of events()" class="muted">{{ e }}</div>
      </div>
    </div>
  `,
  styles: [
    `
      .muted {
        color: var(--color-muted);
      }
    `,
  ],
})
export class SessionComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly hub = inject(SignalRService);
  private readonly game = inject(GameService);

  gameId = Number(this.route.snapshot.paramMap.get('id'));
  session = signal<any | null>(null);
  events = signal<string[]>([]);

  constructor() {
    this.game
      .getGameSession(this.gameId)
      .subscribe((res) => this.session.set(res));
    this.hub.joinGameSession(this.gameId);
    this.hub.playerJoined$.subscribe((e) =>
      this.push(`PlayerJoined: ${e.userId}`)
    );
    this.hub.playerLeft$.subscribe((e) => this.push(`PlayerLeft: ${e.userId}`));
    this.hub.wordGuessSubmitted$.subscribe((e) =>
      this.push(`Guess: ${e.word} by ${e.userId}`)
    );
    this.hub.gameUpdate$.subscribe((e) => this.push(`Update: ${e.message}`));
    this.hub.gameStateChange$.subscribe(() => this.push('Game state changed'));
    this.hub.error$.subscribe((msg) => this.push(`Error: ${msg}`));
  }

  private push(line: string) {
    this.events.set([line, ...this.events()]);
  }
}
