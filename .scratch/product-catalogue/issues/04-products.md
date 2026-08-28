# 04: Products

**Status:** done

**Blocked by:** 02 — Reference data (Brands, Colours, Sizes), 03 — Reference data (Categories)

**What to build:** The full Clean Architecture stack for Products. A Product is the style-level record (e.g. "Nike Basic T-Shirt") that groups ProductVariants. Products reference Brands and Categories, both of which must exist before a Product can be created.

## Acceptance criteria

- [ ] Domain entity `Product` exists in `Longblack.Domain` with fields: id, product_code (unique), name, description (nullable), brand_id (FK), category_id (FK), status (string-backed enum `Active`/`Inactive`), audit fields
- [ ] EF Core entity configuration maps `Product` to the `products` SQL-created table; no EF migrations are produced
- [ ] `AppDbContext` exposes `DbSet<Product>`
- [ ] Application services in `Longblack.Application` implement:
  - List (supports filtering by brand_id, category_id, status; returns Active only by default)
  - Get by id
  - Create
  - Update
  - Set status (activate / deactivate)
- [ ] API controllers expose:
  - `GET /api/products` — all authenticated roles; supports query params `brandId`, `categoryId`, `status`
  - `POST /api/products` — Manager and Admin only
  - `GET /api/products/{id}` — all authenticated roles
  - `PUT /api/products/{id}` — Manager and Admin only
  - `PATCH /api/products/{id}/status` — Manager and Admin only
- [ ] Creating a Product with a `brandId` or `categoryId` that does not exist returns 422 Unprocessable Entity
- [ ] Creating a Product with a duplicate `productCode` returns 409 Conflict
- [ ] `product_code` is immutable after creation (PUT cannot change it)
- [ ] Audit fields are populated from the authenticated user's identity on every write
- [ ] Staff receive 403 when attempting POST/PUT/PATCH
- [ ] Response bodies include the brand name and category name (not just FKs) for convenient display
