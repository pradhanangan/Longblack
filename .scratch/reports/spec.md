Status: ready-for-agent

# Reports

## Problem Statement

Longblack records stock, sales, movements and stock takes, but the only aggregate view is the Dashboard summary. Managers cannot filter, drill down, or export to answer questions such as "what is low at Store X?", "what moved last week?", "what did we sell by category?" or "where is shrinkage coming from?".

## Solution

A single Reports page (`/reports`) for Admins and Managers. The user picks a report type and shared filters, views a paginated table, and exports the filtered result as CSV. Reports are live queries through simple application services and `/api/reports/{report}` endpoints. No pre-aggregation. Quantity-only: no stock value or margin. Vocabulary follows the Reports section of `CONTEXT.md`.

## Decisions

- Access: Admin and Manager only; Staff cannot see the page or call the endpoints.
- Shared filters: date range, Store (default all Stores), Brand and Category where relevant.
- CSV export of the full filtered set, no row cap. Table views paginated server-side (reuse the existing paging contract and `usePagedList`).
- Day boundaries use `Pacific/Auckland`, hard-coded (no timezone setting). Sales filter on completion time; movements on transaction timestamp.
- Low Stock Threshold: one global value in Settings (Admin edits), default 0. Needs a hand-written `db/NNN-*.sql` script; writing it is left to the user unless asked.
- Out of scope: stock value, margin, purchasing and transfer reports, PDF, per-variant reorder levels, charts other than one simple sales-over-time chart.
- Dashboard is unchanged.

## Reports

1. **Stock on Hand**: Inventory per variant per Store with quantity and a Low Stock flag.
2. **Stock Movement**: Inventory Transaction ledger. Filters: Store, product/variant, source type, user, date.
3. **Sales Summary**: Completed Sales by completion time. Voided Sales excluded from totals and shown as a separate count and amount. Shows GST-inclusive total, GST, ex-GST subtotal. Groupable by day, product, category, brand, Store.
4. **Stock Take Variance**: per Stock Take totals (shrinkage, overage) for Approved and Completed (marked pending) Stock Takes, with drill-down to per-variant Variance.

## User Stories

1. As a Manager, I want one Reports page with a report selector, so that I find every report in one place.
2. As a Staff member, I should not see Reports, so that aggregate data stays restricted.
3. As a Manager, I want to filter by Store, date range, Brand and Category, so that I can narrow results.
4. As a Manager, I want to see Low Stock variants against a threshold, so that I know what to reorder.
5. As an Admin, I want to set the Low Stock Threshold in Settings, so that "low" matches my business.
6. As a Manager, I want to browse the Inventory Transaction ledger with filters, so that I can trace where stock went.
7. As a Manager, I want Sales totals with GST breakdown and voids shown separately, so that figures reconcile.
8. As a Manager, I want Stock Take shrinkage and overage with drill-down, so that I can see where losses are.
9. As a Manager, I want to export any report to CSV, so that I can use it in Excel.
