# 04: Product detail page (read-only)

**What to build:** The `/products/:id` page showing a Product's full details and its list of ProductVariants. Read-only at this point — action buttons are present but not yet wired to mutations.

**Blocked by:** 03 — Product list page

**Status:** ready-for-agent

- [ ] `/products/:id` fetches and displays: Product Code (read-only label), Name, Description, Brand name, Category name, Status
- [ ] A Variants section lists each ProductVariant with columns: SKU, Barcode, Colour, Size, Selling Price, Status
- [ ] Loading and error states handled for both product and variants fetches
- [ ] "Edit Product" button visible to Manager and Admin (stubbed — wired in ticket 05)
- [ ] "Deactivate / Reactivate" button visible to Manager and Admin (stubbed — wired in ticket 05)
- [ ] "Add Variant" button visible to Manager and Admin (stubbed — wired in ticket 06)
- [ ] Each Variant row has "Edit" and "Deactivate / Reactivate" actions visible to Manager and Admin (stubbed — wired in ticket 06)
- [ ] "Back to Products" navigation link
- [ ] `npm run build` succeeds; `npm run lint` passes
