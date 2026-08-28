# 02: Reference data — Brands, Colours, Sizes

**Status:** done

**Blocked by:** 01 — SQL schema and seed data scripts

**What to build:** The full Clean Architecture stack for Brands, Colours, and Sizes: domain entities, EF Core mappings (pointed at the SQL-created tables), application services, and API endpoints. Admins can create, edit, and deactivate; all authenticated users can read.

## Acceptance criteria

- [ ] Domain entities `Brand`, `Colour`, and `Size` exist in `Longblack.Domain` with all fields matching the SQL schema (id, name, code where applicable, sort_order for Size, status as a string-backed enum, audit fields)
- [ ] `Status` enum for reference data uses `Active` / `Inactive` values
- [ ] EF Core entity configurations map each entity to its SQL-created table (snake_case table and column names); no EF migrations are produced
- [ ] `AppDbContext` exposes `DbSet<Brand>`, `DbSet<Colour>`, `DbSet<Size>`
- [ ] Application services in `Longblack.Application` implement:
  - List (returns active only by default; accepts optional status filter)
  - Get by id
  - Create
  - Update
  - Set status (activate / deactivate)
- [ ] API controllers in `Longblack.Api` expose:
  - `GET /api/brands` — all authenticated roles
  - `POST /api/brands` — Admin only
  - `GET /api/brands/{id}` — all authenticated roles
  - `PUT /api/brands/{id}` — Admin only
  - `PATCH /api/brands/{id}/status` — Admin only
  - Same pattern for `/api/colours` and `/api/sizes`
- [ ] `GET /api/sizes` returns sizes ordered by `sort_order` ascending
- [ ] Attempting to create a Brand/Colour/Size with a duplicate name returns a 409 Conflict
- [ ] Audit fields (`created_by`, `created_at`, `updated_by`, `updated_at`) are populated from the authenticated user's identity on every write
- [ ] Seeded rows (from ticket 01 SQL script) are returned correctly by the list endpoints
- [ ] Staff and Manager receive 403 when attempting POST/PUT/PATCH on any reference data endpoint
