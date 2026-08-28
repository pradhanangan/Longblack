-- =============================================================================
-- Longblack — Product Catalogue Seed Data
-- Script: 002-catalogue-seed.sql
--
-- Run order: 001-catalogue-schema.sql must be run before this script.
--
-- How to run against a local PostgreSQL database:
--   psql -U <user> -d <database> -f db/002-catalogue-seed.sql
--
-- This script is idempotent: each INSERT uses ON CONFLICT DO NOTHING so rows
-- are only inserted when they do not already exist. It is safe to run multiple
-- times without duplicating data.
--
-- Seed user: rows are attributed to 'system' as the created_by/updated_by value.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Brands
-- ---------------------------------------------------------------------------
INSERT INTO brands (id, name, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0001-000000000001', 'Generic', 'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Colours
-- ---------------------------------------------------------------------------
INSERT INTO colours (id, name, code, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0002-000000000001', 'Black',  'BLK',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000002', 'White',  'WHT',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000003', 'Grey',   'GRY',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000004', 'Navy',   'NVY',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000005', 'Red',    'RED',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000006', 'Blue',   'BLU',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000007', 'Green',  'GRN',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0008-000000000008', 'Pink',   'PNK',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000009', 'Yellow', 'YLW',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0002-000000000010', 'Brown',  'BRN',  'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Sizes (ordered by sort_order)
-- ---------------------------------------------------------------------------
INSERT INTO sizes (id, name, code, sort_order, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0003-000000000001', 'XS',  'XS',  1, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000002', 'S',   'S',   2, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000003', 'M',   'M',   3, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000004', 'L',   'L',   4, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000005', 'XL',  'XL',  5, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000006', 'XXL', 'XXL', 6, 'Active', 'system', 'system'),
    ('00000000-0000-0000-0003-000000000007', '3XL', '3XL', 7, 'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Categories — root level
-- ---------------------------------------------------------------------------
INSERT INTO categories (id, parent_category_id, name, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0004-000000000001', NULL, 'Men',   'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000002', NULL, 'Women', 'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000003', NULL, 'Kids',  'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- Categories — Men
INSERT INTO categories (id, parent_category_id, name, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0004-000000000011', '00000000-0000-0000-0004-000000000001', 'T-Shirts', 'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000012', '00000000-0000-0000-0004-000000000001', 'Shirts',   'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000013', '00000000-0000-0000-0004-000000000001', 'Jeans',    'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000014', '00000000-0000-0000-0004-000000000001', 'Shorts',   'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- Categories — Women
INSERT INTO categories (id, parent_category_id, name, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0004-000000000021', '00000000-0000-0000-0004-000000000002', 'Tops',    'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000022', '00000000-0000-0000-0004-000000000002', 'Dresses', 'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000023', '00000000-0000-0000-0004-000000000002', 'Jeans',   'Active', 'system', 'system')
ON CONFLICT DO NOTHING;

-- Categories — Kids
INSERT INTO categories (id, parent_category_id, name, status, created_by, updated_by) VALUES
    ('00000000-0000-0000-0004-000000000031', '00000000-0000-0000-0004-000000000003', 'Boys',  'Active', 'system', 'system'),
    ('00000000-0000-0000-0004-000000000032', '00000000-0000-0000-0004-000000000003', 'Girls', 'Active', 'system', 'system')
ON CONFLICT DO NOTHING;
