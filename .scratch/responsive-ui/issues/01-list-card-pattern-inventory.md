# 01: Mobile list pages: shared card layout and Inventory list

**What to build:** A reusable responsive list pattern: below `sm` each row renders as a card, at `sm` and above it stays a table. The Inventory list adopts it first. Filters and toolbars wrap on narrow screens, and pagination fits at 375px.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Shared pattern is reusable by other list pages without copy-paste
- [x] Inventory list shows cards at 360-599px and the table at 600px+
- [x] No horizontal page scroll at 360px; filters and pagination usable
- [x] `npm run lint` and `npm run build` pass
