# 02: Store-aware Inventory & Goods Receiving

**What to build:** `Inventory` and `InventoryTransaction` become store-scoped (one row per variant *per store*, one ledger entry per store-scoped event), and `Goods Receipt` always targets the Warehouse Store. Receiving a Goods Receipt still works end-to-end exactly as before, but now correctly increases the Warehouse Store's Inventory rather than an implicit single location.

**Blocked by:** 01 (Store reference data)

**Status:** ready-for-agent

- [ ] `Inventory` gains a required `store_id`; uniqueness is now per `(product_variant_id, store_id)` instead of per `product_variant_id` alone
- [ ] `InventoryTransaction` gains a required `store_id`
- [ ] All existing `inventory`, `inventory_transactions`, and `goods_receipts` rows are backfilled to the Warehouse Store's id as part of the migration script
- [ ] `GoodsReceipt` gains a required `store_id`, always set server-side to the Warehouse Store at creation — not accepted from the request body
- [ ] Marking a Goods Receipt `Received` increases the Warehouse Store's Inventory (and only the Warehouse Store's) for each line's variant, via a `GoodsReceived` transaction carrying the Warehouse `store_id`
- [ ] Querying Inventory returns results scoped correctly per store (a variant's quantity at the Warehouse Store is independent of its quantity, if any, elsewhere)
- [ ] Existing Goods Receipt create/edit/add-line/update-line/remove-line/receive/cancel behaviour is unchanged from the caller's perspective (this is a schema/scoping change, not a workflow change)
- [ ] New `db/00N-*.sql` script adds the new columns/constraints and performs the backfill, following the idempotent conventions in `db/003-goods-receiving-schema.sql`
