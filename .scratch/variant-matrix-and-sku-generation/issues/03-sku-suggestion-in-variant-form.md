# 03: SKU suggestion in Add / Edit Variant form

**What to build:** When a Manager adds or edits a ProductVariant, selecting a Colour and a Size causes the SKU field to be pre-filled with a suggested value in the format `{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}` (e.g. `NIKE-TS-BLK-M`). The suggestion is computed entirely client-side using data already cached from previous API calls. The field remains fully editable.

**Blocked by:** 01 — Add Code field to Brands and Categories

**Status:** done

- [ ] Product detail page fetches and makes available the brand code and category code for the current product (the product response now includes these via the updated Brand/Category DTOs from ticket 01, or via the cached brands/categories data)
- [ ] In the Add Variant modal, when both Colour and Size are selected, the SKU field is pre-filled with `{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}` (all uppercased, hyphen-separated)
- [ ] The suggestion updates reactively when the user changes Colour or Size selection
- [ ] If the user has already typed in the SKU field, the field is not overwritten by a new suggestion (only pre-filled when the field is at its empty default)
- [ ] In the Edit Variant modal, the SKU field is read-only — no suggestion logic needed
- [ ] `npm run build` succeeds with no TypeScript errors
