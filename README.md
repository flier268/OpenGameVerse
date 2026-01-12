# OpenGameVerse

High-performance, cross-platform game library management platform built with .NET 10 Native AOT compilation.

## Status

### Phase 1: ✅ COMPLETE (Console Core)
**Target**: Sub-500ms cold start, <100MB memory footprint
**Achieved**: **16ms cold start**, **3.4MB binary** 🎉

### Phase 2: ✅ COMPLETE (Avalonia Desktop UI)
**Target**: Modern UI, <100MB binary
**Achieved**: **19MB binary**, **Zero warnings** 🎉

## Project Overview

OpenGameVerse is a cross-platform game library manager that automatically detects and catalogs installed games from various platforms (Steam, Epic Games, GOG, etc.). Built with .NET 10 Native AOT for maximum performance and minimal resource usage.

### Key Features

**Phase 1 (Console)**:
- ✅ Cross-platform CLI (Windows & Linux)
- ✅ Steam game detection and cataloging
- ✅ SQLite database with Dapper.AOT
- ✅ Native AOT compilation (no .NET runtime)
- ✅ Sub-20ms cold start, 3.4MB binary

**Phase 2 (Desktop UI)**:
- ✅ Avalonia desktop application
- ✅ Modern Fluent Design interface
- ✅ MVVM with CommunityToolkit.Mvvm
- ✅ Real-time game scanning
- ✅ Grid-based game library view
- ✅ 19MB AOT-compiled binary

## Tech Stack

- **Runtime**: .NET 10 with Native AOT compilation
- **UI Framework**: Console (Avalonia UI in Phase 2)
- **Architecture**: MVVM-ready with abstraction layers
- **Database**: SQLite with Dapper.AOT (compile-time mapping)
- **JSON**: System.Text.Json with source generators
- **VDF Parsing**: Gameloop.Vdf for Steam library files
- **DI**: Manual dependency injection (AOT-compatible)

## Quick Start

### Prerequisites

- .NET 10 SDK

### Build

```bash
# Development build
dotnet build

# Release build with AOT
dotnet publish -c Release src/OpenGameVerse.Console/OpenGameVerse.Console.csproj
```

### Run

**Console Application**:
```bash
# Development
dotnet run --project src/OpenGameVerse.Console/OpenGameVerse.Console.csproj -- scan

# Production
./src/OpenGameVerse.Console/bin/Release/net10.0/linux-x64/publish/OpenGameVerse.Console scan
```

**Desktop Application**:
```bash
# Development
dotnet run --project src/OpenGameVerse.App/OpenGameVerse.App.csproj

# Production
./src/OpenGameVerse.App/bin/Release/net10.0/linux-x64/publish/OpenGameVerse.App
```

### Commands

```bash
# Scan for installed games
OpenGameVerse.Console scan

# List all games in library
OpenGameVerse.Console list

# Show version information
OpenGameVerse.Console version

# Show help
OpenGameVerse.Console help
```

## Project Structure

```
OpenGameVerse/
├── src/
│   ├── OpenGameVerse.Core/          # Models, abstractions, JSON context
│   ├── OpenGameVerse.Data/          # SQLite, Dapper.AOT repositories
│   ├── OpenGameVerse.Platform.Windows/  # Windows scanners & Win32 interop
│   ├── OpenGameVerse.Platform.Linux/    # Linux scanners
│   ├── OpenGameVerse.Console/       # CLI entry point
│   └── OpenGameVerse.App/           # Avalonia desktop application
└── tests/                           # Unit tests (future)
```

## Architecture

### Core Abstractions

- **IPlatformHost**: Platform-specific operations (Windows/Linux)
- **IGameScanner**: Scanner for specific platforms (Steam, Epic, GOG)
- **IGameRepository**: Data access for game library

### Platform Support

**Windows**:
- Steam scanner (Registry + VDF parsing)
- Epic Games scanner (stub)
- GOG Galaxy scanner (stub)

**Linux**:
- Steam scanner (XDG paths + VDF parsing)
- Desktop file scanner (planned)
- Flatpak/Snap support (planned)

## Performance Metrics

| Metric | Target | Achieved |
|--------|--------|----------|
| Cold Start | < 500ms | **16ms** ✅ |
| Binary Size | < 100MB | **3.4MB** ✅ |
| Memory Usage | < 100MB | TBD |
| Build Warnings | 0 | **0** ✅ |
| Build Errors | 0 | **0** ✅ |

## AOT Compatibility

All code follows strict Native AOT requirements:

- ✅ System.Text.Json with source generators (no reflection)
- ✅ Dapper.AOT with compile-time interceptors
- ✅ Manual dependency injection (no assembly scanning)
- ✅ All code paths statically determinable
- ✅ Zero dynamic assembly loading
- ✅ Platform-conditional compilation

## Database Schema

SQLite database stores:
- **games**: Discovered game installations
- **libraries**: Library folder locations
- **platforms**: Platform/launcher metadata

Location: `~/.config/OpenGameVerse/opengameverse.db` (Linux)

## Development

### Adding a New Scanner

1. Implement `IGameScanner` interface
2. Register in appropriate `PlatformHost`
3. Mark with `[SupportedOSPlatform("...")]` attribute
4. Use `IAsyncEnumerable<GameInstallation>` for streaming results

### Building for Specific Platform

```bash
# Windows
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64
```

### Verifying AOT Compatibility

```bash
dotnet publish -c Release /p:PublishAot=true
```

Check for IL2026, IL2087, IL3050 warnings.

## Roadmap

- [x] **Phase 1**: Core Kernel (SQLite, game scanning, AOT foundation) ✅
- [x] **Phase 2**: Avalonia UI with responsive game grid ✅
- [ ] **Phase 3**: IGDB metadata integration, cover art caching
- [x] **Phase 4**: Fullscreen mode with controller support
- [ ] **Phase 5**: Linux optimization (SteamOS, binary size reduction)

## Contributing

See [CLAUDE.md](CLAUDE.md) for detailed development guidelines and AOT constraints.

## License

TBD

---

**Built with ❤️ using .NET 10 Native AOT**
