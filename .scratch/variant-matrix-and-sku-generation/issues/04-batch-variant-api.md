# 04: Batch variant creation API

**What to build:** A new API endpoint that accepts an array of variant create requests and saves them all atomically in a single database transaction. This is the backend that powers the variant matrix generator UI in ticket 05.

**Blocked by:** 01 — Add Code field to Brands and Categories

**Status:** ready-for-agent

- [ ] New endpoint: `POST /api/products/{id}/variants/batch` — accessible to Manager and Admin roles
- [ ] Request body is an array of variant objects, each with: `sku`, `barcode` (optional), `colourId`, `sizeId`, `sellingPrice`
- [ ] The endpoint validates all rows before inserting any: product must exist and be Active; all `colourId` and `sizeId` values must exist; all `sellingPrice` values must be positive
- [ ] If any SKU or barcode in the batch conflicts with an existing variant in the system, the entire batch is rejected with 409 Conflict and a response body listing all conflicting SKUs
- [ ] If validation passes and no conflicts exist, all variants are inserted in a single database transaction — all succeed or all fail
- [ ] On success, returns 201 Created with the array of created variant DTOs (same shape as `GET /api/products/{id}/variants` items)
- [ ] An empty batch array returns 400 Bad Request
- [ ] `dotnet build` succeeds with no errors
