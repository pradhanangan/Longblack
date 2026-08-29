# 05: Variant matrix generator UI

**What to build:** A "Generate variants" flow available in two places — as an optional step during product creation and as a button on the product detail page. The user selects which Colours and Sizes apply to the product, sees a preview of all Colour × Size combinations with suggested SKUs, can edit or remove rows, then saves the whole matrix in one action via the batch API.

**Blocked by:** 03 — SKU suggestion in Add / Edit Variant form, 04 — Batch variant creation API

**Status:** ready-for-agent

- [ ] A "Generate variants" button appears on the product detail page (visible to Manager and Admin)
- [ ] Clicking it opens a multi-step dialog:
  - **Step 1 — Select colours and sizes**: two multi-select lists (all active Colours, all active Sizes); user picks which to include
  - **Step 2 — Preview and edit**: a table showing every Colour × Size combination with a pre-filled SKU (using the `{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}` formula from ticket 03), a Barcode field (optional), and a Selling Price field; each row has a remove button
  - **Step 3 — Confirm**: clicking "Generate" calls `POST /api/products/{id}/variants/batch` with all remaining rows
- [ ] When launched from the product detail page, colour/size combinations for which an active variant already exists are excluded from Step 1 (pre-deselected or hidden)
- [ ] The "Add Product" form offers the same "Generate variants" step after the product fields are filled (before the final Create action), so a Manager can set up variants without navigating to the detail page first
- [ ] SKU fields in the preview are editable; the user can type over a suggested value
- [ ] A 409 response from the batch endpoint is displayed as inline errors on the conflicting SKU rows in the preview, keeping the dialog open for the user to fix
- [ ] On success: dialog closes, variant list cache is invalidated, success Snackbar shown
- [ ] `npm run build` succeeds; `npm run lint` passes
