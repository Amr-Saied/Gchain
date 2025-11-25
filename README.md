# Gchain

A multiplayer word association game platform built with ASP.NET Core and Angular.

## Project Status

**⚠️ PROJECT POSTPONED**

This project is currently postponed. 

## Overview

Gchain is a real-time multiplayer word association game where players form teams and compete by creating chains of semantically related words. The platform features real-time gameplay, leaderboards, badges, notifications, and both guest and Google OAuth authentication.

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Database**: SQL Server with Entity Framework Core
- **Caching**: Redis (StackExchange.Redis)
- **Real-time Communication**: SignalR
- **Authentication**: JWT Bearer tokens, Google OAuth, Guest authentication
- **AI/ML**: Hugging Face API integration for semantic similarity
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **Framework**: Angular 18
- **Real-time**: SignalR client
- **Language**: TypeScript

## Project Structure

```
Gchain/
├── Backend/
│   └── Gchain/
│       ├── Controllers/      # API controllers
│       ├── Services/         # Business logic services
│       ├── Hubs/            # SignalR hubs (GameHub, ChatHub)
│       ├── Models/          # Data models
│       ├── DTOS/            # Data transfer objects
│       ├── Interfaces/      # Service interfaces
│       ├── Data/            # Database context
│       └── Migrations/      # EF Core migrations
├── Frontend/
│   └── src/
│       └── app/             # Angular application
└── Gchain.sln              # Solution file
```

## Key Features

- **Multiplayer Game Sessions**: Create and join game sessions with team-based gameplay
- **Real-time Gameplay**: Live updates via SignalR for game state, turns, and chat
- **Authentication**: Support for Google OAuth and guest accounts
- **Leaderboards**: Track player rankings and statistics
- **Badge System**: Achievement system with various badges
- **Notifications**: In-app notification system
- **Semantic Word Matching**: AI-powered word similarity checking via Hugging Face
- **Rate Limiting**: API rate limiting using Redis
- **Turn Timers**: Real-time turn management with timers

## Documentation

Additional documentation can be found in:
- `Backend/Gchain/DOCS/` - Technical documentation and guides
- `Frontend/Front_DOCS/` - Frontend planning documents



