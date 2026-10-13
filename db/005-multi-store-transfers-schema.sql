-- =============================================================================
-- Longblack — Multi-Store & Stock Transfers Schema
-- Script: 005-multi-store-transfers-schema.sql
--
-- Run order: this script must be run after 001-catalogue-schema.sql,
-- 003-goods-receiving-schema.sql, and 004-stock-take-schema.sql (inventory,
-- inventory_transactions, goods_receipts, stock_takes must already exist).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/005-multi-store-transfers-schema.sql
--
-- This script is idempotent: it uses CREATE TABLE IF NOT EXISTS / ADD COLUMN
-- IF NOT EXISTS / guarded ALTER TABLE blocks, so it is safe to run more than
-- once.
--
-- IMPORTANT — one-time, irreversible data change: existing inventory,
-- inventory_transactions, goods_receipts, and stock_takes rows are backfilled
-- to the single Warehouse store's id below. Review carefully before running
-- against production data. See docs/adr/0003-multi-store-transfers-expand-mvp-scope.md.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- stores
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stores (
    id          uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    name        text        NOT NULL,
    type        text        NOT NULL,
    status      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    created_by  text        NOT NULL,
    updated_by  text        NOT NULL
);

-- Seeded with a fixed, well-known id so this script and the idempotent
-- DatabaseSeeder check at app startup both resolve to the same single row,
-- whichever runs first.
INSERT INTO stores (id, name, type, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0007-000000000001', 'Warehouse', 'Warehouse', 'Active', 'system', 'system')
ON CONFLICT (id) DO NOTHING;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE indexname = 'uq_stores_single_warehouse'
    ) THEN
        -- Partial unique index: defence-in-depth backstop (alongside the application-layer
        -- check) ensuring exactly one Warehouse-type store can ever exist.
        CREATE UNIQUE INDEX uq_stores_single_warehouse ON stores (type) WHERE type = 'Warehouse';
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- inventory: becomes store-scoped — one row per (product_variant_id, store_id)
-- ---------------------------------------------------------------------------
ALTER TABLE inventory ADD COLUMN IF NOT EXISTS store_id uuid REFERENCES stores (id);

UPDATE inventory SET store_id = '00000000-0000-0000-0007-000000000001' WHERE store_id IS NULL;

ALTER TABLE inventory ALTER COLUMN store_id SET NOT NULL;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_inventory_product_variant_id'
    ) THEN
        ALTER TABLE inventory DROP CONSTRAINT uq_inventory_product_variant_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_inventory_product_variant_id_store_id'
    ) THEN
        ALTER TABLE inventory ADD CONSTRAINT uq_inventory_product_variant_id_store_id UNIQUE (product_variant_id, store_id);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_inventory_store_id ON inventory (store_id);

-- ---------------------------------------------------------------------------
-- inventory_transactions: gains store_id (immutable ledger — no backfill
-- ambiguity, every existing row happened at the single pre-multi-store location)
-- ---------------------------------------------------------------------------
ALTER TABLE inventory_transactions ADD COLUMN IF NOT EXISTS store_id uuid REFERENCES stores (id);

UPDATE inventory_transactions SET store_id = '00000000-0000-0000-0007-000000000001' WHERE store_id IS NULL;

ALTER TABLE inventory_transactions ALTER COLUMN store_id SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_inventory_transactions_store_id ON inventory_transactions (store_id);

-- ---------------------------------------------------------------------------
-- goods_receipts: gains store_id, always the Warehouse store (not user-selectable)
-- ---------------------------------------------------------------------------
ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS store_id uuid REFERENCES stores (id);

UPDATE goods_receipts SET store_id = '00000000-0000-0000-0007-000000000001' WHERE store_id IS NULL;

ALTER TABLE goods_receipts ALTER COLUMN store_id SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_goods_receipts_store_id ON goods_receipts (store_id);

-- ---------------------------------------------------------------------------
-- stock_takes: gains store_id, required and user-selectable going forward.
-- Existing stock takes (pre-multi-store) are backfilled to the Warehouse store.
-- ---------------------------------------------------------------------------
ALTER TABLE stock_takes ADD COLUMN IF NOT EXISTS store_id uuid REFERENCES stores (id);

UPDATE stock_takes SET store_id = '00000000-0000-0000-0007-000000000001' WHERE store_id IS NULL;

ALTER TABLE stock_takes ALTER COLUMN store_id SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_stock_takes_store_id ON stock_takes (store_id);

-- ---------------------------------------------------------------------------
-- stock_transfer_seq
-- Backs stock_transfers.sequence_number so transfer numbers (e.g. "TR-0001")
-- are assigned atomically by Postgres.
-- ---------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS stock_transfer_seq
    AS integer
    START WITH 1;

-- ---------------------------------------------------------------------------
-- stock_transfers
--
-- Hub-and-spoke (one of source/destination must be the Warehouse store, and
-- they must differ) is enforced at the application layer, not here.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_transfers (
    id                    uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sequence_number       integer     NOT NULL DEFAULT nextval('stock_transfer_seq'),
    source_store_id       uuid        NOT NULL REFERENCES stores (id),
    destination_store_id  uuid        NOT NULL REFERENCES stores (id),
    status                text        NOT NULL,
    dispatched_date        timestamptz,
    dispatched_by          text,
    received_date          timestamptz,
    received_by            text,
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    created_by             text        NOT NULL,
    updated_by             text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_stock_transfers_sequence_number'
    ) THEN
        ALTER TABLE stock_transfers ADD CONSTRAINT uq_stock_transfers_sequence_number UNIQUE (sequence_number);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_stock_transfers_source_store_id ON stock_transfers (source_store_id);
CREATE INDEX IF NOT EXISTS ix_stock_transfers_destination_store_id ON stock_transfers (destination_store_id);

-- ---------------------------------------------------------------------------
-- stock_transfer_lines
--
-- At most one line per product variant per transfer. received_quantity is
-- nullable specifically to distinguish "not yet received" from "received,
-- quantity confirmed" — never defaulted to dispatched_quantity at insert time.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_transfer_lines (
    id                   uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    stock_transfer_id    uuid        NOT NULL REFERENCES stock_transfers (id),
    product_variant_id   uuid        NOT NULL REFERENCES product_variants (id),
    dispatched_quantity  integer     NOT NULL,
    received_quantity    integer,
    created_at           timestamptz NOT NULL DEFAULT now(),
    updated_at           timestamptz NOT NULL DEFAULT now(),
    created_by           text        NOT NULL,
    updated_by           text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_stock_transfer_lines_transfer_variant'
    ) THEN
        ALTER TABLE stock_transfer_lines
            ADD CONSTRAINT uq_stock_transfer_lines_transfer_variant UNIQUE (stock_transfer_id, product_variant_id);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_stock_transfer_lines_stock_transfer_id ON stock_transfer_lines (stock_transfer_id);
CREATE INDEX IF NOT EXISTS ix_stock_transfer_lines_product_variant_id ON stock_transfer_lines (product_variant_id);
