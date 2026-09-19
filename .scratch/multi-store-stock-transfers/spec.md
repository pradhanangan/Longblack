# Multi-Store & Stock Transfers

Status: ready-for-agent

## Problem Statement

Longblack currently tracks inventory for a single location: everything a business owns lives in one undifferentiated pool of stock. But a growing retailer doesn't sell out of one place — it opens long-standing shops, runs temporary one-off stores (e.g. market stalls, pop-ups), and sells online, and it needs to move stock from where it arrives (the warehouse) out to wherever it's actually sold. Today there is no way to represent a second location, no way to track how much stock is at each one, and no way to record stock physically moving between them.

## Solution

Introduce a `Store` entity representing any location that holds its own stock — the existing single location becomes the seeded `Warehouse` store, with `LongStanding`, `OneOff`, and `Online` as the other store types. Make `Inventory` and `InventoryTransaction` store-aware (one row per variant *per store*, one ledger entry per store-scoped event) instead of single-location. Introduce a `Stock Transfer` workflow that moves stock from one store to another through a `Draft → Dispatched → Received` lifecycle, always routed through the Warehouse store as either the source or destination (hub-and-spoke — no direct transfers between two non-Warehouse stores). Extend `Goods Receipt` and `Stock Take` to work correctly in a store-aware world: receipts always land at the Warehouse, and a stock take is always scoped to exactly one store.

## User Stories

### Store management

1. As an Admin, I want to create a Store with a name and Type (`LongStanding`, `OneOff`, or `Online`), so that a new selling location can start receiving transferred stock.
2. As an Admin, I want to edit a Store's name, so that I can correct mistakes.
3. As an Admin, I want to list all Stores with their Type and Status, so that I can see every location the business operates.
4. As an Admin, I want to view a single Store's details including its current Inventory, so that I can see what stock is at that location right now.
5. As an Admin, I want to close a Store, so that it no longer appears as an option for new Stock Transfers once it has stopped trading.
6. As an Admin, I want closing a Store to be rejected if it still holds nonzero Inventory for any variant, so that stock can never be silently stranded at a closed location.
7. As an Admin, I want the system to have exactly one Warehouse-type Store, seeded automatically, so that Goods Receiving and the hub-and-spoke transfer rule always have a hub to target.
8. As a Manager or Staff member, I want to list and view Stores (read-only), so that I can pick the right store when creating a Stock Transfer or Stock Take.

### Stock transfers

9. As a Staff member, I want to create a Draft Stock Transfer specifying a source Store, a destination Store, and one or more product variant + quantity lines, so that I can prepare a stock movement before it happens.
10. As a Staff member, I want the system to reject a Stock Transfer where neither the source nor the destination is the Warehouse Store, so that stock movement always stays auditable through the hub.
11. As a Staff member, I want the system to reject a Stock Transfer where the source and destination are the same Store, so that a transfer always represents a real movement.
12. As a Staff member, I want at most one line per product variant on a Stock Transfer, so that quantities aren't split ambiguously across lines.
13. As a Staff member, I want to add, edit, and remove lines on a Draft Stock Transfer, so that I can adjust the transfer before it's dispatched.
14. As a Staff member, I want the system to reject adding a line whose quantity exceeds the source Store's current Inventory for that variant, so that I can't plan to dispatch stock that doesn't exist there.
15. As a Manager, I want to dispatch a Stock Transfer, so that the source Store's Inventory is reduced and the stock is recorded as in transit.
16. As a Manager, I want dispatching a Stock Transfer to create a `TransferOut` Inventory Transaction at the source Store for each line, so that the reduction is auditable.
17. As a Manager, I want to receive a dispatched Stock Transfer and record the actual quantity received per line (which may be less than what was dispatched), so that loss or damage in transit is captured.
18. As a Manager, I want receiving a Stock Transfer to create a `TransferIn` Inventory Transaction at the destination Store for each line's received quantity, so that the increase is auditable.
19. As a Manager, I want to see the variance between dispatched and received quantity per line after a transfer is received, so that I can identify and follow up on discrepancies.
20. As a Staff member, I want to cancel a Draft Stock Transfer, so that a transfer that's no longer needed doesn't stay open.
21. As a Manager or Admin, I want cancelling a Stock Transfer to be unavailable once it has been Dispatched, so that a transfer with stock already removed from the source can't simply vanish without a trace.
22. As a Staff member, I want to list Stock Transfers filterable by status, source Store, or destination Store, so that I can find the transfer I'm looking for.
23. As a Staff member, I want to view a single Stock Transfer's full detail including its lines and their dispatched/received quantities, so that I can review its history.

### Goods Receiving (store-aware)

24. As a Staff member, I want every Goods Receipt to implicitly target the Warehouse Store, so that received stock always enters the system at the hub, ready to be transferred out.
25. As a Staff member, I want marking a Goods Receipt as Received to increase the Warehouse Store's Inventory (not some other store's), so that stock levels stay correct after the store-aware change.

