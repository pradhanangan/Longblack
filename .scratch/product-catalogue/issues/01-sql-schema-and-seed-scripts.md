# 01: SQL schema and seed data scripts

**Status:** done

**Blocked by:** None (can start immediately)

**What to build:** Raw SQL scripts that create the catalogue database schema and populate it with default reference data. The application assumes these scripts have been run against the PostgreSQL database before startup. EF Core is used for data access only — it does not own the schema.

## Acceptance criteria

- [ ] A DDL SQL script creates the following tables with correct column types, constraints, and snake_case naming:
  - `brands` — id (uuid PK), name (text, not null), status (text, not null), created_at, updated_at, created_by, updated_by
  - `categories` — id (uuid PK), parent_category_id (uuid, nullable FK to self), name (text, not null), status (text, not null), created_at, updated_at, created_by, updated_by
  - `colours` — id (uuid PK), name (text, not null), code (text, not null), status (text, not null), created_at, updated_at, created_by, updated_by
  - `sizes` — id (uuid PK), name (text, not null), code (text, not null), sort_order (int, not null), status (text, not null), created_at, updated_at, created_by, updated_by
  - `products` — id (uuid PK), product_code (text, unique, not null), name (text, not null), description (text, nullable), brand_id (uuid FK → brands), category_id (uuid FK → categories), status (text, not null), created_at, updated_at, created_by, updated_by
  - `product_variants` — id (uuid PK), product_id (uuid FK → products), sku (text, unique, not null), barcode (text, unique, nullable), colour_id (uuid FK → colours), size_id (uuid FK → sizes), selling_price (numeric, not null), status (text, not null), created_at, updated_at, created_by, updated_by
- [ ] A unique constraint exists on `product_variants.sku`
- [ ] A unique constraint exists on `product_variants.barcode` (nullable-safe: nulls do not violate uniqueness)
- [ ] A unique constraint exists on `products.product_code`
- [ ] A seed SQL script inserts default reference data (idempotent — safe to run multiple times):
  - Sizes: XS, S, M, L, XL, XXL, 3XL (with sort_order 1–7)
  - Colours: Black, White, Grey, Navy, Red, Blue, Green, Pink, Yellow, Brown
  - Categories: Men, Women, Kids (root); T-Shirts, Shirts, Jeans, Shorts under Men; Tops, Dresses, Jeans under Women; Boys, Girls under Kids
  - At least one starter Brand (e.g. Generic / No Brand)
- [ ] Scripts are stored under a `db/` directory at the repo root: `db/001-catalogue-schema.sql` and `db/002-catalogue-seed.sql`
- [ ] A `README` section or inline comment in each script explains the run order and how to execute against a local PostgreSQL database
