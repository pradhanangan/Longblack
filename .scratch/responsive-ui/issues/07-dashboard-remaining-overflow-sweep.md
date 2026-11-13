# 07: Mobile Dashboard, remaining detail pages, and overflow sweep

**What to build:** Dashboard and the Purchase Order, Product, Inventory and Store detail pages are usable on phones. Reports and Settings use a horizontal-scroll fallback for wide tables. A final pass confirms no page overflows horizontally at 360px.

**Blocked by:** 02, 03, 04, 05, 06.

**Status:** done

- [x] Dashboard and listed detail pages usable at 360px
- [x] Reports and Settings do not break the page layout (wide tables scroll inside their container)
- [x] Every page checked at 360, 768 and 1280px with no page-level horizontal scroll
- [x] `npm run lint` and `npm run build` pass
