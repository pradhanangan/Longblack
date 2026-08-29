# Variant Matrix and SKU Generation

Status: ready-for-agent

## Problem Statement

Creating products with many size/colour combinations is slow and error-prone today. A manager must create each ProductVariant individually, manually typing a SKU each time with no guidance on format. There is no standard SKU convention enforced across the catalogue, making it hard to look up or reason about a SKU without checking the system. Product codes are also fully manual with no suggested starting point.

## Solution

Two connected improvements:

1. **Suggested codes on creation**: when a Manager creates a Product, the system suggests a Product Code in the format `{BrandCode}-{CategoryCode}-{NNN}` (e.g. `NIKE-TS-001`). When a ProductVariant is created individually, the system suggests a SKU in the format `{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}` (e.g. `NIKE-TS-BLK-M`). Both are pre-filled but fully overridable before saving.

2. **Variant matrix generation**: a Manager can select a subset of Colours and Sizes and have the system generate all combinations as draft ProductVariants in one action — both during product creation and via a "Generate variants" button on the product detail page. Each generated variant gets a suggested SKU using the formula above, which the Manager can review and edit before saving.

To support these formulas, `Brand` and `Category` entities gain a `Code` field (short, user-supplied identifier, e.g. `NIKE`, `TS`).

## User Stories

1. As a Manager, I want each Brand to have a short Code (e.g. `NIKE`), so that it can be used in generated SKUs and Product Codes.
2. As an Admin, I want to supply a Code when creating or editing a Brand, so that the code reflects our conventions.
3. As an Admin, I want the Brand Code to be required and unique across all Brands, so that generated SKUs are unambiguous.
4. As a Manager, I want each Category to have a short Code (e.g. `TS`), so that it can be used in generated SKUs and Product Codes.
5. As an Admin, I want to supply a Code when creating or editing a Category, so that the code reflects our conventions.
6. As an Admin, I want the Category Code to be required and unique across all Categories, so that generated SKUs are unambiguous.
7. As a Manager, I want the "Add Product" form to pre-fill the Product Code field with a suggested value in the format `{BrandCode}-{CategoryCode}-{NNN}`, so that I have a sensible starting point without having to invent one.
8. As a Manager, I want the suggested Product Code to update when I change the Brand or Category selection, so that it always reflects the current choice.
9. As a Manager, I want to override the suggested Product Code before saving, so that I can use my own convention when needed.
10. As a Manager, I want the "Add Variant" form to pre-fill the SKU field with a suggested value in the format `{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}`, so that I have a consistent starting point.
11. As a Manager, I want the suggested SKU to update when I change the Colour or Size selection, so that it always reflects the current choice.
12. As a Manager, I want to override the suggested SKU before saving, so that I can use my own convention when needed.
13. As a Manager, I want to generate all colour/size combinations for a product in one action during product creation, so that I can set up a product's full range without creating variants one at a time.
14. As a Manager, I want to select which Colours and Sizes to include in the matrix before generating variants, so that I don't produce combinations that don't exist in stock.
15. As a Manager, I want to see a preview of the variants that will be generated (with their suggested SKUs) before committing, so that I can catch mistakes.
16. As a Manager, I want to edit any suggested SKU in the preview before saving, so that I can correct specific combinations.
17. As a Manager, I want to remove specific combinations from the preview before saving, so that I don't create variants I don't need.
18. As a Manager, I want to generate additional variants for an existing product via a "Generate variants" button on the product detail page, so that I can add new colours or sizes introduced mid-season.
19. As a Manager, I want the variant matrix generator to skip combinations where a matching SKU already exists, so that it doesn't produce duplicates.
20. As a Manager, I want to be warned if a generated SKU conflicts with an existing SKU in the system, so that I can resolve the conflict before saving.
21. As a Manager, I want all generated variants to be saved atomically (all or nothing), so that a partial failure doesn't leave the product in an inconsistent state.
22. As a Staff member, I want to see the Brand Code and Category Code on the brand and category list pages, so that I understand what the codes are.

## Implementation Decisions

### Schema changes

- Add `code` (text, not null, unique) to the `brands` table.
- Add `code` (text, not null, unique) to the `categories` table.
- Update the SQL DDL script (`001-catalogue-schema.sql`) with these columns and unique constraints.
- Update the seed script (`002-catalogue-seed.sql`) to populate codes for all seeded brands and categories (e.g. Brand "Generic" → `GEN`; Category "Men" → `MEN`, "T-Shirts" → `TS`, etc.).
- No EF migration — schema is SQL-owned; EF mappings are updated to include the new columns.

