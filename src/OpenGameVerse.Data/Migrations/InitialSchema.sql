-- OpenGameVerse Initial Schema
-- SQLite database schema for game library management

-- Games table
CREATE TABLE IF NOT EXISTS games (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    normalized_title TEXT NOT NULL,
    install_path TEXT NOT NULL UNIQUE,
    platform TEXT NOT NULL,
    executable_path TEXT,
    icon_path TEXT,
    size_bytes INTEGER NOT NULL DEFAULT 0,
    last_played INTEGER, -- Unix timestamp
    discovered_at INTEGER NOT NULL, -- Unix timestamp
    updated_at INTEGER NOT NULL, -- Unix timestamp

    -- Metadata (Phase 3)
    igdb_id TEXT,
    cover_image_path TEXT
);

CREATE INDEX IF NOT EXISTS idx_games_platform ON games(platform);
CREATE INDEX IF NOT EXISTS idx_games_normalized_title ON games(normalized_title);
CREATE INDEX IF NOT EXISTS idx_games_install_path ON games(install_path);

-- Libraries table (launcher library folders)
CREATE TABLE IF NOT EXISTS libraries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    platform TEXT NOT NULL,
    library_path TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    discovered_at INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_libraries_platform_path ON libraries(platform, library_path);

-- Platforms table (launcher metadata)
CREATE TABLE IF NOT EXISTS platforms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    install_path TEXT,
    is_installed INTEGER NOT NULL DEFAULT 0,
    last_scan INTEGER -- Unix timestamp
);

-- Schema version tracking
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER PRIMARY KEY,
    applied_at INTEGER NOT NULL
);

INSERT OR IGNORE INTO schema_version (version, applied_at) VALUES (1, unixepoch());
