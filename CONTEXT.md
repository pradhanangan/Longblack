# Goods Receiving

Recording stock that physically arrives at the store from a supplier, and the resulting increase to inventory. See `docs/stocktake-prd.md` §7-9 for the full functional spec.

## Language

**Goods Receipt**:
A record of a delivery of stock arriving at the Warehouse Store (the only Store a Goods Receipt can target), containing one or more lines. Progresses through `Draft` → `Received` → (optionally, from `Draft` only) `Cancelled`. Marking a receipt `Received` is the action that increases inventory; a `Received` receipt is then immutable.
_Avoid_: Delivery, shipment, receiving order

**Goods Receipt Line**:
A single product variant + quantity + unit cost entry within a Goods Receipt. Multiple lines for the same variant on one receipt are allowed (e.g. separate cartons at different costs).
_Avoid_: Receipt item, line item

**Supplier Code**:
A free-text identifier of the supplier a Goods Receipt came from, entered directly on the Goods Receipt. For the MVP this is plain text with no backing `Supplier` entity, no uniqueness enforcement, and no FK — see [ADR-0001](./docs/adr/0001-supplier-code-instead-of-supplier-entity.md). A future `Supplier` reference-data entity may replace this with a proper reference.
_Avoid_: Supplier ID, supplier reference

**Inventory Transaction**:
An immutable record of a single quantity change to one product variant's inventory at one Store, carrying a `Type` (e.g. `GoodsReceived`, `TransferOut`, `TransferIn`), a signed quantity delta, and a reference back to the source record that caused it (e.g. a Goods Receipt Line, or a Stock Transfer). One is created per Goods Receipt Line when a receipt is marked `Received`; a Stock Transfer creates a `TransferOut` at its source Store on `Dispatched` and a `TransferIn` at its destination Store on `Received`. The current inventory balance is always derived by summing transactions, never stored as a snapshot on the transaction itself.
_Avoid_: Stock movement, ledger entry

**Inventory**:
The current system stock quantity of a single product variant at a single Store (one row per variant per Store), always equal to the sum of that variant's Inventory Transactions at that Store. A variant that has never had a transaction at a Store still has an Inventory of zero there — it is never considered "absent" from stock, only at zero.
_Avoid_: Stock level, quantity on hand, stock count

## Stores & Transfers

Tracking stock across more than one physical or virtual location, and moving stock between them.

**Store**:
A location that holds its own Inventory, distinguished by `Type` (`Warehouse`, `LongStanding`, `OneOff`, `Online`). Exactly one Store has `Type = Warehouse` — the hub where stock enters the system via Goods Receipt. Has a Status (`Active`/`Closed`); closing requires its Inventory to be zero for every variant first. Lifecycle is status-driven only — no planned open/close dates, even for `OneOff` stores.
_Avoid_: Location, branch, shop, site

**Stock Transfer**:
A movement of stock from one Store to another, always with the Warehouse Store as either source or destination — direct transfers between two non-Warehouse stores aren't allowed; they go through the Warehouse as two separate transfers. Progresses `Draft` → `Dispatched` → `Received`, with `Cancel` reachable from `Draft`. Dispatching reduces the source Store's Inventory; Receiving increases the destination Store's Inventory. The received quantity may differ from the dispatched quantity (e.g. loss or damage in transit).
_Avoid_: Stock movement, inventory transfer, shipment

**Stock Transfer Line**:
A single product variant + quantity entry within a Stock Transfer, consolidated to at most one line per variant per transfer (no cost dimension, unlike a Goods Receipt Line).
_Avoid_: Transfer item, line item

## Stock Take

Comparing physical stock against system Inventory for a chosen scope, and adjusting Inventory to match what was actually found.

**Stock Take**:
A count session scoped to exactly one Store, covering a scope of variants — optionally filtered by Brand and/or Category (no filter means the whole catalogue at that Store). Progresses `Draft` → `InProgress` → `Completed` → `Approved`, with `Cancel` reachable from `Draft` or `InProgress`, and an explicit `Completed` → `InProgress` reopen for when a completed count needs revisiting. Only `Approved` is terminal and read-only; approving posts one `StockTakeAdjustment` Inventory Transaction per item with nonzero Variance.
_Avoid_: Stocktake, inventory count, cycle count

**Stock Take Item**:
One product variant within a Stock Take's scope. Generated — with its Expected Quantity snapshotted from Inventory at that moment — only when the Stock Take starts (`Draft` → `InProgress`), never at creation. Tracks Status (`Pending` until first counted, then `Counted`) and mirrors its latest Stock Take Count for quick display. Removable from the list only while still `Pending`.
_Avoid_: Count item, stock take line

**Stock Take Count**:
A single physical count entry recorded against a Stock Take Item. Never edited or deleted — recounting always appends a new Count, so the full count history stays visible for audit. Variance is always calculated from the latest Count only; earlier counts are historical record.
_Avoid_: Count record, count entry

**Variance**:
A Stock Take Item's latest Count minus its Expected Quantity. Positive means more stock was physically found than the system expected; negative means less.
_Avoid_: Discrepancy, difference
