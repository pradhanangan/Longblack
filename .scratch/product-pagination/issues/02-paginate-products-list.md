# 02: Paginate the Products list

**What to build:** Browsing Products one page at a time. The Products list endpoint returns a paged envelope, the Products grid shows page controls and rows-per-page, and the variant search used by the Goods Receipt and Purchase Order forms keeps working against the new shape. See `.scratch/product-pagination/spec.md`.

**Blocked by:** 01 (Reusable paging contract and grid control).

**Status:** done

- [ ] `GET /api/products` accepts `page` and `pageSize` and returns `{ items, total, page, pageSize }`; ordered by Product code ascending with Id as tiebreaker; filtering, count and paging run in the database
- [ ] Products grid uses the shared control, shows the total, and resets to page 1 on any search, Brand, Category or Status change
- [ ] Empty, loading and error states unchanged; Add Product still works and the new Product appears in the list
- [ ] Variant search reads `items` and requests about 20 results; Goods Receipt and Purchase Order line pickers still find variants by SKU, barcode, name and code
- [ ] No caller of the list endpoint is left expecting a bare array
- [ ] Builds and lint pass; verified manually
