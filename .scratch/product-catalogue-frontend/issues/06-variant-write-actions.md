# 06: Add, Edit, Deactivate, and Reactivate ProductVariant

**What to build:** Full write capability for ProductVariants on the product detail page: Add Variant modal, Edit Variant modal, Deactivate with confirmation, and Reactivate without confirmation.

**Blocked by:** 05 — Add, Edit, Deactivate, and Reactivate Product

**Status:** ready-for-agent

- [ ] "Add Variant" button on the product detail page opens an MUI Dialog with a form: SKU, Barcode (optional), Colour (dropdown from `GET /api/colours`), Size (dropdown from `GET /api/sizes`), Selling Price
- [ ] "Edit Variant" row action opens the same form pre-filled; SKU field is read-only
- [ ] Form validated with Zod: SKU required (Add only), Colour required, Size required, Selling Price required and must be a positive number
- [ ] On submit, the correct endpoint is called (`POST /api/products/:id/variants` or `PUT /api/products/:id/variants/:vid`)
- [ ] A 409 Conflict response for duplicate SKU maps to an inline "SKU already exists" error on the SKU field
- [ ] A 409 Conflict response for duplicate barcode maps to an inline "Barcode already exists" error on the Barcode field
- [ ] On success: dialog closes, variant list cache invalidated, success Snackbar shown
- [ ] "Deactivate" row action opens a confirmation dialog; confirming calls `PATCH /api/products/:id/variants/:vid/status` with `{ status: "Inactive" }`
- [ ] "Reactivate" row action (shown when variant is Inactive) calls the same endpoint with `{ status: "Active" }` immediately, no confirmation
- [ ] Inactive variants visible in the variant list (with visual distinction — e.g. greyed row or "Inactive" badge)
- [ ] Success Snackbar shown after all write actions; error Snackbar on API failure
- [ ] Staff role sees no Add/Edit/Deactivate/Reactivate controls
- [ ] `npm run build` succeeds; `npm run lint` passes
