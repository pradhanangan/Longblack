Status: ready-for-agent

# Sales

## Problem Statement

Longblack tracks stock arriving, moving between Stores and being counted, but has no way to record stock leaving through a sale. Store staff cannot record what they sold, so Inventory drifts upward until the next Stock Take, and the Dashboard cannot show what is actually selling. There is also no Selling Price anywhere in the system.

## Solution

Add lightweight sale recording: a Sale is created at one Active Store (the Warehouse included), holds one or more Sale Lines priced from the variant's Selling Price, and records a Payment. Completing a Sale decreases that Store's Inventory through `Sale` Inventory Transactions. A Manager or Admin can void a completed Sale, which restores stock via reversing `SaleVoid` transactions. This is not a full till: no shifts, receipt printing, payment processing, customers, promotions or returns.

Vocabulary follows the Sales section of `CONTEXT.md`.

## User Stories

1. As an Admin, I want to set a Selling Price on a product variant, so that sales have a price to start from.
2. As an Admin, I want a variant with no Selling Price to be unsellable, so that nothing is sold at an unknown price.
3. As a Manager, I want to change a variant's Selling Price, so that I can reprice stock.
4. As a Manager, I want past Sales to keep the price they sold at, so that repricing never rewrites history.
5. As a Staff member, I want to start a Sale at my Store, so that I can record a customer purchase.
6. As a Staff member, I want to add a variant to a Sale by searching SKU, name, brand, colour or size, so that I can find items quickly.
7. As a Staff member, I want a Sale Line to default to the variant's Selling Price, so that I don't type prices.
8. As a Staff member, I want to set a quantity on each Sale Line, so that I can sell several units.
9. As a Staff member, I want to apply a manual discount to a Sale Line, so that I can honour a price adjustment.
10. As a Staff member, I want a discount rejected if it exceeds the line total, so that a line never goes negative.
11. As a Staff member, I want a line removed from a Draft Sale, so that I can fix mistakes before completing.
12. As a Staff member, I want one Sale Line per variant consolidated, so that quantities are not scattered across lines.
13. As a Staff member, I want to record the Payment type and amount, so that the Sale shows how it was paid.
14. As a Staff member, I want to complete a Sale, so that Inventory at my Store decreases.
15. As a Staff member, I want a Completed Sale to be immutable, so that records stay trustworthy.
16. As a Staff member, I want to cancel a Draft Sale, so that abandoned sales leave no trace in Inventory.
17. As a Staff member, I want only variants with available stock at my Store offered for sale, and quantities capped to that stock, so that Inventory never goes negative.
18. As a Manager, I want to void a Completed Sale, so that I can correct a wrongly recorded sale.
19. As a Manager, I want voiding to restore Inventory, so that stock matches reality again.
20. As a Staff member, I want voiding to be unavailable to me, so that sales cannot be reversed without oversight.
21. As an Admin, I want a void to record who voided it and when, so that it is auditable.
22. As a Manager, I want to list Sales filtered by Store, status and date range, so that I can review activity.
23. As a Manager, I want to open a Sale and see its lines, discounts, Payment and status, so that I can answer questions about it.
24. As a Staff member, I want to pick any Active Store, including the Warehouse, so that I can sell from the hub.
25. As a Manager, I want a Sale at a Closed Store rejected, so that closed Stores stay at zero.
26. As a Manager, I want each Sale to appear in the Inventory Transaction history with its Sale as source, so that stock changes are traceable.
27. As a Manager, I want the Dashboard to show recent Sales and a sales total, so that I can see trading at a glance.
28. As a Manager, I want the Dashboard sales total to respect the Store selector, so that I can look at one Store.
29. As a Manager, I want Stock Take Expected Quantity to reflect sold stock, so that counts reconcile against up-to-date Inventory.
30. As a Staff member, I want amounts shown GST-inclusive, so that totals match shelf prices.
31. As an Admin, I want audit fields on every Sale, so that creator and timestamps are always known.

## Implementation Decisions

- New domain concepts: Sale, Sale Line and Payment, with string-enum status `Draft` → `Completed` → `Voided`, plus `Cancelled` from `Draft` only. Audit fields on all transactional entities, plus void audit (who and when).
- Selling Price is a GST-inclusive field on ProductVariant. Each Sale Line copies it at sale time. Purchase cost stays only on Goods Receipt Lines. Per-Store price overrides are deferred. Candidate for an ADR (hard to reverse, needs context).
- A Sale belongs to exactly one Active Store (including the Warehouse); Closed Stores are rejected.
- Anonymous: no Customer reference.
- Sale Lines are consolidated to at most one per variant per Sale. Optional manual line-level discount, an amount that may not exceed the line total. No rule-based promotions.
- Payment records a type and amount for reference only; no processing. Payment amount must equal the Sale total on completion. The payment type set is a small string enum, to be fixed during implementation.
- Completing a Sale posts one `Sale` Inventory Transaction per line (negative delta, source is the Sale Line) at the Sale's Store. Quantity may not exceed the Store's available Inventory; this is checked when a line is added or edited and re-checked on completion, which rejects the whole Sale if any line fails.
- Voiding posts reversing `SaleVoid` transactions and moves the Sale to `Voided`. Only Manager and Admin can void; Staff, Manager and Admin can create, edit Draft and complete.
- Add `Sale` and `SaleVoid` to the Inventory Transaction type set. Inventory remains derived from transactions.
- Application layer: a simple `SaleService` (no MediatR/CQRS), consistent with the other services. Complete and void run in a single database transaction.
- API: `/api/sales` resource with list (filter by Store, status, date range), get, create, edit Draft, cancel, complete and void actions. A variant's Selling Price is edited through the existing product-variant endpoints.
- Schema: hand-written idempotent SQL script under `db/` in run order (sales tables, `selling_price` column, snake_case names). No EF migrations; the user applies it via `psql`. Update `AppDbContext.OnModelCreating` to match.
- Frontend (React SPA): sales list, a create/edit Draft screen with variant search, and a detail screen with Complete, Cancel and Void actions gated by role. Selling Price field on the variant form. Dashboard gains recent Sales and a sales total that respects the Store selector.
- `CONTEXT.md` already holds the glossary terms. Update the Dashboard entry when the dashboard change lands.

## Testing Decisions

- No automated tests for now; this is deferred. Verify manually and via `dotnet build Longblack.slnx` and the frontend `npm run build` and `npm run lint`.
- When tests are added later, the intended seam is HTTP via `WebApplicationFactory<Program>` against a real PostgreSQL database, as in `.scratch/multi-store-stock-transfers/spec.md`.

## Out of Scope

- Full POS features: shifts, cash drawer, receipt printing, barcode hardware, payment gateways.
- Customer management and customer-linked Sales.
- Returns and exchanges (void only for now).
- Rule-based promotions and coupons.
- Per-Store price overrides and price history.
- Purchase orders, native mobile apps and container orchestration (unchanged).

## Further Notes

- `.github/copilot-instructions.md` and `docs/stocktake-prd.md` §2.2 have already had sales/POS removed from the out-of-scope lists. Customer management remains out of scope.
- GST rate and rounding rules are not specified; treat prices as GST-inclusive and show GST only if a later spec asks for it.
