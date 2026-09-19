# Expand MVP scope to multi-store inventory and stock transfers

The PRD (§2.2) and repo instructions list "Multi-store management" as out of scope for the MVP, and `Inventory`/`InventoryTransaction` were designed as single-store (one row per variant, no location). We are deliberately expanding scope to support multiple stores — long-standing shops, one-off/temporary stores, and an online store — and transferring stock between them.

We chose a hub-and-spoke model over free-form store-to-store transfers or a separate central-only inventory table:

- `Store` is a single entity with a `Type` (`Warehouse`, `LongStanding`, `OneOff`, `Online`). The existing single location becomes a seeded `Store` row (`Type = Warehouse`) rather than a null/special-cased "central" — every store, including HQ, has the same shape.
- `Inventory` and `InventoryTransaction` both gain a `StoreId`, becoming one row per variant *per store*. This mirrors the existing rule that `Inventory` is a derived cache of `InventoryTransaction`: since a transfer's `TransferOut`/`TransferIn` transactions are inherently store-specific events, the ledger has to be store-aware regardless of how the cache table is shaped, so a single unified `Inventory(store_id, variant_id)` table (rather than a separate central-only table plus a per-store table) avoids duplicating cache and query logic.
- A `StockTransfer` must have the Warehouse store as either its source or destination — no direct transfers between two non-warehouse stores. This keeps stock movement auditable through one hub instead of an N×N matrix of store pairs.
- `GoodsReceipt` still only ever targets the Warehouse store; goods enter the system at the hub and reach other stores only via transfer.

This is hard to reverse: it touches the schema of `Inventory`, `InventoryTransaction`, and `GoodsReceipt`, and reframes `StockTake` to be scoped per store. Existing single-store data is backfilled with the Warehouse store's id.
