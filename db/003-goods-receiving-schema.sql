-- =============================================================================
-- Longblack — Goods Receiving & Inventory Schema
-- Script: 003-goods-receiving-schema.sql
--
-- Run order: this script must be run after 001-catalogue-schema.sql
-- (product_variants must already exist).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/003-goods-receiving-schema.sql
--
-- This script is idempotent: it uses CREATE TABLE IF NOT EXISTS / CREATE
-- SEQUENCE IF NOT EXISTS so it is safe to run more than once. Constraints are
-- added inline on first creation only.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- goods_receipt_seq
-- Backs goods_receipts.sequence_number so receipt numbers (e.g. "GR-0001")
-- are assigned atomically by Postgres.
-- ---------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS goods_receipt_seq
    AS integer
    START WITH 1;

-- ---------------------------------------------------------------------------
-- goods_receipts
--
-- SupplierCode is free text for the MVP — there is no suppliers table/FK yet.
-- See docs/adr/0001-supplier-code-instead-of-supplier-entity.md.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS goods_receipts (
    id               uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sequence_number  integer       NOT NULL DEFAULT nextval('goods_receipt_seq'),
    supplier_code    varchar(50)   NOT NULL,
    received_date    timestamptz   NOT NULL,
    status           text          NOT NULL,
    received_by      text,
    created_at       timestamptz   NOT NULL DEFAULT now(),
    updated_at       timestamptz   NOT NULL DEFAULT now(),
    created_by       text          NOT NULL,
    updated_by       text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_goods_receipts_sequence_number'
    ) THEN
        ALTER TABLE goods_receipts ADD CONSTRAINT uq_goods_receipts_sequence_number UNIQUE (sequence_number);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- goods_receipt_lines
--
-- Duplicate product_variant_id per receipt is allowed (e.g. separate cartons
-- at different unit costs), so there is no uniqueness constraint here.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS goods_receipt_lines (
    id                  uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    goods_receipt_id    uuid          NOT NULL REFERENCES goods_receipts (id),
    product_variant_id  uuid          NOT NULL REFERENCES product_variants (id),
    quantity            integer       NOT NULL,
    unit_cost           numeric(10,2) NOT NULL,
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    created_by          text          NOT NULL,
    updated_by          text          NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_goods_receipt_lines_goods_receipt_id
    ON goods_receipt_lines (goods_receipt_id);

CREATE INDEX IF NOT EXISTS ix_goods_receipt_lines_product_variant_id
    ON goods_receipt_lines (product_variant_id);

-- ---------------------------------------------------------------------------
-- inventory
--
-- One row per product variant — single-store MVP. A future location_id/
-- store_id column can be added later without breaking this row shape; the
-- unique constraint on product_variant_id would then become
-- (product_variant_id, location_id).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    product_variant_id  uuid        NOT NULL REFERENCES product_variants (id),
    quantity            integer     NOT NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    created_by          text        NOT NULL,
    updated_by          text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_inventory_product_variant_id'
    ) THEN
        ALTER TABLE inventory ADD CONSTRAINT uq_inventory_product_variant_id UNIQUE (product_variant_id);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- inventory_transactions
--
-- Immutable ledger — rows are never updated after insert, so there is no
-- updated_at/updated_by. source_type + source_id point back to the record
-- that caused the change (e.g. 'GoodsReceiptLine' + its id).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory_transactions (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    product_variant_id  uuid        NOT NULL REFERENCES product_variants (id),
    type                text        NOT NULL,
    quantity_delta      integer     NOT NULL,
    source_type         text        NOT NULL,
    source_id           uuid        NOT NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    created_by          text        NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_inventory_transactions_product_variant_id
    ON inventory_transactions (product_variant_id);

CREATE INDEX IF NOT EXISTS ix_inventory_transactions_source_type_source_id
    ON inventory_transactions (source_type, source_id);
