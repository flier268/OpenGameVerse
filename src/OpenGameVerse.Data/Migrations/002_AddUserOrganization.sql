-- Migration 2: Add user organization columns (Phase 6)
-- Adds support for favorites and custom categories

-- Add new columns to games table
ALTER TABLE games ADD COLUMN is_favorite INTEGER NOT NULL DEFAULT 0;
ALTER TABLE games ADD COLUMN custom_category TEXT;
ALTER TABLE games ADD COLUMN sort_order INTEGER NOT NULL DEFAULT 0;

-- Add indexes for the new columns
CREATE INDEX IF NOT EXISTS idx_games_is_favorite ON games(is_favorite);
CREATE INDEX IF NOT EXISTS idx_games_custom_category ON games(custom_category);

-- Update schema version
INSERT OR IGNORE INTO schema_version (version, applied_at) VALUES (2, unixepoch());
