# 05: Add, Edit, Deactivate, and Reactivate Product

**What to build:** Full write capability for Products: Add Product modal (from the list page), Edit Product modal (from the detail page), Deactivate with confirmation, and Reactivate without confirmation.

**Blocked by:** 04 — Product detail page

**Status:** ready-for-agent

- [ ] "Add Product" button on the list page opens an MUI Dialog with a form: Product Code, Name, Description (optional), Brand (dropdown), Category (dropdown)
- [ ] "Edit Product" button on the detail page opens the same form pre-filled; Product Code field is read-only
- [ ] Both forms validated with Zod: Product Code required, Name required
- [ ] On submit, the correct API endpoint is called (`POST /api/products` or `PUT /api/products/:id`)
- [ ] A 409 Conflict response maps to an inline "Product Code already exists" error on the Product Code field
- [ ] On success: dialog closes, product list (and detail) TanStack Query cache is invalidated, success Snackbar shown
- [ ] "Deactivate" button on the detail page opens a confirmation dialog ("Are you sure you want to deactivate this product?"); confirming calls `PATCH /api/products/:id/status` with `{ status: "Inactive" }`
- [ ] "Reactivate" button (shown when product is Inactive) calls `PATCH /api/products/:id/status` with `{ status: "Active" }` immediately, no confirmation
- [ ] Success Snackbar shown after deactivate/reactivate; product detail cache invalidated
- [ ] Error Snackbar shown on any API failure
- [ ] Staff role sees no Add/Edit/Deactivate/Reactivate controls
- [ ] `npm run build` succeeds; `npm run lint` passes
