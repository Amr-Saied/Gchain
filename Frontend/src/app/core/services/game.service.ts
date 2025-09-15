import { inject, Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  GameSessionResponse,
  CreateGameRequest,
  CreateGameResponse,
  JoinGameRequest,
  JoinGameResponse,
  LeaveGameRequest,
  LeaveGameResponse,
  AvailableGamesResponse,
  MyGamesResponse,
  SubmitWordGuessRequest,
  SubmitWordGuessResponse,
  WordGuessResponse,
  RoundResultResponse,
} from '../../shared/interfaces/games';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private readonly api = inject(ApiService);

  /**
   * Get all available games
   */
  getAvailableGames(
    page: number = 1,
    pageSize: number = 20,
    status?: string,
    language?: string
  ): Observable<AvailableGamesResponse> {
    let params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (status) params.append('status', status);
    if (language) params.append('language', language);

    return this.api.get<AvailableGamesResponse>(
      `game/available?${params.toString()}`
    );
  }

  /**
   * Get my games (active and completed)
   */
  getMyGames(): Observable<MyGamesResponse> {
    return this.api.get<MyGamesResponse>('game/my');
  }

  /**
   * Get game by ID
   */
  getGameById(gameId: number): Observable<GameSessionResponse> {
    return this.api.get<GameSessionResponse>(`game/${gameId}`);
  }

  /**
   * Create a new game
   */
  createGame(request: CreateGameRequest): Observable<CreateGameResponse> {
    return this.api.post<CreateGameResponse>('game/create', request);
  }

  /**
   * Join a game
   */
  joinGame(request: JoinGameRequest): Observable<JoinGameResponse> {
    return this.api.post<JoinGameResponse>('game/join', request);
  }

  /**
   * Leave a game
   */
  leaveGame(request: LeaveGameRequest): Observable<LeaveGameResponse> {
    return this.api.post<LeaveGameResponse>('game/leave', request);
  }

  /**
   * Submit a word guess
   */
  submitWordGuess(
    request: SubmitWordGuessRequest
  ): Observable<SubmitWordGuessResponse> {
    return this.api.post<SubmitWordGuessResponse>('game/submit-guess', request);
  }

  /**
   * Get word guesses for a game
   */
  getWordGuesses(
    gameId: number,
    roundNumber?: number
  ): Observable<WordGuessResponse[]> {
    let params = '';
    if (roundNumber) {
      params = `?roundNumber=${roundNumber}`;
    }
    return this.api.get<WordGuessResponse[]>(
      `game/${gameId}/word-guesses${params}`
    );
  }

  /**
   * Get round results for a game
   */
  getRoundResults(gameId: number): Observable<RoundResultResponse[]> {
    return this.api.get<RoundResultResponse[]>(`game/${gameId}/round-results`);
  }

  /**
   * Start a game (host only)
   */
  startGame(gameId: number): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `game/${gameId}/start`,
      {}
    );
  }

  /**
   * End a game (host only)
   */
  endGame(gameId: number): Observable<{ success: boolean; message: string }> {
    return this.api.post<{ success: boolean; message: string }>(
      `game/${gameId}/end`,
      {}
    );
  }

  /**
   * Get game statistics
   */
  getGameStats(gameId: number): Observable<any> {
    return this.api.get<any>(`game/${gameId}/stats`);
  }

  /**
   * Check if user can join a game
   */
  canJoinGame(
    gameId: number
  ): Observable<{ canJoin: boolean; reason?: string }> {
    return this.api.get<{ canJoin: boolean; reason?: string }>(
      `game/${gameId}/can-join`
    );
  }
}
