# 03: Keep Products grid state in the URL

**What to build:** The Products grid's page, page size and filters live in the URL, so Back from a Product returns to the same page and a filtered, paged view can be shared or bookmarked. See `.scratch/product-pagination/spec.md`.

**Blocked by:** 02 (Paginate the Products list).

**Status:** done

- [ ] Page, page size, search, Brand, Category and Status are read from and written to URL query parameters
- [ ] Opening a Product and pressing Back returns to the same page with the same filters
- [ ] A URL with a page past the last page steps back to the last page
- [ ] Changing any filter resets the page to 1 in the URL; search stays debounced
- [ ] A URL with no parameters shows page 1, 25 rows, Status Active as today
- [ ] Builds and lint pass; verified manually
