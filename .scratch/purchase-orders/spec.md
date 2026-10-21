Status: ready-for-agent

# Purchase Orders

## Problem Statement

Stock is ordered from suppliers outside the system (phone, ad-hoc email, spreadsheets). Nobody can see what has been ordered, what is still outstanding, or compare what arrived against what was ordered. Goods Receipts have to be keyed in from scratch every time, and there is no record of what was sent to a Supplier.

## Solution

Add Purchase Orders to Longblack. A Manager or Admin drafts a Purchase Order for a Supplier, issues it (locking it and assigning its number), and emails a PDF to the Supplier or marks it as sent manually. When stock arrives, a Goods Receipt can be created from the Purchase Order with outstanding quantities pre-filled; the Purchase Order tracks how much has been received and moves to `PartiallyReceived` and `Received` automatically. Direct receiving without a Purchase Order keeps working. See ADR-0005 and the Purchase Order entries in `CONTEXT.md`.

## User Stories

1. As a Manager, I want to create a Draft Purchase Order for an Active Supplier, so that I can plan an order before committing to it.
2. As a Manager, I want to add lines (variant, ordered quantity, expected unit cost) to a Draft Purchase Order, so that I can specify exactly what I am ordering.
3. As a Manager, I want at most one line per variant on a Purchase Order, so that quantities are not duplicated by mistake.
4. As a Manager, I want to search variants by SKU or name when adding lines, so that I can find items quickly.
5. As a Manager, I want to edit or remove lines and change the expected delivery date and notes while the Purchase Order is Draft, so that I can refine it.
6. As a Manager, I want to delete or cancel a Draft Purchase Order, so that abandoned drafts do not clutter the list.
7. As a Manager, I want ordered quantity to be greater than zero and expected cost to be non-negative, so that the data is valid.
8. As a Manager, I want a Purchase Order to need at least one line before it can be issued, so that empty orders are never sent.
9. As a Manager, I want to issue a Purchase Order, so that it is locked and receives its number.
10. As a Manager, I want the number (`PO-0001` style) assigned only on issue, so that abandoned drafts do not burn numbers.
11. As a Manager, I want an issued Purchase Order's lines to be locked, so that what the Supplier received matches what we recorded.
12. As a Manager, I want to correct an issued Purchase Order by cancelling and recreating it, so that there is a clear history.
13. As an Admin, I want to be the only role that can cancel an issued Purchase Order, and only while nothing has been received, so that cancellations are controlled.
14. As a Manager, I want to download the PDF of an issued Purchase Order, so that I can print or share it.
15. As a Manager, I want the PDF to show company name and address, PO number, date, Supplier details, lines with SKU, description, quantity, unit cost and line total, the total, and notes, so that the Supplier has everything they need.
16. As a Manager, I want to email the PDF to the Supplier, defaulting the recipient to the Supplier's email, so that I do not retype it.
17. As a Manager, I want to edit the recipient and add an optional CC before sending, so that I can reach the right people.
18. As a Manager, I want to resend a Purchase Order, so that I can follow up if the Supplier did not get it.
19. As a Manager, I want each send attempt logged with who, when, to whom and success or failure, so that I can see what the Supplier was sent.
20. As a Manager, I want to see clearly when an email send failed and retry it, so that failures are not silent.
21. As a Manager, I want to mark an issued Purchase Order as sent manually (for example when phoned through), so that the record reflects reality.
22. As a Manager, I want to receive stock against any issued Purchase Order even if it was never marked sent, so that receiving is never blocked by paperwork.
23. As a Manager, I want to create a Goods Receipt from a Purchase Order, so that the lines are pre-filled with outstanding quantities and expected costs.
24. As a Manager, I want the Goods Receipt's Supplier to match the Purchase Order's Supplier, so that receipts cannot be linked to the wrong order.
25. As a Manager, I want Goods Receipt lines limited to variants on the Purchase Order, so that unexpected items are handled deliberately via direct receiving.
26. As a Manager, I want a warning, not a block, when a receipt line exceeds the outstanding quantity, so that suppliers who over-ship can still be received.
27. As a Manager, I want the actual cost on the Goods Receipt line to remain authoritative over the expected cost, so that purchase cost stays accurate.
28. As a Manager, I want received quantities to count only when the Goods Receipt is marked `Received`, so that drafts do not distort the order status.
29. As a Manager, I want the Purchase Order to become `PartiallyReceived` and then `Received` automatically, so that I do not maintain it by hand.
30. As a Manager, I want to see ordered, received and outstanding quantities per line, so that I know what is still to come.
31. As a Manager, I want to force-close a Purchase Order when a Supplier short-ships, so that it stops showing as outstanding.
32. As a Manager, I want to see all Goods Receipts linked to a Purchase Order, so that I can trace deliveries.
33. As a Manager, I want to receive stock directly without any Purchase Order, so that the existing workflow keeps working.
34. As a Manager, I want to list Purchase Orders filtered by status, Supplier and date, so that I can find what I need.
35. As a Manager, I want to see which issued Purchase Orders are overdue against their expected delivery date, so that I can chase them.
36. As a Staff user, I want read-only access to Purchase Orders, so that I can see what is expected without changing anything.
37. As an Admin, I want the same capabilities as a Manager plus cancelling issued orders, so that I can resolve exceptions.
38. As a Manager, I want an Inactive Supplier to be unusable for new Purchase Orders, so that retired suppliers are not ordered from.
39. As an Admin, I want email settings and company details in configuration and secrets, never committed, so that credentials stay safe.
40. As any user, I want every Purchase Order action to record who did it and when, so that the audit trail is complete.

