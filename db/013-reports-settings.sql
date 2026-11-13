-- =============================================================================
-- Longblack — Low Stock Threshold setting (Reports)
-- Script: 013-reports-settings.sql
--
-- Run order: after 012-gst-settings.sql (app_settings).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/013-reports-settings.sql
--
-- Idempotent: safe to run more than once.
--
-- Adds app_settings.low_stock_threshold — the global quantity at or below which a
-- variant is flagged Low Stock in the Stock on Hand report (default 0).
-- =============================================================================

ALTER TABLE app_settings
    ADD COLUMN IF NOT EXISTS low_stock_threshold integer NOT NULL DEFAULT 0 CHECK (low_stock_threshold >= 0);
