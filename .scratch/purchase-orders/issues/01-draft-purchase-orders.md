# 01: Draft Purchase Orders

**What to build:** Manager and Admin can create, edit, list, view and delete/cancel a Draft Purchase Order for an Active Supplier, adding lines (variant, ordered quantity, expected unit cost) via variant search. Staff can view. No Goods Receipt or Inventory effect. See `.scratch/purchase-orders/spec.md` and ADR-0005.

**Blocked by:** none.

**Status:** done

- [ ] Hand-written idempotent SQL script `db/010-purchase-orders-schema.sql` creates `purchase_orders`, `purchase_order_lines`, the send log table and nullable `goods_receipts.purchase_order_id` (snake_case, audit fields); `AppDbContext.OnModelCreating` matches. The user applies the script via `psql`
- [ ] Domain entities with string-enum status `Draft`, `Issued`, `PartiallyReceived`, `Received`, `Closed`, `Cancelled`
- [ ] `PurchaseOrderService` and `/api/purchase-orders`: create, edit Draft, delete/cancel Draft, list (filter by status, Supplier, date range), get
- [ ] Inactive Suppliers rejected; quantity > 0; expected cost >= 0; one line per variant
- [ ] Manager/Admin write, Staff read-only
- [ ] Purchase Orders nav item, list screen and Draft create/edit screen with variant search and line removal
- [ ] Builds and lint pass; verified manually
