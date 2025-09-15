import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface PlayerEvent {
  userId: string;
  gameSessionId: number;
  timestamp: string;
  reason?: string;
  type?: string;
}

export interface WordGuessEvent {
  userId: string;
  word: string;
  gameSessionId: number;
  timestamp: string;
}

export interface GameUpdateEvent {
  gameSessionId: number;
  message: string;
  data: unknown;
  timestamp: string;
}

export interface GameStateChangeEvent {
  gameSessionId: number;
  gameState: unknown;
  timestamp: string;
  type?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private connection?: signalR.HubConnection;

  // connection state
  isConnected = signal(false);

  // event streams
  playerJoined$ = new Subject<PlayerEvent>();
  playerLeft$ = new Subject<PlayerEvent>();
  wordGuessSubmitted$ = new Subject<WordGuessEvent>();
  teamRevivalProcessed$ = new Subject<{
    teamId: number;
    success: boolean;
    gameSessionId: number;
    timestamp: string;
  }>();
  gameUpdate$ = new Subject<GameUpdateEvent>();
  gameStateChange$ = new Subject<GameStateChangeEvent>();
  error$ = new Subject<string>();

  buildConnection() {
    if (this.connection) return;
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.gameHubUrl, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // register handlers
    this.connection.on('PlayerJoined', (e: PlayerEvent) =>
      this.playerJoined$.next(e)
    );
    this.connection.on('PlayerLeft', (e: PlayerEvent) =>
      this.playerLeft$.next(e)
    );
    this.connection.on('WordGuessSubmitted', (e: WordGuessEvent) =>
      this.wordGuessSubmitted$.next(e)
    );
    this.connection.on('TeamRevivalProcessed', (e: any) =>
      this.teamRevivalProcessed$.next(e)
    );
    this.connection.on('GameUpdate', (e: GameUpdateEvent) =>
      this.gameUpdate$.next(e)
    );
    this.connection.on('GameStateChange', (e: GameStateChangeEvent) =>
      this.gameStateChange$.next(e)
    );
    this.connection.on('Error', (msg: string) => this.error$.next(msg));

    this.connection.onreconnected(() => this.isConnected.set(true));
    this.connection.onclose(() => this.isConnected.set(false));
  }

  async start(): Promise<void> {
    this.buildConnection();
    if (!this.connection) return;
    if (this.connection.state === signalR.HubConnectionState.Connected) return;
    await this.connection.start();
    this.isConnected.set(true);
  }

  async stop(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.isConnected.set(false);
  }

  async joinGameSession(gameSessionId: number): Promise<void> {
    await this.start();
    await this.connection!.invoke('JoinGameSession', gameSessionId);
  }

  async leaveGameSession(gameSessionId: number): Promise<void> {
    if (!this.connection) return;
    await this.connection.invoke('LeaveGameSession', gameSessionId);
  }

  async submitWordGuess(gameSessionId: number, word: string): Promise<void> {
    await this.start();
    await this.connection!.invoke('SubmitWordGuess', gameSessionId, word);
  }

  async requestTeamRevival(
    gameSessionId: number,
    teamId: number
  ): Promise<void> {
    await this.start();
    await this.connection!.invoke('RequestTeamRevival', gameSessionId, teamId);
  }
}
