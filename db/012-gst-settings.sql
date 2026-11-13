-- =============================================================================
-- Longblack — GST Settings and Sale GST
-- Script: 012-gst-settings.sql
--
-- Run order: after 009-sales-schema.sql (sales, sale_lines).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/012-gst-settings.sql
--
-- Idempotent: safe to run more than once.
--
-- Adds:
--   * app_settings      — single row holding the Admin-editable GST rate
--   * sales.gst_rate    — rate snapshotted when a Sale is completed
--   * sale_lines.gst_amount — GST included in the discounted line total, snapshotted on completion
-- Existing Completed/Voided Sales are backfilled at 15%.
-- =============================================================================

CREATE TABLE IF NOT EXISTS app_settings (
    id          integer       NOT NULL PRIMARY KEY CHECK (id = 1),
    gst_rate    numeric(5,4)  NOT NULL CHECK (gst_rate >= 0 AND gst_rate <= 1),
    updated_at  timestamptz   NOT NULL DEFAULT now(),
    updated_by  text          NOT NULL
);

INSERT INTO app_settings (id, gst_rate, updated_by)
VALUES (1, 0.15, 'system')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE sales ADD COLUMN IF NOT EXISTS gst_rate numeric(5,4);
ALTER TABLE sale_lines ADD COLUMN IF NOT EXISTS gst_amount numeric(10,2);

-- Backfill: GST = discounted line total * rate / (1 + rate), rounded to 2 decimals (half away from zero).
UPDATE sale_lines l
SET gst_amount = round((l.quantity * l.unit_price - l.discount) * 0.15 / 1.15, 2)
FROM sales s
WHERE s.id = l.sale_id
  AND s.status IN ('Completed', 'Voided')
  AND l.gst_amount IS NULL;

UPDATE sales
SET gst_rate = 0.15
WHERE status IN ('Completed', 'Voided')
  AND gst_rate IS NULL;
