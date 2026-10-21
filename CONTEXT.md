# Goods Receiving

Recording stock that physically arrives at the store from a supplier, and the resulting increase to inventory. See `docs/stocktake-prd.md` §7-9 for the full functional spec.

## Language

**Goods Receipt**:
A record of a delivery of stock arriving at the Warehouse Store (the only Store a Goods Receipt can target), containing one or more lines. Progresses through `Draft` → `Received` → (optionally, from `Draft` only) `Cancelled`. Marking a receipt `Received` is the action that increases inventory; a `Received` receipt is then immutable. May optionally reference one Purchase Order from the same Supplier; lines are then pre-filled with outstanding quantities, may only be for variants on that Purchase Order, and may exceed the ordered quantity with a warning rather than a block.
_Avoid_: Delivery, shipment, receiving order

**Goods Receipt Line**:
A single product variant + quantity + unit cost entry within a Goods Receipt. Multiple lines for the same variant on one receipt are allowed (e.g. separate cartons at different costs).
_Avoid_: Receipt item, line item

**Purchase Order**:
A request to one Supplier for stock, destined for the Warehouse Store, containing one or more Purchase Order Lines. Progresses `Draft` → `Issued` → `PartiallyReceived` → `Received`, with `Closed` (forced by a Manager/Admin when the Supplier short-ships) and `Cancelled` (from `Draft`, or from `Issued` by an Admin only, while nothing has been received). Receiving can occur against any `Issued` Purchase Order. Its number (`PO-0001`) is assigned on `Issued`, and from then on its lines are locked; changing an issued Purchase Order means cancelling and recreating it. Issuing makes a PDF available, which can be emailed to the Supplier or manually marked as sent (recorded as `SentAt`/`SentBy`, not a status); every send is logged. Created and issued by Manager or Admin; Staff read-only. Receiving does not require a Purchase Order — direct receiving remains valid.
_Avoid_: PO order, order, requisition

**Purchase Order Line**:
A single product variant + ordered quantity + expected unit cost within a Purchase Order. Its received quantity is derived from `Received` Goods Receipts linked to the Purchase Order. The expected unit cost only pre-fills the Goods Receipt Line; the Goods Receipt Line's actual cost is authoritative.
_Avoid_: Order item, line item

**Supplier**:
A reference-data entity identifying who a Goods Receipt's stock came from, with a unique `Code`, `Name`, required `Email`, required `Phone`, and a `Status` (`Active`/`Inactive`). A Goods Receipt references exactly one Supplier by FK. Admin-only create/edit/status change, matching the `Brand`/`Colour`/`Size` reference-data convention. Supersedes the earlier free-text `SupplierCode` design — see [ADR-0004](./docs/adr/0004-introduce-supplier-entity.md) (supersedes [ADR-0001](./docs/adr/0001-supplier-code-instead-of-supplier-entity.md)).
_Avoid_: Supplier code (no longer a thing — a Supplier is a full entity, not a string), vendor

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

## Sales

Recording stock sold to anonymous customers at a Store, and the resulting decrease to inventory. Not a full till/POS: no shifts or receipt printing.

**Sale**:
A record of stock sold at exactly one Active Store (the Warehouse Store included), containing one or more Sale Lines and a Payment. Anonymous — no Customer is recorded. Progresses `Draft` → `Completed` → (optionally) `Voided`. Completing a Sale is the action that decreases Inventory, posting one `Sale` Inventory Transaction per line; a Sale Line's quantity can never exceed the Store's available Inventory for that variant, so only in-stock variants can be sold and Inventory never goes negative through a Sale. Any Role can create and complete a Sale; only a Manager or Admin can void one, which posts reversing `SaleVoid` Inventory Transactions. Returns are not yet supported.
_Avoid_: Order, transaction, checkout

**Sale Line**:
A single product variant + quantity entry within a Sale, snapshotting the Selling Price at the time of sale and carrying an optional manual line-level discount.
_Avoid_: Sale item, line item

**Selling Price**:
The GST-inclusive price a product variant sells for, held on the Product Variant. Each Sale Line copies it at sale time so later price changes never alter past Sales. Distinct from purchase cost, which lives only on Goods Receipt Lines.
_Avoid_: Retail price, RRP

**Payment**:
How a Sale was paid (a payment type and amount). Recorded for reference only; no payment processing.
_Avoid_: Tender

## Dashboard

A read-only landing page summarising stock and outstanding work. Same content for every role.

**Dashboard**:
The page shown after login. Stock figures (total units on hand, variants at zero or below) respect an optional Store selector defaulting to all Stores; outstanding-work counts always cover all Stores. Also lists the most recent Inventory Transactions, the most recent completed Sales, and a GST-inclusive total of Completed Sales over the last 7 days (respecting the Store selector). Shows no stock value, because purchase cost lives only on Goods Receipt Lines.
_Avoid_: Home, overview

**In Transit**:
A Stock Transfer that is `Dispatched` but not yet `Received`.
_Avoid_: Pending transfer

## Users & Settings

Who can sign in and what they may do.

**User**:
A person who signs in, holding exactly one Role (`Admin`, `Manager` or `Staff`). Never deleted, because every record's audit fields name its creator; instead a User is deactivated, which blocks sign-in but keeps their name on history. An Admin cannot deactivate or change the Role of themselves, and the last active Admin can never be deactivated or demoted. An Admin sets another User's password directly.
_Avoid_: Account, employee, operator

**Settings**:
Configuration page open to every Role. Everyone sees "My account" (change own password); only Admins also see "Users".
_Avoid_: Admin panel, preferences

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
