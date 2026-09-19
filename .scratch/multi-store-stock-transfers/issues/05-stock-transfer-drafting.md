# 05: Stock Transfer drafting

**What to build:** A `Stock Transfer` can be created in Draft status specifying a source Store, a destination Store, and one or more product variant + quantity lines. Enforces the hub-and-spoke rule (one side must be the Warehouse Store) and validates line quantities against the source Store's current Inventory. Drafts can be edited and cancelled.

**Blocked by:** 02 (Store-aware Inventory & Goods Receiving)

**Status:** ready-for-agent

- [ ] Staff/Manager/Admin can create a Draft Stock Transfer with a source Store and a destination Store
- [ ] Creating a transfer where neither source nor destination is the Warehouse Store is rejected
- [ ] Creating a transfer where source and destination are the same Store is rejected
- [ ] Staff/Manager/Admin can add a line (product variant + quantity) to a Draft transfer
- [ ] Adding a line whose quantity exceeds the source Store's current Inventory for that variant is rejected
- [ ] At most one line per product variant per transfer is enforced (adding a second line for an already-present variant is rejected or merges — pick one and apply consistently)
- [ ] Lines can be edited and removed while the transfer is in `Draft`
- [ ] A Draft transfer can be cancelled (`Draft` → `Cancelled`)
- [ ] Stock Transfers can be listed, filterable by status, source Store, and destination Store
- [ ] A single Stock Transfer's full detail (including lines) can be retrieved by id
- [ ] No Inventory changes occur yet at this stage — Draft transfers have no effect on stock quantities anywhere
