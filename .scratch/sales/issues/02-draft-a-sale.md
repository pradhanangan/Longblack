# 02: Draft a Sale

**What to build:** Staff, Manager and Admin can create, edit, list, view and cancel a Draft Sale at a non-Warehouse, Active Store, adding Sale Lines via variant search, with prices snapshotted from the Selling Price, optional manual line discounts, and a Payment type/amount. No Inventory effect yet. See `.scratch/sales/spec.md`.

**Blocked by:** 01 (Selling Price on product variants).

**Status:** done

- [ ] SQL script under `db/` creates sales, sale lines and payments tables (snake_case, audit fields); `AppDbContext` matches
- [ ] Domain entities with string-enum status `Draft`, `Completed`, `Voided`, `Cancelled`
- [ ] `SaleService` and `/api/sales`: create, edit Draft, cancel Draft, list (filter by Store, status, date range), get
- [ ] Warehouse Store, Closed Store and unpriced variants are rejected
- [ ] One Sale Line per variant (repeated variants consolidated); a discount cannot exceed the line total; Sale Lines copy the Selling Price
- [ ] Sales list screen and Draft create/edit screen with variant search and line removal
- [ ] Amounts shown GST-inclusive
- [ ] Builds and lint pass; verified manually
