# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Snake-Poison is a philosophical snake game with a poison system featuring risk/reward mechanics and procedural audio. Three implementations exist:
- **MacApp** (Raylib): Primary desktop version with graphics and audio
- **ConsoleApp**: Terminal-based ASCII version
- **Assets/Scripts**: Unity version using Domain-Driven Design

## Development Commands

### MacApp (Raylib Desktop)
```bash
# Run the game
dotnet run --project MacApp

# Build release (self-contained, single file for macOS ARM64)
dotnet publish MacApp -c Release
```

### ConsoleApp (Terminal)
```bash
# Run the console version
dotnet run --project ConsoleApp
```

### Prerequisites
- .NET 8.0+ (for development)
- MacApp uses Raylib-cs 6.1.1 via NuGet (auto-restored)

## CI/CD

GitHub Actions handles automated builds and releases:

### Workflows
- **ci.yml**: Runs on push/PR to main - builds on Ubuntu, macOS, Windows
- **release.yml**: Triggers on version tags (e.g., `v1.0.0`) - publishes self-contained executables

### Creating a Release
```bash
git tag v1.0.0
git push origin v1.0.0
```

This automatically builds and publishes:
- `SnakePoison-macOS-arm64.zip` (Apple Silicon)
- `SnakePoison-macOS-x64.zip` (Intel Mac)
- `SnakePoison-Windows-x64.zip`

### Local Cross-Platform Build
```bash
# macOS ARM64
dotnet publish MacApp -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# Windows x64
dotnet publish MacApp -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Architecture

### MacApp / ConsoleApp
Self-contained single-file implementations in `Program.cs`. Key components:
- `Game` class: Main loop, state machine (Menu/Playing/Paused/GameOver)
- `Snake` class: Position, movement, poison states, awakening abilities
- `World` class: Grid management, food spawning
- Procedural audio generation: `GenerateAndLoadSound()` creates WAV files at runtime

### Unity Version (DDD Architecture)
Follows Domain-Driven Design with clear layer separation:

```
Assets/Scripts/
├── Domain/           # Core entities, no external dependencies
│   ├── Snake/        # SnakeEntity, SnakeState, SnakeTrajectory
│   ├── Poison/       # PoisonType, PoisonEffect, FoodItem
│   ├── Evolution/    # EvolutionEngine (awakening abilities)
│   └── World/        # WorldGrid
├── Application/      # Game logic, depends on Domain only
│   ├── GameManager.cs
│   └── Events/       # GameEvents
├── Infrastructure/   # Persistence, depends on Domain & Application
│   └── Persistence/  # SaveManager, SaveData
└── Presentation/     # Input handling, depends on Domain & Application
    └── Input/        # InputController
```

Assembly dependencies flow inward: Presentation/Infrastructure → Application → Domain

## Game Mechanics

### Poison Types (Core Game Design)
| Type | Trade-off |
|------|-----------|
| Perception | Vision distortion ↔ See hidden items |
| Impulsive | Control instability ↔ Speed boost |
| Memory | Trail disappears ↔ Reset negative states |
| Evolving | 3-stage mutation → Awakening abilities |

### Awakening Abilities (unlocked via Evolving poison)
- TrueSight, Dash, Rebirth, Phase
