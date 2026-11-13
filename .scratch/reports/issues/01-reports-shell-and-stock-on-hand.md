Status: implemented (db/013 applied locally; manual UI check pending)

# Reports page shell and Stock on Hand

Add the `/reports` page (Admin/Manager only, sidebar entry hidden for Staff), a report-type selector, shared filters (Store, Brand, Category), and the Stock on Hand report with server-side pagination and CSV export. Includes the global Low Stock Threshold in Settings (Admin edit, default 0) and the Low Stock flag.

Blocked by: none

## Acceptance
- Staff get 403 from `/api/reports/*` and no nav entry.
- Stock on Hand lists variant, Store, quantity, Low Stock flag; filters work; CSV exports the full filtered set.
- Threshold editable by Admin in Settings; `db/` script left to the user unless asked.
