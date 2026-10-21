-- =============================================================================
-- Longblack — Purchase Orders Schema
-- Script: 010-purchase-orders-schema.sql
--
-- Run order: after 003-goods-receiving-schema.sql and 008-suppliers-schema.sql
-- (goods_receipts and suppliers must already exist) and
-- 001-catalogue-schema.sql (product_variants).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/010-purchase-orders-schema.sql
--
-- Idempotent: safe to run more than once.
-- See docs/adr/0005-bring-purchase-orders-into-scope.md.
-- =============================================================================

-- Backs purchase_orders.sequence_number. The number (e.g. "PO-0001") is
-- assigned on issue, not on draft creation, so the column is nullable and has
-- no default.
CREATE SEQUENCE IF NOT EXISTS purchase_order_seq
    AS integer
    START WITH 1;

-- ---------------------------------------------------------------------------
-- purchase_orders
--
-- A Purchase Order implicitly targets the Warehouse store, so there is no
-- store_id. sent_at/sent_by hold the latest successful email send or manual
-- mark-as-sent; "sent" is not a status.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS purchase_orders (
    id                      uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sequence_number         integer,
    supplier_id             uuid          NOT NULL REFERENCES suppliers (id),
    expected_delivery_date  timestamptz,
    notes                   text,
    status                  text          NOT NULL,
    issued_at               timestamptz,
    issued_by               text,
    sent_at                 timestamptz,
    sent_by                 text,
    created_at              timestamptz   NOT NULL DEFAULT now(),
    updated_at              timestamptz   NOT NULL DEFAULT now(),
    created_by              text          NOT NULL,
    updated_by              text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_purchase_orders_sequence_number'
    ) THEN
        ALTER TABLE purchase_orders
            ADD CONSTRAINT uq_purchase_orders_sequence_number UNIQUE (sequence_number);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_purchase_orders_supplier_id ON purchase_orders (supplier_id);
CREATE INDEX IF NOT EXISTS ix_purchase_orders_status ON purchase_orders (status);
CREATE INDEX IF NOT EXISTS ix_purchase_orders_created_at ON purchase_orders (created_at);

-- ---------------------------------------------------------------------------
-- purchase_order_lines
--
-- At most one line per product variant per purchase order. unit_cost is the
-- expected cost only; goods_receipt_lines.unit_cost is authoritative.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS purchase_order_lines (
    id                  uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    purchase_order_id   uuid          NOT NULL REFERENCES purchase_orders (id),
    product_variant_id  uuid          NOT NULL REFERENCES product_variants (id),
    quantity            integer       NOT NULL,
    unit_cost           numeric(10,2) NOT NULL,
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    created_by          text          NOT NULL,
    updated_by          text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_purchase_order_lines_po_variant'
    ) THEN
        ALTER TABLE purchase_order_lines
            ADD CONSTRAINT uq_purchase_order_lines_po_variant UNIQUE (purchase_order_id, product_variant_id);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_purchase_order_lines_purchase_order_id ON purchase_order_lines (purchase_order_id);
CREATE INDEX IF NOT EXISTS ix_purchase_order_lines_product_variant_id ON purchase_order_lines (product_variant_id);

-- ---------------------------------------------------------------------------
-- purchase_order_sends
--
-- Immutable log of every email send attempt (successful or not).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS purchase_order_sends (
    id                  uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    purchase_order_id   uuid          NOT NULL REFERENCES purchase_orders (id),
    recipient           text          NOT NULL,
    cc                  text,
    succeeded           boolean       NOT NULL,
    error               text,
    sent_at             timestamptz   NOT NULL DEFAULT now(),
    sent_by             text          NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_purchase_order_sends_purchase_order_id ON purchase_order_sends (purchase_order_id);

-- ---------------------------------------------------------------------------
-- goods_receipts.purchase_order_id
--
-- Optional link: direct receiving without a purchase order stays valid.
-- ---------------------------------------------------------------------------
ALTER TABLE goods_receipts
    ADD COLUMN IF NOT EXISTS purchase_order_id uuid REFERENCES purchase_orders (id);

CREATE INDEX IF NOT EXISTS ix_goods_receipts_purchase_order_id ON goods_receipts (purchase_order_id);
