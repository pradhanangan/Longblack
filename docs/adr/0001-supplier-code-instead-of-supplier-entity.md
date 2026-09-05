# Use a free-text supplier code on Goods Receipt instead of a Supplier entity

The PRD (§6) specifies a full Supplier reference-data entity (code, name, email, phone, status) that Goods Receipt references by FK. For the MVP we are deferring that entity entirely: `GoodsReceipt` instead stores a free-text `SupplierCode` field, with no `suppliers` table and no FK.

We chose this to avoid building a CRUD module (and its authorization, search, and validation surface) that the MVP workflow doesn't strictly need — a receipt only needs to record *which* supplier stock came from, not manage supplier master data yet.

This is deliberately reversible but not free: introducing `Supplier` later means adding the table, backfilling/mapping existing `SupplierCode` values to `Supplier.Code`, and migrating `GoodsReceipt` from a string column to a FK. Until then, supplier codes are uncontrolled free text (no uniqueness or existence check).
