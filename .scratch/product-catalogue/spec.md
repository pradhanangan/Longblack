# Product Catalogue

Status: ready-for-agent

## Problem Statement

The business needs to maintain a catalogue of the products it sells before it can receive stock, track inventory, or perform stock takes. Currently there is no way to define what products exist, what variants (SKUs) they come in, or the reference data (Brands, Categories, Colours, Sizes) those products depend on. Without this foundation, every other inventory workflow is blocked.

## Solution

Build the product catalogue feature: a set of API endpoints that allow authorised users to manage Brands, Categories, Colours, Sizes, Products, and ProductVariants. Reference data (Brands, Categories, Colours, Sizes) is seeded with sensible defaults at startup and is also manageable via Admin-only API endpoints. Products and ProductVariants are fully managed by Managers and Admins. Staff can read the catalogue but not modify it.

## User Stories

1. As an Admin, I want to create a Brand, so that Products can be tagged with the correct brand.
2. As an Admin, I want to edit a Brand's name, so that I can correct mistakes.
3. As an Admin, I want to deactivate a Brand, so that it no longer appears as an option for new Products without deleting historical data.
4. As an Admin, I want to list all Brands (active and inactive), so that I can review and manage the full set.
5. As an Admin, I want to create a root Category (no parent), so that I can define top-level groupings like "Men" or "Women".
6. As an Admin, I want to create a child Category under an existing Category, so that I can define sub-groupings like "Men > T-Shirts".
7. As an Admin, I want Category nesting to be unlimited in depth, so that the hierarchy can grow without constraint.
8. As an Admin, I want to edit a Category's name, so that I can correct mistakes.
9. As an Admin, I want to deactivate a Category, so that it no longer appears as an option for new Products.
10. As an Admin, I want to list all Categories including their parent relationships, so that I can understand the full hierarchy.
11. As an Admin, I want to create a Colour with a name and code, so that ProductVariants can be assigned a colour.
12. As an Admin, I want to edit a Colour, so that I can correct mistakes.
13. As an Admin, I want to deactivate a Colour, so that it no longer appears as an option for new ProductVariants.
14. As an Admin, I want to list all Colours, so that I can review and manage them.
15. As an Admin, I want to create a Size with a name, code, and sort order, so that ProductVariants can be assigned a size and sizes display in the correct order.
16. As an Admin, I want to edit a Size, so that I can correct mistakes.
17. As an Admin, I want to deactivate a Size, so that it no longer appears as an option for new ProductVariants.
18. As an Admin, I want to list all Sizes in sort-order sequence, so that I can review them as they will appear to users.
19. As a Manager, I want to create a Product with a code, name, description, brand, and category, so that it appears in the catalogue.
20. As a Manager, I want to edit a Product's details, so that I can correct mistakes or update information.
21. As a Manager, I want to activate or deactivate a Product, so that I can control whether it is available for new transactions.
22. As a Manager, I want to list Products with filtering by brand, category, and status, so that I can navigate large catalogues efficiently.
23. As a Manager, I want to search Products by name, product code, SKU, or barcode, so that I can find a specific item quickly.
24. As a Manager, I want to create a ProductVariant under a Product, assigning a SKU, barcode, colour, size, and selling price, so that the countable SKU exists in the system.
25. As a Manager, I want SKU to be unique across all ProductVariants, so that no two variants are ambiguous.
26. As a Manager, I want barcode to be unique across all ProductVariants where supplied, so that barcode scanning reliably identifies a single variant.
27. As a Manager, I want to edit a ProductVariant's details (barcode, colour, size, selling price), so that I can correct mistakes.
28. As a Manager, I want to activate or deactivate a ProductVariant, so that inactive variants cannot be used for new receiving transactions.
29. As a Manager, I want to list all ProductVariants for a given Product, so that I can see all available sizes and colours.
30. As a Manager, I want to be prevented from deleting a ProductVariant that has historical inventory or transaction data, so that audit history is preserved.
31. As a Staff member, I want to search Products by name, code, SKU, or barcode, so that I can look up stock information.
32. As a Staff member, I want to view a Product's variants, so that I can see what SKUs exist.
33. As a Staff member, I want to view a ProductVariant's details (SKU, colour, size, selling price), so that I can identify it correctly.

## Implementation Decisions

### Layer structure

Four projects already exist: `Longblack.Domain`, `Longblack.Application`, `Longblack.Infrastructure`, and `Longblack.Api`. All new domain entities go in `Longblack.Domain`. Application services (one per resource group) go in `Longblack.Application`. EF Core configuration and migrations go in `Longblack.Infrastructure`. API controllers and request/response models go in `Longblack.Api`.

### Domain entities

The following entities are introduced:

- **Brand** — id, name, status, created_at, updated_at, created_by, updated_by
- **Category** — id, parent_category_id (nullable FK to self), name, status, created_at, updated_at, created_by, updated_by
- **Colour** — id, name, code, status, created_at, updated_at, created_by, updated_by
- **Size** — id, name, code, sort_order, status, created_at, updated_at, created_by, updated_by
- **Product** — id, product_code (unique), name, description, brand_id (FK), category_id (FK), status, created_at, updated_at, created_by, updated_by
- **ProductVariant** — id, product_id (FK), sku (unique), barcode (unique, nullable), colour_id (FK), size_id (FK), selling_price, status, created_at, updated_at, created_by, updated_by

