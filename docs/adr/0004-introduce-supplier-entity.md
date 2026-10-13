# Introduce a Supplier entity, replacing free-text Supplier Code

**Supersedes [ADR-0001](./0001-supplier-code-instead-of-supplier-entity.md).**

ADR-0001 deliberately deferred a `Supplier` reference-data entity, storing a free-text `SupplierCode` directly on `GoodsReceipt` instead. The business now needs real supplier master data — a contact email and phone per supplier — so we're introducing the `Supplier` entity the PRD (§6) originally specified.

- `Supplier` — id, code, name, email, phone, status (`Active`/`Inactive`), standard audit fields. `Code` is unique. Shape and CRUD convention mirror `Brand`/`Colour`/`Size` exactly (Admin-only create/edit/status, Staff/Manager/Admin read).
- `GoodsReceipt.SupplierCode` (string) is replaced by `GoodsReceipt.SupplierId` (FK to `Supplier`). A Goods Receipt now references a real Supplier row instead of carrying an uncontrolled free-text value.
- Email and Phone are both required on `Supplier`.

### Migration of existing data

This is irreversible for any environment with real Goods Receipt history: every distinct existing `goods_receipts.supplier_code` value is backfilled into a new `Supplier` row (`code` = the old value, `name` = the old value, placeholder `email`/`phone`), and `goods_receipts.supplier_id` is populated by matching on that code before `supplier_code` is dropped. The placeholder contact details must be corrected manually via the UI after migration — see `db/008-suppliers-schema.sql`.

### Why now

Unlike ADR-0001's MVP-era tradeoff (avoid building a CRUD module the workflow didn't yet need), the business now wants to manage supplier contact details directly, and a free-text code can't carry that. The cost ADR-0001 flagged (table + backfill + FK migration) is paid here.
