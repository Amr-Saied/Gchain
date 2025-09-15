# Gchain Frontend Backbone Plan

## 1) Backend capabilities to integrate

- Auth
  - Guest: POST /api/auth/guest/login, POST /api/auth/guest/refresh
  - Google: POST /api/auth/google/login with { token, tokenType: 'id_token'|'code' }
- User
  - GET /api/user/profile, PUT /api/user/profile (multipart)
- Badges
  - GET /api/badge (list), GET /api/badge/{id}, GET by type
  - GET user badges, stats, progress
  - Admin: POST /api/badge, PUT /api/badge/{id}, DELETE /api/badge/{id}, award, check eligibility
- Leaderboard
  - GET /api/leaderboard (+filters), GET /api/leaderboard/user/{userId}/rank, GET /api/leaderboard/my-rank, GET /api/leaderboard/stats, GET /api/leaderboard/top, GET /api/leaderboard/my-rank/calculate
- Game + Team
  - Game: create, available, get session, join team, leave, start, end, my-games, can-join
  - Team: get team, members, can-join
- Realtime (SignalR GameHub)
  - Client → Server: JoinGameSession, LeaveGameSession, SubmitWordGuess, RequestTeamRevival
  - Server → Client: PlayerJoined, PlayerLeft, WordGuessSubmitted, TeamRevivalProcessed, GameUpdate, GameStateChange, Error

## 2) Technology, structure, and theme

- Angular 18 standalone + Signals, Router, HttpClient, RxJS
- Realtime: @microsoft/signalr with accessTokenFactory
- Project structure
  - core/: interceptors, guards, services (auth, api, user, badges, leaderboard, game, team, signalr, google-auth)
  - shared/: interfaces from DTOs, ui primitives
  - features/: auth, dashboard, games, teams, leaderboard, badges, notifications, profile, chat
  - state/: signal stores (authStore, gameStore, badgeStore)
  - environments/: apiBaseUrl, signalrHubUrl, googleClientId
- Theme
  - Base: dark (#0f1115 background, #171a21 surface, #e6e6e6 text)
  - Accent: dark yellow primary (#f2c037; hover #e0b22f)
  - Secondary: teal #1ec8a5; Danger: #ef476f
  - Typography: Inter/Roboto; 14–16px base; 600 headings
  - Components: AppShell (navbar + side rail), PageHeader, Card, Button, Input, Select, Badge/Pill, Table, Modal, Toast, Skeleton

## 3) API integration patterns

- Http layer: ApiService wraps base URL; AuthInterceptor adds Bearer; Refresh on 401 via refresh endpoint
- Auth flows
  - Guest: one click
  - Google: Google Identity Services (GIS) button → id_token → POST /api/auth/google/login
  - Tokens in localStorage; guard checks + optional refresh
- Badges: list/detail/user data, admin CRUD if role=Admin
- Leaderboard: list with filters, my rank, stats
- Game/Team: REST actions + Realtime updates via SignalR
- Forms: profile multipart; inline validation errors

## 4) UI templates (pages)

- AppShell: top navbar, optional side rail, content with router-outlet
- Login: two options (Guest button, GIS-rendered Google button)
- Dashboard: My Games, My Rank, Recent Badges, Notifications
- Games
  - Available list, My games
  - Session view: teams columns, live feed, guess input, countdown, right-side chat
- Badges: grid with filters, progress rings, details drawer
- Leaderboard: sortable table, highlight current user row, stats cards
- Notifications: list and toasts
- Chat: team chat per session (SignalR), different colors for self vs others

## 5) Implementation phases

1. AppShell + Theme (dark + dark-yellow), base components, routing skeleton — DONE
2. Auth: GIS Google button + Guest login — DONE; interceptor + guard + refresh — IN PROGRESS (refresh added)
3. Realtime foundation: SignalR service and basic join/leave events — DONE (SignalR service created)
4. Feature pages MVP: Games (available, session), Badges (list), Leaderboard (list), Profile (view) — PARTIAL (badges/leaderboard/profile basics)
5. Enhanced UX: loaders, toasts, tables, filters, pagination; chat panel — TODO
6. Admin badge CRUD and award flows (optional) — TODO
7. Polish & a11y: focus states, reduced motion, responsive refinements — TODO

## 6) Config

- environment.ts
  - apiBaseUrl: https://localhost:7235/api — SET
  - gameHubUrl: https://localhost:7235/gamehub — SET
  - chatHubUrl: https://localhost:7235/chathub — SET
  - googleClientId — SET

## 7) Done/Next

- Keep this doc as the reference. Each phase will be implemented and tested incrementally.
