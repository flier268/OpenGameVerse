# Repository Guidelines

## Project Structure & Module Organization

- `src/`: production code organized by module.
  - `OpenGameVerse.Core/` domain models, abstractions, JSON source-gen context.
  - `OpenGameVerse.Data/` SQLite repositories and migrations.
  - `OpenGameVerse.Platform.Windows/` and `OpenGameVerse.Platform.Linux/` platform scanners/launchers.
  - `OpenGameVerse.Console/` CLI entry point.
  - `OpenGameVerse.App/` Avalonia UI (desktop/fullscreen).
- `tests/`: xUnit test projects (Core/Data).
- Assets: Avalonia assets live under `src/OpenGameVerse.App/Assets/`.

## Build, Test, and Development Commands

- `dotnet build`: dev build for all projects.
- `dotnet publish -c Release`: Native AOT publish for current platform.
- `dotnet publish -c Release -r win-x64` / `linux-x64`: AOT publish for a target runtime.
- `dotnet publish -c Release /p:PublishAot=true`: AOT compatibility check (required before commits).
- `dotnet run --project src/OpenGameVerse.Console/OpenGameVerse.Console.csproj -- scan`: run CLI scan.
- `dotnet run --project src/OpenGameVerse.App/OpenGameVerse.App.csproj`: run desktop UI.
- `dotnet test`: run all tests.

## Coding Style & Naming Conventions

- C# with nullable reference types enabled and implicit usings.
- Indentation: 4 spaces; braces on new lines (default C# style).
- Naming: `PascalCase` for types/methods, `camelCase` for locals/fields.
- AOT constraints are strict: avoid reflection and runtime scanning; use source generators (JSON, MVVM).

## Testing Guidelines

- Frameworks: xUnit + FluentAssertions + `coverlet.collector`.
- Test names follow `*Tests.cs` and live under `tests/<Project>.Tests/`.
- Run targeted tests with `dotnet test tests/OpenGameVerse.Core.Tests/OpenGameVerse.Core.Tests.csproj`.
- Keep tests AOT-friendly (no reflection-heavy helpers).

## Commit & Pull Request Guidelines

- Commit messages in history are short, sentence-case statements (e.g., “Add category management UI…”). Use concise, imperative phrasing; optional scope prefix is acceptable.
- PRs should include: summary of changes, testing notes (commands + results), and UI screenshots for Avalonia changes.
- If a change affects AOT behavior, mention the `dotnet publish /p:PublishAot=true` result.

## Security & Configuration Tips

- Local database lives under `~/.config/OpenGameVerse/opengameverse.db` on Linux.
- IGDB usage transmits only game titles; avoid adding new telemetry without discussion.
