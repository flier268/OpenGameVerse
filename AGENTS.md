# Repository Guidelines

## Project Structure & Module Organization

- `src/OpenGameVerse.Core/` holds domain models, abstractions, and JSON source generation.
- `src/OpenGameVerse.Data/` contains SQLite access and Dapper.AOT repositories plus migrations.
- `src/OpenGameVerse.Platform.Windows/` and `src/OpenGameVerse.Platform.Linux/` implement platform-specific scanning and host logic.
- `src/OpenGameVerse.Console/` is the CLI entry point with manual dependency injection.
- `tests/` is reserved for unit tests (Phase 2); it is currently empty.

## Build, Test, and Development Commands

- `dotnet build` builds the solution for development.
- `dotnet run --project src/OpenGameVerse.Console/OpenGameVerse.Console.csproj -- scan` runs the CLI scanner from source.
- `dotnet publish -c Release src/OpenGameVerse.Console/OpenGameVerse.Console.csproj` produces a release AOT build.
- `dotnet publish -c Release -r win-x64` or `dotnet publish -c Release -r linux-x64` targets a specific runtime.
- `dotnet publish -c Release /p:PublishAot=true` verifies Native AOT compatibility (check for IL2026/IL2087/IL3050 warnings).

## Coding Style & Naming Conventions

- Follow existing C# conventions: `PascalCase` for public types/members, `camelCase` for locals/parameters, and clear, descriptive names (e.g., `SteamScanner`, `GameInstallation`).
- Keep code AOT-safe: no runtime reflection, no dynamic assembly loading, and prefer source generators.
- Use manual DI wiring (no assembly scanning) and keep platform-specific code behind `IPlatformHost` implementations.

## Testing Guidelines

- Automated tests are planned for Phase 2; there are no active test projects yet.
- When adding tests, place them under `tests/` and name classes/files after the component under test (e.g., `SteamScannerTests`).
- Document how to run new test suites in this file or `README.md` once introduced.

## Commit & Pull Request Guidelines

- This directory does not appear to be a Git repository, so no commit message conventions are available. Use concise, imperative messages until guidelines are added.
- PRs should include: a short problem statement, key changes, and the exact commands run (for example, `dotnet publish -c Release /p:PublishAot=true`).
- If a change affects platform behavior, call out Windows/Linux validation explicitly. Include screenshots only for UI changes (Phase 2+).

## AOT & Performance Notes

- Maintain Native AOT constraints from `CLAUDE.md`: source-generated JSON, no reflection, and statically determinable code paths.
- Prefer streaming results (`IAsyncEnumerable`) for scanners and keep cold start and binary size targets in mind.