All `status` fields use string-backed enums defined in the domain. Reference data uses `Active`/`Inactive`. Products and ProductVariants use `Active`/`Inactive`.

### Category hierarchy

Categories use a self-referential `parent_category_id` FK (nullable) with no depth limit. The API returns the parent's id but does not expand the full tree recursively on list endpoints (clients traverse by following parent IDs). A dedicated tree endpoint may be added later.

### Variant attributes

`colour_id` and `size_id` are typed FK columns on `ProductVariant`, not a flexible attribute table. Both are required on creation.

### SKU ownership

SKUs are always user-supplied. The API validates uniqueness on create and on update (if the SKU is changed).

### Reference data seeding

Brands, Categories, Colours, and Sizes are seeded with a standard clothing retail set at application startup via the existing `DatabaseSeeder` mechanism. Seeding is idempotent (no-op if records already exist). The same records are also manageable via Admin-only endpoints.

### API routes

All routes follow `/api/{resource}` convention:

- `/api/brands` — GET (list), POST (Admin)
- `/api/brands/{id}` — GET, PUT (Admin), PATCH status (Admin)
- `/api/categories` — GET (list), POST (Admin)
- `/api/categories/{id}` — GET, PUT (Admin), PATCH status (Admin)
- `/api/colours` — GET (list), POST (Admin)
- `/api/colours/{id}` — GET, PUT (Admin), PATCH status (Admin)
- `/api/sizes` — GET (list), POST (Admin)
- `/api/sizes/{id}` — GET, PUT (Admin), PATCH status (Admin)
- `/api/products` — GET (list + search), POST (Manager, Admin)
- `/api/products/{id}` — GET, PUT (Manager, Admin), PATCH status (Manager, Admin)
- `/api/products/{id}/variants` — GET (list), POST (Manager, Admin)
- `/api/products/{id}/variants/{variantId}` — GET, PUT (Manager, Admin), PATCH status (Manager, Admin)

### Authorization

- Staff: read-only access to Products and ProductVariants only
- Manager: full CRUD on Products and ProductVariants; read-only on reference data
- Admin: full CRUD on everything including reference data

### Soft deletion via status

No hard deletes. Deactivation sets `status = Inactive`. A ProductVariant with historical inventory or transaction data cannot be hard-deleted (enforced at the application service layer by checking for linked records before any delete attempt).

### Database schema

Tables use snake_case names and snake_case column names, consistent with the existing Identity table convention. All transactional entities carry audit fields (`created_by`, `created_at`, `updated_by`, `updated_at`).

### Migrations

EF Core migrations are used. The migration command is:
`dotnet ef migrations add <Name> --project src/Longblack.Infrastructure --startup-project src/Longblack.Api`

## Testing Decisions

### What makes a good test

Tests assert external, observable behaviour — what the HTTP API returns or what state the database is in after a request. Tests do not assert which application service methods were called, which repository methods were invoked, or any other implementation detail. A test should survive an internal refactor without changing.

### Test seam

A single seam: **HTTP via `WebApplicationFactory<Program>`**. A new `Longblack.Api.Tests` project will be created. Each test spins up the full ASP.NET Core pipeline against a real PostgreSQL test database. No mocking of application services, domain, or infrastructure.

### What is tested

- Creating, editing, and deactivating each reference data type (Brand, Category, Colour, Size)
- Creating a Product, editing it, changing its status
- Creating a ProductVariant, editing it, changing its status
- SKU uniqueness enforcement (second variant with same SKU is rejected)
- Barcode uniqueness enforcement where supplied
- Search by name, code, SKU, barcode
- Role-based access: Staff cannot create/edit; Manager cannot manage reference data; Admin can do everything
- Deactivated items do not appear in default list responses (only when status filter includes Inactive)

### Prior art

No existing tests in the codebase. The `AuthController` and `DatabaseSeeder` provide patterns for how auth and seeding work; these can be used to set up authenticated test requests.

## Out of Scope

- Goods Receiving, Inventory, Stock Takes (future specs)
- Purchase Orders
- Supplier management
- Price history or multiple selling prices
- Promotions or store-specific pricing
- Multi-store / location support
- Product images or media
- Import/export of catalogue data
- Frontend / React UI (this spec covers the API only)
- Soft-delete recovery endpoints (reactivation is via PATCH status)
- Full category tree endpoint (returning all nested children recursively)

## Further Notes

- The `selling_price` on `ProductVariant` records the current selling price only. Historical price changes are out of scope for MVP.
- The `purchase cost` of a variant is **not** stored here — it lives on `GoodsReceiptLine` (future spec).
- Reference data seeded at startup should cover common clothing retail defaults: sizes XS–3XL, basic colours, a starter set of brands and categories (Men, Women, Kids at the root level).
- The `product_code` field on `Product` must be unique and is user-supplied.
