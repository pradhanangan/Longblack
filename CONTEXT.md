# Goods Receiving

Recording stock that physically arrives at the store from a supplier, and the resulting increase to inventory. See `docs/stocktake-prd.md` §7-9 for the full functional spec.

## Language

**Goods Receipt**:
A record of a delivery of stock arriving at the store, containing one or more lines. Progresses through `Draft` → `Received` → (optionally, from `Draft` only) `Cancelled`. Marking a receipt `Received` is the action that increases inventory; a `Received` receipt is then immutable.
_Avoid_: Delivery, shipment, receiving order

**Goods Receipt Line**:
A single product variant + quantity + unit cost entry within a Goods Receipt. Multiple lines for the same variant on one receipt are allowed (e.g. separate cartons at different costs).
_Avoid_: Receipt item, line item

**Supplier Code**:
A free-text identifier of the supplier a Goods Receipt came from, entered directly on the Goods Receipt. For the MVP this is plain text with no backing `Supplier` entity, no uniqueness enforcement, and no FK — see [ADR-0001](./docs/adr/0001-supplier-code-instead-of-supplier-entity.md). A future `Supplier` reference-data entity may replace this with a proper reference.
_Avoid_: Supplier ID, supplier reference

**Inventory Transaction**:
An immutable record of a single quantity change to one product variant's inventory, carrying a `Type` (e.g. `GoodsReceived`), a signed quantity delta, and a reference back to the source record that caused it (e.g. a Goods Receipt Line). One is created per Goods Receipt Line when a receipt is marked `Received`. The current inventory balance is always derived by summing transactions, never stored as a snapshot on the transaction itself.
_Avoid_: Stock movement, ledger entry
