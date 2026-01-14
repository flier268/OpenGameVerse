-- Migration 4: Add platform_id for storing app IDs

ALTER TABLE games ADD COLUMN platform_id TEXT;

-- Update schema version
INSERT OR IGNORE INTO schema_version (version, applied_at) VALUES (4, unixepoch());
