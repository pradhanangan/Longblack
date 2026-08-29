# 01: Add Code field to Brands and Categories

**What to build:** Brands and Categories gain a required, unique `code` field (short identifier, e.g. `NIKE`, `TS`). The SQL schema, seed data, backend API, and EF mappings are all updated so that `code` is returned on every read and required on every write. This is the foundation all other tickets in this feature depend on.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] SQL DDL script updated: `code` column (text, not null) added to `brands` and `categories` tables with a unique constraint on each
- [ ] Seed script updated with codes for all seeded rows: Generic → `GEN`; Men → `MEN`, Women → `WMN`, Kids → `KDS`; T-Shirts → `TS`, Shirts → `SH`, Jeans → `JNS`, Shorts → `SHT`, Tops → `TOP`, Dresses → `DRS`, Boys → `BOYS`, Girls → `GRLS`
- [ ] `Brand` domain entity and `BrandDto` include `code`
- [ ] `Category` domain entity and `CategoryDto` include `code`
- [ ] EF mappings for both entities map the `code` column
- [ ] `CreateBrandDto` / `UpdateBrandDto` require `code`; `CreateCategoryDto` / `UpdateCategoryDto` require `code`
- [ ] `BrandService` and `CategoryService` validate uniqueness of `code` on create and update; duplicate returns `DuplicateException` → 409 Conflict
- [ ] `GET /api/brands`, `GET /api/brands/{id}` responses include `code`
- [ ] `POST /api/brands`, `PUT /api/brands/{id}` require `code` in request body
- [ ] `GET /api/categories`, `GET /api/categories/{id}` responses include `code`
- [ ] `POST /api/categories`, `PUT /api/categories/{id}` require `code` in request body
- [ ] `dotnet build` succeeds with no errors
