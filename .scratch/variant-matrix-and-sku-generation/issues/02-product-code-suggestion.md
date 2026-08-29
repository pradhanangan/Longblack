# 02: Product Code suggestion

**What to build:** When a Manager opens the "Add Product" form and selects both a Brand and a Category, the system suggests a Product Code in the format `{BrandCode}-{CategoryCode}-{NNN}` (e.g. `NIKE-TS-003`) and pre-fills the Product Code field. The field remains fully editable. The sequence number is the count of existing Products with that Brand + Category combination plus one.

**Blocked by:** 01 — Add Code field to Brands and Categories

**Status:** ready-for-agent

- [ ] New API endpoint: `GET /api/products/suggest-code?brandId={id}&categoryId={id}` — returns `{ "suggestedCode": "NIKE-TS-003" }` for authenticated users of any role
- [ ] Endpoint counts existing products with the given brand + category and computes the next zero-padded sequence number (e.g. 3 existing → suggest `004`)
- [ ] If either `brandId` or `categoryId` is missing or the brand/category has no code, the endpoint returns `{ "suggestedCode": "" }` rather than a malformed suggestion
- [ ] "Add Product" form calls `GET /api/products/suggest-code` when both Brand and Category dropdowns have a value selected
- [ ] The returned suggestion is placed in the Product Code input field as a pre-fill
- [ ] The suggested code updates if the user changes Brand or Category selection
- [ ] If the user has already typed in the Product Code field, the field is not overwritten by a new suggestion (only pre-filled when the field is still at its default empty value)
- [ ] `dotnet build` succeeds; frontend `npm run build` succeeds
