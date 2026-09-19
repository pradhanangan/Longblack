# 01: Store reference data

**What to build:** A `Store` entity representing any location that holds its own stock, distinguished by a `Type` (`Warehouse`, `LongStanding`, `OneOff`, `Online`). Admins can create, edit, and list Stores; Managers and Staff have read-only access. Exactly one `Warehouse`-type Store exists, created automatically at startup, and cannot be created again by a user.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `Store` has name, type, status (`Active`/`Closed`), and the standard audit fields (created/updated at/by)
- [ ] Admin can create a Store with name + type (`LongStanding`, `OneOff`, or `Online`)
- [ ] Admin can edit a Store's name
- [ ] Admin, Manager, and Staff can all list Stores (with type and status) and view a single Store's details
- [ ] Staff/Manager cannot create or edit a Store (Admin-only)
- [ ] A single `Warehouse`-type Store is seeded automatically and idempotently at application startup (safe to restart without duplicating it)
- [ ] Attempting to create a second `Warehouse`-type Store via the API is rejected
- [ ] New `db/00N-*.sql` script (idempotent, following the conventions in `db/003-goods-receiving-schema.sql`) creates the `stores` table
