-- =============================================================================
-- Longblack — Sales Schema
-- Script: 009-sales-schema.sql
--
-- Run order: after 001-catalogue-schema.sql (product_variants, with its
-- selling_price column) and 005-multi-store-transfers-schema.sql (stores).
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/009-sales-schema.sql
--
-- Idempotent: safe to run more than once.
--
-- inventory_transactions.type is free text, so the new 'Sale' and 'SaleVoid'
-- types need no schema change.
-- =============================================================================

CREATE SEQUENCE IF NOT EXISTS sale_seq
    AS integer
    START WITH 1;

-- ---------------------------------------------------------------------------
-- sales
--
-- A Sale is never made at the Warehouse store; that is enforced at the
-- application layer. Payment is set on completion and recorded for reference.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sales (
    id               uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sequence_number  integer       NOT NULL DEFAULT nextval('sale_seq'),
    store_id         uuid          NOT NULL REFERENCES stores (id),
    status           text          NOT NULL,
    payment_type     text,
    payment_amount   numeric(10,2),
    completed_date   timestamptz,
    completed_by     text,
    voided_date      timestamptz,
    voided_by        text,
    created_at       timestamptz   NOT NULL DEFAULT now(),
    updated_at       timestamptz   NOT NULL DEFAULT now(),
    created_by       text          NOT NULL,
    updated_by       text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_sales_sequence_number'
    ) THEN
        ALTER TABLE sales ADD CONSTRAINT uq_sales_sequence_number UNIQUE (sequence_number);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_sales_store_id ON sales (store_id);
CREATE INDEX IF NOT EXISTS ix_sales_status ON sales (status);
CREATE INDEX IF NOT EXISTS ix_sales_created_at ON sales (created_at);

-- ---------------------------------------------------------------------------
-- sale_lines
--
-- At most one line per product variant per sale. unit_price is a snapshot of
-- product_variants.selling_price (GST-inclusive) taken when the line is added;
-- discount applies to the whole line.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sale_lines (
    id                  uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    sale_id             uuid          NOT NULL REFERENCES sales (id),
    product_variant_id  uuid          NOT NULL REFERENCES product_variants (id),
    quantity            integer       NOT NULL,
    unit_price          numeric(10,2) NOT NULL,
    discount            numeric(10,2) NOT NULL DEFAULT 0,
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    created_by          text          NOT NULL,
    updated_by          text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_sale_lines_sale_variant'
    ) THEN
        ALTER TABLE sale_lines
            ADD CONSTRAINT uq_sale_lines_sale_variant UNIQUE (sale_id, product_variant_id);
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_sale_lines_sale_id ON sale_lines (sale_id);
CREATE INDEX IF NOT EXISTS ix_sale_lines_product_variant_id ON sale_lines (product_variant_id);