### SKU suggestion formula

`{BrandCode}-{CategoryCode}-{ColourCode}-{SizeCode}` — all uppercased, hyphen-separated.

Example: Brand `NIKE`, Category `TS`, Colour `BLK`, Size `M` → `NIKE-TS-BLK-M`.

Suggestion is computed client-side as the user selects values in the form. The suggested value is placed in the SKU input field as a pre-fill; the user may type over it.

### Product Code suggestion formula

`{BrandCode}-{CategoryCode}-{NNN}` where `NNN` is a zero-padded sequential integer.

The sequence is the count of existing Products with the same Brand + Category combination plus one (e.g. if 2 Nike T-Shirt products already exist, the next suggestion is `NIKE-TS-003`).

The sequence number is fetched from the API at the moment the Brand + Category combination is chosen. The suggested value is placed in the Product Code input field; the user may type over it.

New API endpoint: `GET /api/products/suggest-code?brandId={id}&categoryId={id}` — returns `{ suggestedCode: "NIKE-TS-003" }`. Authenticated, any role.

### Variant matrix generation

A "Generate variants" UI step is available:
1. **During product creation**: after the product fields are filled, a "Generate variants" optional step lets the user select Colours and Sizes, preview the Colour × Size matrix with suggested SKUs, edit/remove rows, then submit everything in one request.
2. **On the product detail page**: a "Generate variants" button opens the same matrix selector, excluding colour/size combinations for which an active variant already exists.

The matrix preview is built entirely client-side from the selected Colours × Sizes.

New API endpoint: `POST /api/products/{id}/variants/batch` — accepts an array of variant create requests and saves them atomically (all in a single database transaction). Returns the created variants. If any SKU or barcode already exists, the whole batch is rejected with a 409 and a list of conflicting SKUs.

### Brand and Category API changes

- `POST /api/brands` and `PUT /api/brands/{id}` now require and return `code`.
- `POST /api/categories` and `PUT /api/categories/{id}` now require and return `code`.
- `GET /api/brands` and `GET /api/categories` responses include `code`.
- Duplicate `code` returns 409 Conflict.

### Frontend changes

- Brand and Category forms (currently Admin-only, not yet built as screens) will need `code` fields when those screens are built; the dropdowns already show name only — no change needed to dropdowns.
- "Add Product" form: when Brand and Category are both selected, call `GET /api/products/suggest-code` and pre-fill Product Code.
- "Add Variant" form: when Colour and Size are both selected, compute the SKU suggestion client-side from the product's brand/category codes and the selected colour/size codes, and pre-fill the SKU field.
- New "Generate variants" step/dialog on both the Create Product flow and the Product Detail page.

## Testing Decisions

No automated tests in this spec (consistent with prior decision — manual testing against the running API). The `POST /api/products/{id}/variants/batch` endpoint is the most critical path to verify manually: test happy path (full matrix created), conflict detection (one SKU in the batch already exists → 409 for all), and the atomicity guarantee (database should have no partial inserts on failure).

## Out of Scope

- Enforcing the SKU formula — the system suggests it but does not validate that the user's final SKU matches the pattern.
- Auto-renaming SKUs when a Brand or Category code is changed.
- Admin screens for managing Brand and Category codes (codes are set via the existing Admin API endpoints; the frontend admin screens are a future ticket).
- Barcode generation.
- Importing variant matrices from CSV or external systems.

## Further Notes

- Brand Code and Category Code must be added to the seed data. Suggested codes for existing seed rows: Generic → `GEN`; Men → `MEN`, Women → `WMN`, Kids → `KDS`; T-Shirts → `TS`, Shirts → `SH`, Jeans → `JNS`, Shorts → `SHT`, Tops → `TOP`, Dresses → `DRS`, Boys → `BOYS`, Girls → `GRLS`.
- The `suggest-code` endpoint must handle the edge case where Brand or Category has no code yet (returns empty string so the product code field remains blank rather than generating a malformed suggestion).
- The batch variants endpoint should validate selling price (positive), colour/size existence, and product active status for every row before inserting any of them — fail-fast before the transaction begins.
