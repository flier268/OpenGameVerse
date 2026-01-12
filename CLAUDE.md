# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**OpenGameVerse** is a high-performance, cross-platform game library management platform built with .NET 10 Native AOT compilation. The project targets sub-500ms cold start times and <100MB memory footprint by eliminating JIT and managed runtime overhead.

**Core Goals:**
- Single native binary distribution (self-contained, no runtime dependencies)
- Cross-platform support for Windows and Linux
- Native AOT compilation with zero reflection
- Modern UI with both desktop and fullscreen (big picture) modes

## Tech Stack

- **Runtime**: .NET 10 with Native AOT compilation
- **UI Framework**: Avalonia UI (cross-platform with Skia/Vulkan/DirectX rendering)
- **Architecture**: MVVM with CommunityToolkit.Mvvm source generators
- **Database**: SQLite with compile-time entity mapping (Dapper.AOT or equivalent)
- **Metadata API**: IGDB (Twitch) for game data and artwork
- **Image Processing**: ImageSharp for cover art resizing and WebP caching

## Native AOT Constraints

All code MUST be AOT-compatible. This imposes strict requirements:

### Critical Rules
1. **JSON Serialization**: Use ONLY `System.Text.Json` with `JsonSourceGenerationOptions` - all JSON types must be declared at compile time
2. **No Runtime Reflection**: Avoid reflection-based APIs; use source generators instead
3. **Dependency Injection**: Do NOT use runtime assembly scanning - manually wire dependencies or use compile-time DI
4. **No Dynamic Loading**: All code paths must be statically determinable at compile time for tree shaking

### AOT Build Configuration

All `.csproj` files should include:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <StripSymbols>true</StripSymbols>
  <StackTraceSupport>false</StackTraceSupport>
  <InvariantGlobalization>false</InvariantGlobalization>
  <OptimizationPreference>Speed</OptimizationPreference>
  <IlcGenerateCompleteTypeMetadata>false</IlcGenerateCompleteTypeMetadata>
  <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
</PropertyGroup>
```

## System Architecture

### Platform Abstraction Layer

Define `IPlatformHost` interface with OS-specific implementations:

**Windows Implementation:**
- Use Win32 API for installation detection
- ShellExecute for game launching

**Linux Implementation:**
- Parse `/usr/share/applications/*.desktop` files
- Integrate Flatpak and Snap package managers
- Handle Proton/Wine prefix environment variables (WINEPREFIX)

### Module Structure

**Game Discovery Engine:**
- Auto-detect Steam (via `libraryfolders.vdf`), Epic Launcher, and GOG Galaxy
- Use `IAsyncEnumerable` for non-blocking filesystem scanning
- Support hardcoded paths and environment variable resolution

**Metadata Scraper:**
- Local hash/path matching first
- IGDB API integration for detailed game info
- ImageSharp pipeline for cover art: download → resize → WebP cache

**UI Modes:**
- **Desktop Mode**: Fluent Design (Mica/Acrylic effects), virtualized grid for thousands of games
- **Fullscreen Mode**: 4K big-screen optimized with FocusManager, Xbox/PlayStation controller support via Avalonia.Input

## Build Commands

```bash
# Development build
dotnet build

# AOT publish for current platform
dotnet publish -c Release

# AOT publish for specific platform
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64

# Run AOT compatibility checks
dotnet publish -c Release /p:PublishAot=true
```

## Development Workflow

1. **Always verify AOT compatibility** before committing - run publish with PublishAot=true
2. **Use source generators** for any repetitive code generation (JSON, MVVM properties, DB mappings)
3. **Test on clean environment** without .NET runtime to ensure self-contained deployment works
4. **Profile cold start time** - target is <500ms
5. **Monitor binary size** - optimize with tree shaking and symbol stripping

## Development Phases

1. **Phase 1 (Kernel)**: SQLite access and basic game scanning under AOT constraints
2. **Phase 2 (UX Framework)**: Avalonia main window with responsive game list
3. **Phase 3 (Metadata)**: IGDB API integration and local image cache
4. **Phase 4 (Fullscreen)**: Controller input mapping and big-screen navigation
5. **Phase 5 (Polishing)**: Linux distro compatibility (SteamOS/Ubuntu) and binary size optimization

## Platform-Specific Considerations

### Windows
- Target clean Win32 API usage (avoid WPF-style patterns)
- Test without any .NET runtime installed

### Linux
- Test on SteamOS (Steam Deck target platform)
- Validate .desktop file parsing
- Ensure Flatpak/Snap detection works across distros
- Verify Proton environment variable handling

## Critical Performance Targets

- Cold start: <500ms
- Memory footprint: <100MB
- UI responsiveness: 60fps minimum in virtualized grid with 1000+ games
- Binary size: Minimize through aggressive tree shaking
