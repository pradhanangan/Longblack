-- =============================================================================
-- Longblack — Stock Take Schema
-- Script: 004-stock-take-schema.sql
--
-- Run order: this script must be run after 001-catalogue-schema.sql
-- (brands, categories, product_variants must already exist).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/004-stock-take-schema.sql
--
-- This script is idempotent: it uses CREATE TABLE IF NOT EXISTS / CREATE
-- SEQUENCE IF NOT EXISTS so it is safe to run more than once. Constraints are
-- added inline on first creation only.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- stock_take_seq
-- Backs stock_takes.sequence_number so reference numbers (e.g. "ST-0001") are
-- assigned atomically by Postgres.
-- ---------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS stock_take_seq
    AS integer
    START WITH 1;

-- ---------------------------------------------------------------------------
-- stock_takes
--
-- brand_id/category_id are the scope filter used to generate stock_take_items
-- when the Stock Take starts (Draft -> InProgress) — both nullable, and both
-- null means the whole catalogue. They are kept afterwards purely as
-- descriptive metadata; stock_take_items is the source of truth for what's
-- actually in scope once counting has started.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_takes (
    id               uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sequence_number  integer     NOT NULL DEFAULT nextval('stock_take_seq'),
    brand_id         uuid        REFERENCES brands (id),
    category_id      uuid        REFERENCES categories (id),
    status           text        NOT NULL,
    start_date       timestamptz,
    completion_date  timestamptz,
    completed_by     text,
    approved_date    timestamptz,
    approved_by      text,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    created_by       text        NOT NULL,
    updated_by       text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_stock_takes_sequence_number'
    ) THEN
        ALTER TABLE stock_takes ADD CONSTRAINT uq_stock_takes_sequence_number UNIQUE (sequence_number);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- stock_take_items
--
-- One row per product variant in scope, generated only when the Stock Take
-- starts (Draft -> InProgress) — never at creation. expected_quantity is a
-- one-time snapshot of Inventory.quantity taken at that moment.
-- counted_quantity mirrors the item's latest stock_take_counts row.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_take_items (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    stock_take_id       uuid        NOT NULL REFERENCES stock_takes (id),
    product_variant_id  uuid        NOT NULL REFERENCES product_variants (id),
    expected_quantity   integer     NOT NULL,
    counted_quantity    integer,
    status              text        NOT NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    created_by          text        NOT NULL,
    updated_by          text        NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_stock_take_items_stock_take_id
    ON stock_take_items (stock_take_id);

CREATE INDEX IF NOT EXISTS ix_stock_take_items_product_variant_id
    ON stock_take_items (product_variant_id);

-- ---------------------------------------------------------------------------
-- stock_take_counts
--
-- Immutable audit rows — never edited or deleted. Recounting always appends a
-- new row; the item's counted_quantity always mirrors the latest one here.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_take_counts (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    stock_take_item_id  uuid        NOT NULL REFERENCES stock_take_items (id),
    quantity            integer     NOT NULL,
    counted_at          timestamptz NOT NULL DEFAULT now(),
    counted_by          text        NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_stock_take_counts_stock_take_item_id
    ON stock_take_counts (stock_take_item_id);
