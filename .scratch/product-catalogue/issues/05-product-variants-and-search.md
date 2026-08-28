# 05: ProductVariants and product search

**Status:** ready-for-agent

**Blocked by:** 04 — Products

**What to build:** The full Clean Architecture stack for ProductVariants (the countable SKU), plus the cross-variant search endpoint on Products. A ProductVariant is the thing you physically count and receive stock against — it has a SKU, barcode, colour, size, and selling price.

## Acceptance criteria

- [ ] Domain entity `ProductVariant` exists in `Longblack.Domain` with fields: id, product_id (FK), sku (unique), barcode (unique, nullable), colour_id (FK), size_id (FK), selling_price (decimal), status (string-backed enum `Active`/`Inactive`), audit fields
- [ ] EF Core entity configuration maps `ProductVariant` to the `product_variants` SQL-created table; no EF migrations are produced
- [ ] `AppDbContext` exposes `DbSet<ProductVariant>`
- [ ] Application services in `Longblack.Application` implement:
  - List variants for a product (returns Active only by default; accepts optional status filter)
  - Get variant by id
  - Create variant
  - Update variant
  - Set variant status (activate / deactivate)
  - Search across all variants by query string
- [ ] API controllers expose:
  - `GET /api/products/{id}/variants` — all authenticated roles
  - `POST /api/products/{id}/variants` — Manager and Admin only
  - `GET /api/products/{id}/variants/{variantId}` — all authenticated roles
  - `PUT /api/products/{id}/variants/{variantId}` — Manager and Admin only
  - `PATCH /api/products/{id}/variants/{variantId}/status` — Manager and Admin only
  - `GET /api/products?q={query}` — all authenticated roles; searches product name, product code, variant SKU, and variant barcode; returns matching Products with their matching variants
- [ ] Creating a ProductVariant with a duplicate SKU returns 409 Conflict
- [ ] Creating a ProductVariant with a duplicate barcode (where supplied) returns 409 Conflict
- [ ] Creating a ProductVariant against an inactive Product returns 422 Unprocessable Entity
- [ ] Creating a ProductVariant against a non-existent `colourId` or `sizeId` returns 422 Unprocessable Entity
- [ ] `sku` is immutable after creation (PUT cannot change it)
- [ ] Audit fields are populated from the authenticated user's identity on every write
- [ ] Staff receive 403 when attempting POST/PUT/PATCH
- [ ] `GET /api/products?q=` with a barcode value returns the correct Product and variant
- [ ] Response includes colour name and size name (not just FKs) for convenient display