## Implementation Decisions

- New domain entities `PurchaseOrder` and `PurchaseOrderLine`, plus a PO Send log. Status is a string enum: `Draft`, `Issued`, `PartiallyReceived`, `Received`, `Closed`, `Cancelled`. Standard audit fields on all transactional entities.
- A Purchase Order references exactly one Supplier (must be Active at creation) and implicitly targets the Warehouse Store; there is no store field.
- Fields: PO number (null while Draft), Supplier, expected delivery date (optional), notes, status, `SentAt`/`SentBy` (latest send), plus audit fields. Lines: variant, ordered quantity, expected unit cost; received quantity is derived, not stored.
- At most one line per variant per Purchase Order.
- Transitions: Draft→Issued (needs at least one line; assigns the number; locks lines); Draft→Cancelled; Issued→Cancelled (Admin only, only while nothing received); Issued/PartiallyReceived→Closed (Manager/Admin); Issued→PartiallyReceived→Received derived from `Received` linked Goods Receipts, recomputed when a linked Goods Receipt is marked `Received`.
- PO number is a sequence formatted `PO-0001`, assigned on issue, following the Goods Receipt numbering approach.
- "Sent" is a timestamp, not a status. Receiving is allowed against any Issued or PartiallyReceived Purchase Order.
- `GoodsReceipt` gains an optional `PurchaseOrderId`. When set: Supplier must match; lines must be for variants on the PO; over-receipt produces a warning in the response, not an error; creating from a PO pre-fills lines with outstanding quantity and expected cost. Purchase cost remains on `GoodsReceiptLine`. Existing receipts and direct receiving are unaffected.
- Application layer: simple `PurchaseOrderService` (no MediatR/CQRS), consistent with other services; marking a linked Goods Receipt `Received` refreshes the PO status inside the same database transaction.
- PDF: generated server-side with QuestPDF behind an abstraction, so the same bytes are downloaded and emailed. Company name and address come from a `Company` configuration section; no logo.
- Email: an `IEmailSender` abstraction with an SMTP implementation. Settings come from configuration and secrets (user-secrets or environment variables); nothing real is committed. Each attempt writes a PO Send record (user, time, recipient, CC, outcome, error). Resend is allowed. Recipient defaults to the Supplier email and is editable, with optional CC.
- Roles: Manager and Admin create, edit Draft, issue, send, mark sent, close; Admin only cancels an Issued PO; Staff read-only.
- API: `/api/purchase-orders` resource with list (filter by status, Supplier, date range), get, create, edit Draft, delete/cancel Draft, issue, send email, mark sent, close, cancel, download PDF, and list linked Goods Receipts. Goods Receipt endpoints accept an optional purchase order reference, plus a create-from-PO action.
- Schema: hand-written idempotent SQL script under `db/` in run order (purchase order tables, send log, `purchase_order_id` on `goods_receipts`, snake_case names). No EF migrations; the user applies it via `psql`. Update `AppDbContext.OnModelCreating` to match.
- Frontend (React SPA): Purchase Orders nav item and pages (list, create/edit Draft with variant search, detail with Issue, Send/Resend, Mark sent, Close, Cancel and PDF download gated by role, send history); Goods Receipt screen gains a PO picker and create-from-PO entry point showing over-receipt warnings; overdue indicator on the list.
- `CONTEXT.md`, ADR-0005 and PRD §10 already hold the decisions. Update remaining stale PRD lines (future-capability references) when implementing.

## Testing Decisions

- No automated tests for now; this is deferred, as in the sales spec. Verify manually and via `dotnet build Longblack.slnx` and the frontend `npm run build` and `npm run lint`.
- A good test exercises external behavior only: HTTP requests and responses and resulting database state, never internal service calls.
- The single intended seam, when tests are added, is the HTTP surface (`/api/purchase-orders`, `/api/goods-receipts`) via `WebApplicationFactory<Program>` against a real PostgreSQL database, with `IEmailSender` faked at its boundary. Prior art: `.scratch/multi-store-stock-transfers/spec.md` and `.scratch/sales/spec.md`.

## Out of Scope

- Reorder suggestions or automatic ordering from stock levels.
- Tax, freight, discounts and multi-currency on Purchase Orders.
- Supplier-specific cost lists and cost history.
- Approval workflows.
- Supplier portal and supplier-side acknowledgements.
- Logo on the PDF.
- Receiving against multiple Purchase Orders in one Goods Receipt.
- Returns to suppliers.
- Purchase Orders targeting a Store other than the Warehouse.

## Further Notes

- Seam confirmed with the user: HTTP, tests deferred.
- Adds two new external dependencies: QuestPDF (check its licence terms for this use) and an SMTP/email provider. Provider choice is left to implementation; the abstraction keeps it swappable.
- Concurrency: two Goods Receipts marked `Received` against the same Purchase Order at once must not corrupt status; recompute status inside the transaction.
- Related: ADR-0005, ADR-0004 (Supplier), `docs/stocktake-prd.md` §7 and §10.
