# 03: Reference data — Categories

**Status:** done

**Blocked by:** 01 — SQL schema and seed data scripts

**What to build:** The full Clean Architecture stack for Categories, including the self-referential parent/child hierarchy. Same layer shape as ticket 02 but with the additional relationship: a Category can optionally belong to a parent Category, allowing unlimited nesting.

## Acceptance criteria

- [ ] Domain entity `Category` exists in `Longblack.Domain` with fields: id, name, parent_category_id (nullable), status (string-backed enum `Active`/`Inactive`), audit fields
- [ ] EF Core entity configuration maps `Category` to the `categories` SQL-created table with a self-referential FK on `parent_category_id`; no EF migrations are produced
- [ ] `AppDbContext` exposes `DbSet<Category>`
- [ ] Application services in `Longblack.Application` implement:
  - List (returns active only by default; accepts optional status filter)
  - Get by id
  - Create (accepts optional `parentCategoryId`)
  - Update
  - Set status (activate / deactivate)
- [ ] API controllers expose:
  - `GET /api/categories` — all authenticated roles
  - `POST /api/categories` — Admin only
  - `GET /api/categories/{id}` — all authenticated roles
  - `PUT /api/categories/{id}` — Admin only
  - `PATCH /api/categories/{id}/status` — Admin only
- [ ] `GET /api/categories` response includes `parentCategoryId` (nullable) on each item; no recursive tree expansion
- [ ] Creating a child Category with a non-existent `parentCategoryId` returns 422 Unprocessable Entity
- [ ] Deactivating a Category that has active children is allowed (children are not cascade-deactivated)
- [ ] Audit fields are populated from the authenticated user's identity on every write
- [ ] Seeded root categories (Men, Women, Kids) and their children are returned correctly by the list endpoint
- [ ] Staff and Manager receive 403 when attempting POST/PUT/PATCH on any category endpoint
