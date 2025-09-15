import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LoadingService } from '../services/loading.service';

@Component({
  selector: 'app-app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell">
      <!-- Top Navbar -->
      <header class="navbar">
        <div class="navbar-container">
          <div class="navbar-brand">
            <div class="brand-logo">
              <span class="logo-icon">G</span>
            </div>
            <span class="brand-text">Gchain</span>
          </div>

          <nav class="navbar-nav" *ngIf="auth.isAuthenticated()">
            <a
              routerLink="/"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              class="nav-link"
            >
              <span class="nav-icon">🏠</span>
              Home
            </a>
            <a routerLink="/games" routerLinkActive="active" class="nav-link">
              <span class="nav-icon">🎮</span>
              Games
            </a>
            <a
              routerLink="/leaderboard"
              routerLinkActive="active"
              class="nav-link"
            >
              <span class="nav-icon">🏆</span>
              Leaderboard
            </a>
            <a routerLink="/badges" routerLinkActive="active" class="nav-link">
              <span class="nav-icon">🏅</span>
              Badges
            </a>
          </nav>

          <div class="navbar-actions">
            <div
              *ngIf="auth.isAuthenticated(); else loginButton"
              class="user-menu"
            >
              <div class="user-info">
                <img
                  [src]="
                    auth.currentUser()?.picture || '/assets/default-avatar.png'
                  "
                  [alt]="auth.currentUser()?.name || 'User'"
                  class="user-avatar"
                />
                <span class="user-name">{{
                  auth.currentUser()?.name || 'User'
                }}</span>
              </div>
              <div class="user-dropdown">
                <a routerLink="/profile" class="dropdown-item">
                  <span class="dropdown-icon">👤</span>
                  Profile
                </a>
                <button (click)="logout()" class="dropdown-item">
                  <span class="dropdown-icon">🚪</span>
                  Logout
                </button>
              </div>
            </div>
            <ng-template #loginButton>
              <a routerLink="/login" class="btn btn-primary">Login</a>
            </ng-template>
          </div>
        </div>
      </header>

      <!-- Main Content -->
      <main class="main-content">
        <router-outlet />
      </main>

      <!-- Loading Overlay -->
      <div *ngIf="loading.isLoading()" class="loading-overlay">
        <div class="loading-spinner">
          <div class="spinner"></div>
          <span class="loading-text">Loading...</span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .app-shell {
        min-height: 100vh;
        background-color: #0f1115;
        color: #e6e6e6;
        font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI',
          Roboto, sans-serif;
      }

      .navbar {
        background-color: #171a21;
        border-bottom: 1px solid #2d3748;
        padding: 0 1rem;
        position: sticky;
        top: 0;
        z-index: 1000;
      }

      .navbar-container {
        max-width: 1200px;
        margin: 0 auto;
        display: flex;
        align-items: center;
        justify-content: space-between;
        height: 64px;
      }

      .navbar-brand {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        font-weight: 600;
        font-size: 1.25rem;
      }

      .brand-logo {
        width: 32px;
        height: 32px;
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #0f1115;
        font-weight: 700;
        font-size: 1.125rem;
      }

      .brand-text {
        color: #e6e6e6;
      }

      .navbar-nav {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .nav-link {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        border-radius: 8px;
        color: #a0aec0;
        text-decoration: none;
        font-weight: 500;
        transition: all 0.2s ease;
      }

      .nav-link:hover {
        background-color: #2d3748;
        color: #e6e6e6;
      }

      .nav-link.active {
        background-color: #f2c037;
        color: #0f1115;
      }

      .nav-icon {
        font-size: 1rem;
      }

      .navbar-actions {
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .btn {
        padding: 0.5rem 1rem;
        border-radius: 8px;
        font-weight: 500;
        text-decoration: none;
        transition: all 0.2s ease;
        border: none;
        cursor: pointer;
        font-size: 0.875rem;
      }

      .btn-primary {
        background-color: #f2c037;
        color: #0f1115;
      }

      .btn-primary:hover {
        background-color: #e0b22f;
      }

      .user-menu {
        position: relative;
      }

      .user-info {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem;
        border-radius: 8px;
        cursor: pointer;
        transition: background-color 0.2s ease;
      }

      .user-info:hover {
        background-color: #2d3748;
      }

      .user-avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        object-fit: cover;
      }

      .user-name {
        font-weight: 500;
        color: #e6e6e6;
      }

      .user-dropdown {
        position: absolute;
        top: 100%;
        right: 0;
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 8px;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
        min-width: 160px;
        z-index: 1001;
        display: none;
      }

      .user-menu:hover .user-dropdown {
        display: block;
      }

      .dropdown-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem 1rem;
        color: #e6e6e6;
        text-decoration: none;
        transition: background-color 0.2s ease;
        border: none;
        background: none;
        width: 100%;
        text-align: left;
        cursor: pointer;
      }

      .dropdown-item:hover {
        background-color: #2d3748;
      }

      .dropdown-icon {
        font-size: 1rem;
      }

      .main-content {
        min-height: calc(100vh - 64px);
      }

      .loading-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(15, 17, 21, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
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
        color: #e6e6e6;
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
        .navbar-container {
          padding: 0 0.5rem;
        }

        .navbar-nav {
          display: none;
        }

        .user-name {
          display: none;
        }
      }
    `,
  ],
})
export class AppShellComponent {
  auth = inject(AuthService);
  loading = inject(LoadingService);

  logout() {
    this.auth.logout();
  }
}
