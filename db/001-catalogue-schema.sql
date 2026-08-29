-- =============================================================================
-- Longblack — Product Catalogue Schema
-- Script: 001-catalogue-schema.sql
--
-- Run order: this script must be run before 002-catalogue-seed.sql
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/001-catalogue-schema.sql
--
-- This script is idempotent: it uses CREATE TABLE IF NOT EXISTS so it is safe
-- to run more than once. Constraints are added inline on first creation only.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- brands
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS brands (
    id          uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    name        text        NOT NULL,
    code        text        NOT NULL,
    status      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    created_by  text        NOT NULL,
    updated_by  text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_brands_code'
    ) THEN
        ALTER TABLE brands ADD CONSTRAINT uq_brands_code UNIQUE (code);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- categories (self-referential hierarchy, unlimited depth)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS categories (
    id                  uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    parent_category_id  uuid        REFERENCES categories (id),
    name                text        NOT NULL,
    code                text        NOT NULL,
    status              text        NOT NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    created_by          text        NOT NULL,
    updated_by          text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_categories_code'
    ) THEN
        ALTER TABLE categories ADD CONSTRAINT uq_categories_code UNIQUE (code);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- colours
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS colours (
    id          uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    name        text        NOT NULL,
    code        text        NOT NULL,
    status      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    created_by  text        NOT NULL,
    updated_by  text        NOT NULL
);

-- ---------------------------------------------------------------------------
-- sizes
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sizes (
    id          uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    name        text        NOT NULL,
    code        text        NOT NULL,
    sort_order  integer     NOT NULL,
    status      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    created_by  text        NOT NULL,
    updated_by  text        NOT NULL
);

-- ---------------------------------------------------------------------------
-- products
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS products (
    id            uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    product_code  text        NOT NULL,
    name          text        NOT NULL,
    description   text,
    brand_id      uuid        REFERENCES brands (id),
    category_id   uuid        REFERENCES categories (id),
    status        text        NOT NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    created_by    text        NOT NULL,
    updated_by    text        NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_products_product_code'
    ) THEN
        ALTER TABLE products ADD CONSTRAINT uq_products_product_code UNIQUE (product_code);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- product_variants
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS product_variants (
    id            uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    product_id    uuid          NOT NULL REFERENCES products (id),
    sku           text          NOT NULL,
    barcode       text,
    colour_id     uuid          NOT NULL REFERENCES colours (id),
    size_id       uuid          NOT NULL REFERENCES sizes (id),
    selling_price numeric(10,2) NOT NULL,
    status        text          NOT NULL,
    created_at    timestamptz   NOT NULL DEFAULT now(),
    updated_at    timestamptz   NOT NULL DEFAULT now(),
    created_by    text          NOT NULL,
    updated_by    text          NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_product_variants_sku'
    ) THEN
        ALTER TABLE product_variants ADD CONSTRAINT uq_product_variants_sku UNIQUE (sku);
    END IF;

    -- Nulls are distinct in PostgreSQL unique constraints by default,
    -- so this constraint correctly allows multiple NULL barcodes.
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_product_variants_barcode'
    ) THEN
        ALTER TABLE product_variants ADD CONSTRAINT uq_product_variants_barcode UNIQUE (barcode);
    END IF;
END
$$;
