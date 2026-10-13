-- =============================================================================
-- Longblack — Supplier Schema
-- Script: 008-suppliers-schema.sql
--
-- Run order: this script must be run after 003-goods-receiving-schema.sql
-- (goods_receipts must already exist, with its supplier_code column).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/008-suppliers-schema.sql
--
-- This script is idempotent: it uses CREATE TABLE IF NOT EXISTS / ADD COLUMN
-- IF NOT EXISTS / guarded ALTER TABLE blocks, so it is safe to run more than
-- once.
--
-- IMPORTANT — one-time, irreversible data change: every distinct existing
-- goods_receipts.supplier_code value is backfilled into a new Supplier row
-- (code = name = the old free-text value, with PLACEHOLDER email/phone, since
-- no real contact details exist for historical codes). goods_receipts.supplier_id
-- is then populated by matching on that code, and supplier_code is dropped.
-- Review carefully before running against production data, and correct the
-- placeholder Supplier contact details via the UI afterwards.
-- See docs/adr/0004-introduce-supplier-entity.md (supersedes ADR-0001).
-- =============================================================================

-- ---------------------------------------------------------------------------
-- suppliers
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS suppliers (
    id          uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    code        text        NOT NULL,
    name        text        NOT NULL,
    email       text        NOT NULL,
    phone       text        NOT NULL,
    status      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    created_by  text        NOT NULL,
    updated_by  text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_suppliers_code'
    ) THEN
        ALTER TABLE suppliers ADD CONSTRAINT uq_suppliers_code UNIQUE (code);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- Backfill: one Supplier per distinct existing free-text supplier_code.
-- PLACEHOLDER contact details — correct these via the UI after migrating.
-- ---------------------------------------------------------------------------
INSERT INTO suppliers (code, name, email, phone, status, created_by, updated_by)
SELECT DISTINCT
    gr.supplier_code,
    gr.supplier_code,
    'unknown@example.com',
    '0000000000',
    'Active',
    'system',
    'system'
FROM goods_receipts gr
WHERE gr.supplier_code IS NOT NULL
ON CONFLICT (code) DO NOTHING;

-- ---------------------------------------------------------------------------
-- goods_receipts: add supplier_id, backfill by matching the old supplier_code
-- to the Supplier row just created/existing with the same code, then drop the
-- old free-text column.
-- ---------------------------------------------------------------------------
ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS supplier_id uuid REFERENCES suppliers (id);

UPDATE goods_receipts gr
SET supplier_id = s.id
FROM suppliers s
WHERE gr.supplier_id IS NULL
  AND gr.supplier_code IS NOT NULL
  AND s.code = gr.supplier_code;

ALTER TABLE goods_receipts ALTER COLUMN supplier_id SET NOT NULL;

ALTER TABLE goods_receipts DROP COLUMN IF EXISTS supplier_code;

CREATE INDEX IF NOT EXISTS ix_goods_receipts_supplier_id ON goods_receipts (supplier_id);
