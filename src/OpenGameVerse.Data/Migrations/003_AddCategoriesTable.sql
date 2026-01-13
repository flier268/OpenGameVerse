-- Migration 3: Add categories table (Phase 6)
-- Adds support for persistent category storage

-- Create categories table
CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    created_at INTEGER NOT NULL,
    updated_at INTEGER NOT NULL
);

-- Insert default Uncategorized category
INSERT OR IGNORE INTO categories (name, created_at, updated_at) VALUES ('Uncategorized', unixepoch(), unixepoch());

-- Add index for category name lookup
CREATE INDEX IF NOT EXISTS idx_categories_name ON categories(name);

-- Update schema version
INSERT OR IGNORE INTO schema_version (version, applied_at) VALUES (3, unixepoch());
