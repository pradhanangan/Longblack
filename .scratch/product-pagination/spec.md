Status: ready-for-agent

# Product list pagination

## Problem Statement

The Products grid loads every matching Product in one response and renders them all. As the catalogue grows the page gets slow and long, and the product list endpoint returns an ever larger payload that the variant search pickers also pay for.

## Solution

Paginate the Products list on the server. The grid shows one page at a time with page controls and a rows-per-page selector. Page, page size and filters live in the URL so Back from a Product returns to the same page. The variant search pickers (Goods Receipt, Purchase Order and others) request only a small page of results. A reusable pagination contract and grid control are introduced so other list grids can adopt them later.

## User Stories

1. As a Manager, I want the Products grid to show a page of Products at a time, so that the page loads quickly however large the catalogue is.
2. As a Staff user, I want the same paged Products grid, so that browsing is fast for every Role.
3. As a user, I want to move to next, previous, first and last pages, so that I can browse the whole catalogue.
4. As a user, I want to choose rows per page from 10, 25, 50 or 100, so that I can trade scrolling against page count.
5. As a user, I want 25 rows per page by default, so that the grid is sensible without any setup.
6. As a user, I want to see the total number of matching Products and which rows I am viewing, so that I know how big the result is.
7. As a user, I want changing the search text, Brand, Category or Status filter to return me to page 1, so that I never land on an empty page.
8. As a user, I want results ordered by Product code ascending, so that pages are stable and predictable.
9. As a user, I want the page, page size and filters kept in the URL, so that I can use Back from a Product and return to the same page.
10. As a user, I want to share or bookmark a filtered, paged Products URL, so that a colleague sees the same view.
11. As a user, I want a page number beyond the last page to show the last page (or the empty state), so that stale links do not break.
12. As a user, I want the existing empty message when no Products match, so that behaviour is unchanged.
13. As a user, I want loading and error states to behave as before, so that the grid stays familiar.
14. As a Manager, I want the Add Product action unchanged, so that creating Products still works and the new Product appears in the list.
15. As a user of the variant search on a Goods Receipt, Purchase Order or other form, I want results capped to a small page, so that the picker stays fast.
16. As a user of the variant search, I want searching by SKU, barcode, name or code to work as before, so that I find items the same way.
17. As an API consumer, I want a paged response with items, total, page and page size, so that I can render pagination controls.
18. As an API consumer, I want default page 1 and page size 25 when none are supplied, so that calls without paging parameters are safe.
19. As an API consumer, I want page size capped at 100 and out-of-range values clamped rather than rejected, so that clients cannot request unbounded results.
20. As a developer, I want a reusable pagination contract and grid control, so that other list grids can adopt paging with little work.

## Implementation Decisions

- Server-side pagination on the Products list endpoint. It accepts `page` and `pageSize` alongside the existing `q`, `brandId`, `categoryId` and `status` filters.
- Defaults: `page = 1`, `pageSize = 25`. `pageSize` is capped at 100. Values below 1 are clamped to the defaults. A `page` past the last page returns an empty `items` array with the correct `total`, and the grid steps back to the last page.
- Response changes from a bare array to a paged envelope: `items`, `total` (count of all matching Products), `page`, `pageSize`. This is a breaking change to the list endpoint, so every caller is updated together: the Products grid and the shared variant search used by the receiving and purchasing forms. The single-Product, suggest-code, variants and write endpoints are unchanged.
- Ordering is Product code ascending, with Id as a tiebreaker so pages are deterministic. Column sorting is not added.
- Filtering, search matching and the count run in the database; only the requested page of Products is loaded and mapped.
- A generic paged-result type and request parameters live in the shared application layer so other services can reuse them. Only the Product service adopts it in this spec.
- Frontend: a reusable paged-result type and a paginated grid control built on the existing MUI table pagination component, with rows-per-page options 10, 25, 50, 100 and default 25.
- The Products grid stores `page`, `pageSize` and the filters in URL query parameters. Any filter change resets `page` to 1. Search remains debounced as it is today.
- The variant search asks for a small page (about 20) and reads `items` from the envelope. It stays enabled only once the query is long enough, as today.
- No schema changes and no new domain terms; `CONTEXT.md` and the ADRs are unchanged.

## Testing Decisions

- No automated tests for now; this is deferred, as in the sales and purchase orders specs. Verify manually and via `dotnet build Longblack.slnx` and the frontend `npm run build` and `npm run lint`.
- A good test exercises external behavior only: HTTP requests to the list endpoint and the resulting envelope and ordering, never internal service calls.
- The single intended seam, when tests are added, is the HTTP surface (`GET /api/products`) via `WebApplicationFactory<Program>` against a real PostgreSQL database. Prior art: the seam described in `.scratch/sales/spec.md` and `.scratch/purchase-orders/spec.md`.
- Cases worth covering later: defaults, cap at 100, clamping of invalid values, `total` across filters, stable ordering across pages, page beyond the end.

## Out of Scope

- Paginating other grids (Purchase Orders, Goods Receipts, Sales, Stock Takes, Stock Transfers, Inventory, Suppliers). Follow-up work using the reusable contract.
- Clickable column sorting.
- Infinite scroll or "load more".
- Pagination of a Product's variants on the Product detail page.
- Backwards compatibility with the old bare-array response.

## Further Notes

- Seam confirmed with the user: HTTP, tests deferred.
- Any other consumer of the Products list endpoint must be updated in the same change; the frontend callers found are the Products grid and the shared variant search autocomplete.
- Related: `.scratch/purchase-orders/spec.md` (variant search is reused by Purchase Order lines).