### Stock Take (store-aware)

26. As a Staff member, I want to specify a single Store when creating a Stock Take, so that a count session always corresponds to one physical count at one location.
27. As a Staff member, I want a Stock Take's Expected Quantity for each item to be snapshotted from that Store's Inventory (not another store's), so that variance is calculated against the correct baseline.
28. As a Manager, I want approving a Stock Take to post its `StockTakeAdjustment` Inventory Transactions against that Stock Take's Store, so that the adjustment lands at the right location.

## Implementation Decisions

### Layer structure

Follows the existing four-project layout: `Longblack.Domain` (new entities), `Longblack.Application` (new `Stores` and `StockTransfers` service groups, changes to existing `Receiving` and `StockTake` services), `Longblack.Infrastructure` (EF Core configuration, hand-written SQL script), `Longblack.Api` (new controllers, request/response models; changes to existing `GoodsReceiptsController` and `StockTakesController`).

### Domain entities

- **Store** — id, name, type (string enum: `Warehouse`, `LongStanding`, `OneOff`, `Online`), status (string enum: `Active`, `Closed`), created_at, updated_at, created_by, updated_by. Exactly one row has `type = Warehouse`, created by the seeder.
- **StockTransfer** — id, sequence_number (Postgres-sequence-backed, surfaced as e.g. "TR-0001" like `GoodsReceipt`'s `ReceiptNumber`), source_store_id (FK), destination_store_id (FK), status (string enum: `Draft`, `Dispatched`, `Received`, `Cancelled`), dispatched_date, dispatched_by, received_date, received_by, created_at, updated_at, created_by, updated_by.
- **StockTransferLine** — id, stock_transfer_id (FK), product_variant_id (FK), dispatched_quantity, received_quantity (nullable until the transfer is Received), created_at, updated_at, created_by, updated_by. Unique on (stock_transfer_id, product_variant_id).
- **Inventory** — gains a `store_id` (FK to `Store`, NOT NULL). Unique constraint changes from `(product_variant_id)` to `(product_variant_id, store_id)`.
- **InventoryTransaction** — gains a `store_id` (FK to `Store`, NOT NULL). Two new `Type` values: `TransferOut`, `TransferIn` (alongside existing `GoodsReceived`, `StockTakeAdjustment`, `ManualAdjustment`). `SourceType`/`SourceId` for both point at the `StockTransferLine`.
- **GoodsReceipt** — gains a `store_id` (FK to `Store`, NOT NULL), always set to the Warehouse store's id at creation time; not user-selectable.
- **StockTake** — gains a `store_id` (FK to `Store`, NOT NULL), required at creation time, user-selectable from any Active store.

### Hub-and-spoke enforcement

Enforced at the application service layer (not a DB constraint): creating a `StockTransfer` is rejected unless `source_store_id == warehouseStoreId || destination_store_id == warehouseStoreId`, and rejected if `source_store_id == destination_store_id`. The Warehouse store's id is resolved by querying for `Type = Warehouse` (there is always exactly one).

### Stock Transfer lifecycle

`Draft → Dispatched → Received`, with `Cancel` reachable from `Draft` only (mirrors `GoodsReceipt`'s `Draft → Received → Cancelled` shape, but with the extra in-transit state). Adding/editing/removing lines is only allowed in `Draft`. Dispatching validates each line's `dispatched_quantity` against the source store's current Inventory at that moment, then creates one `TransferOut` Inventory Transaction per line and is the point at which the source Inventory is reduced. Receiving requires a received quantity per line (defaults to matching dispatched quantity if the caller doesn't specify a discrepancy), creates one `TransferIn` Inventory Transaction per line for the received quantity, and is the point at which the destination Inventory is increased. A `Received` transfer is immutable, matching the `GoodsReceipt` convention for terminal receipt states.

### Store closing validation

Closing a Store (`Active → Closed`) is rejected if any `Inventory` row for that store has nonzero quantity. Enforced at the application service layer by checking all `Inventory` rows for the store before allowing the transition.

### API routes

Following the `/api/{resource}` convention:

- `/api/stores` — GET (list), POST (Admin)
- `/api/stores/{id}` — GET, PUT (Admin), PATCH status (Admin, for closing)
- `/api/stock-transfers` — GET (list, filterable by status/source/destination), POST (Staff, Manager, Admin)
- `/api/stock-transfers/{id}` — GET
- `/api/stock-transfers/{id}/lines` — POST (Staff, Manager, Admin)
- `/api/stock-transfers/{id}/lines/{lineId}` — PUT, DELETE (Staff, Manager, Admin)
- `/api/stock-transfers/{id}/dispatch` — POST (Manager, Admin)
- `/api/stock-transfers/{id}/receive` — POST (Manager, Admin), body carries per-line received quantities
- `/api/stock-transfers/{id}/cancel` — POST (Staff, Manager, Admin)
- `/api/goods-receipts` — POST no longer accepts a store selection; `StoreId` is set server-side to the Warehouse store
- `/api/stock-takes` — POST gains a required `storeId` field in the request body

### Authorization

Mirrors the existing `GoodsReceipt`/`StockTake` convention: `Staff,Manager,Admin` for draft creation, line edits, and cancel; `Manager,Admin` for the two actions that move inventory (`Dispatch`, `Receive`). `Store` CRUD is `Admin`-only, matching reference-data entities (`Brand`, `Category`, `Colour`, `Size`).

### Database schema

New hand-written idempotent SQL script `db/005-multi-store-transfers-schema.sql` (run after `004-stock-take-schema.sql`), following the conventions in `db/003-goods-receiving-schema.sql`: `CREATE TABLE IF NOT EXISTS`, a `CREATE SEQUENCE IF NOT EXISTS stock_transfer_seq`, snake_case columns, guarded `ALTER TABLE ... ADD CONSTRAINT` blocks for constraints added to existing tables (`inventory`, `inventory_transactions`, `goods_receipts`, `stock_takes`). No EF Core migrations, per repo convention — `AppDbContext.OnModelCreating` is updated to match, and the SQL script is provided but run manually by the user via `psql`.

### Seeding

`DatabaseSeeder` gains a step that creates the single Warehouse `Store` row if one doesn't already exist (idempotent, same pattern as role/admin-user seeding). A one-time data backfill (in the same SQL script) sets `store_id` = the Warehouse store's id on every existing `inventory`, `inventory_transactions`, and `goods_receipts` row, since those tables predate the store-aware schema.

## Testing Decisions

### What makes a good test

Tests assert external, observable behaviour — HTTP responses and resulting database state — never which internal service or repository method was called. A test should survive an internal refactor unchanged.

### Test seam

Single seam: **HTTP via `WebApplicationFactory<Program>`**, in a new `Longblack.Api.Tests` project (this is the first spec to actually create it; no test project currently exists in the solution despite it being the documented convention from the `product-catalogue` spec). Tests run against a real PostgreSQL test database, no mocking.

### What is tested

- Creating, editing, listing, and closing a Store; closing rejected when Inventory is nonzero
- Exactly one Warehouse store exists after seeding, and it cannot be created a second time
- Creating a Draft Stock Transfer, adding/editing/removing lines
- Hub-and-spoke rejection: transfer between two non-Warehouse stores is rejected; transfer with identical source and destination is rejected
- Line quantity validation against source store's current Inventory
- At most one line per variant per transfer enforced
- Dispatch: reduces source Inventory, creates `TransferOut` transactions, transitions to `Dispatched`
- Receive: increases destination Inventory by received (not dispatched) quantity, creates `TransferIn` transactions, transitions to `Received`, records variance when received ≠ dispatched
- Cancel allowed from `Draft`, rejected from `Dispatched`/`Received`
- Role-based access: Staff cannot Dispatch/Receive; only Admin can manage Stores
- Goods Receipt: `Received` always increases Warehouse Inventory regardless of any other stores that exist
- Stock Take: Expected Quantity snapshotted from the specified store's Inventory only; approving posts adjustments against that store

### Prior art

No existing tests in the codebase yet. `GoodsReceiptsController`/`IGoodsReceiptService` and `StockTakesController`/`IStockTakeService` provide the DTO/controller/service patterns to follow for `StockTransfer` and `Store`. `AuthController` and `DatabaseSeeder` provide patterns for authenticated test requests and seeded fixture data.

## Out of Scope

- Direct store-to-store transfers that don't route through the Warehouse
- Planned/scheduled open and close dates for `OneOff` stores (status-driven lifecycle only)
- Store-specific pricing or online sales/POS integration (a store's Inventory only changes via Goods Receipt, Stock Transfer, and Stock Take in this spec)
- Purchase Orders and Supplier entity (unchanged, still out of scope per existing ADRs)
- Reversing/returning a completed Stock Transfer (a mis-sent transfer requires a new, separate transfer back)
- A recursive "total stock across all stores" reporting endpoint (each Store's Inventory remains independently queryable, but a rollup view is a future concern)
- Frontend / React UI (this spec covers the API only)
- Automatic stock transfer triggered by low inventory at a store (manual creation only)

## Further Notes

- This spec is the implementation of the design settled in [ADR-0003](../../docs/adr/0003-multi-store-transfers-expand-mvp-scope.md) and the updated `Store`/`Stock Transfer`/`Inventory`/`Inventory Transaction` language in `CONTEXT.md`.
- Existing single-store data (all current `inventory`, `inventory_transactions`, and `goods_receipts` rows) must be backfilled to the Warehouse store's id as part of the migration script — this is a one-time, irreversible data change and should be reviewed carefully before running against production data.
- The `received_quantity` on `StockTransferLine` is nullable specifically to distinguish "not yet received" (Draft/Dispatched) from "received, quantity confirmed" (Received) — do not default it to `dispatched_quantity` at line-creation time.
