import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/profile/profile.component').then(
        (m) => m.ProfileComponent
      ),
  },
  {
    path: 'badges',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/badges/badges.component').then(
        (m) => m.BadgesComponent
      ),
  },
  {
    path: 'leaderboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/leaderboard/leaderboard.component').then(
        (m) => m.LeaderboardComponent
      ),
  },
  {
    path: 'games',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/games/games.component').then((m) => m.GamesComponent),
  },
  {
    path: 'games/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/games/game-detail.component').then(
        (m) => m.GameDetailComponent
      ),
  },
  {
    path: 'team/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/team/team.component').then((m) => m.TeamComponent),
  },
  { path: '**', redirectTo: '' },
];
