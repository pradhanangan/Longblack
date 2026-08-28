# 03: Product list page

**What to build:** The `/products` page: a filterable, searchable list of Products that links to each Product's detail page. The list defaults to active products and updates reactively as filters change.

**Blocked by:** 02 — Login and auth flow

**Status:** done

- [ ] `/products` page renders an MUI table of Products with columns: Product Code, Name, Brand, Category, Status
- [ ] Filter bar includes: text search input (`q=`), Brand dropdown (from `GET /api/brands`), Category dropdown (from `GET /api/categories`), Status select (Active / Inactive / All — defaults to Active)
- [ ] Filters are applied reactively: the product list re-fetches as filters change (debounce text input)
- [ ] Brand and Category dropdowns populated via TanStack Query; cached with a long stale time
- [ ] Each table row links to `/products/:id`
- [ ] Empty state shown when no products match the current filters
- [ ] Loading state shown while fetching
- [ ] An "Add Product" button is visible for Manager and Admin roles (clicking it will open a modal — stubbed for now, wired in ticket 05)
- [ ] `npm run build` succeeds; `npm run lint` passes
