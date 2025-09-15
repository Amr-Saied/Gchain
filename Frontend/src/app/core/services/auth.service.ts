import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import {
  GuestAuthResponse,
  RefreshTokenRequest,
  RefreshTokenResponse,
  GoogleOAuthRequest,
  GoogleOAuthResponse,
} from '../../shared/interfaces/auth';
import { map, tap } from 'rxjs/operators';
import { Observable, of } from 'rxjs';

const ACCESS_TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);

  currentUser = signal<GuestAuthResponse['user'] | null>(null);
  isAuthenticated = signal<boolean>(false);

  constructor() {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    this.isAuthenticated.set(!!token);
  }

  guestLogin(): Observable<GuestAuthResponse> {
    return this.api.post<GuestAuthResponse>('auth/guest/login', {}).pipe(
      tap((res) => {
        localStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
        this.currentUser.set(res.user);
        this.isAuthenticated.set(true);
      })
    );
  }

  googleLogin(payload: GoogleOAuthRequest): Observable<GoogleOAuthResponse> {
    return this.api
      .post<GoogleOAuthResponse>('auth/google/login', payload)
      .pipe(
        tap((res) => {
          localStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
          localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
          // Google user shape differs but both have name/email/picture; keep as unknown
          // and let consuming code handle fields if needed.
          this.isAuthenticated.set(true);
        })
      );
  }

  refreshToken(): Observable<RefreshTokenResponse | null> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) return of(null);
    const body: RefreshTokenRequest = { refreshToken };
    return this.api.post<RefreshTokenResponse>('auth/guest/refresh', body).pipe(
      tap((res) => {
        localStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
        this.isAuthenticated.set(true);
      })
    );
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  logout() {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }
}
