# 04: Store-scoped Stock Take

**What to build:** A Stock Take is always scoped to exactly one Store. Creating a Stock Take requires selecting a Store, and the Expected Quantity snapshot and any posted adjustments are scoped to that Store's Inventory.

**Blocked by:** 02 (Store-aware Inventory & Goods Receiving)

**Status:** ready-for-agent

- [ ] Creating a Stock Take requires a `storeId` referencing an Active Store
- [ ] Starting a Stock Take (`Draft` → `InProgress`) snapshots each item's Expected Quantity from that Store's Inventory only, not any other store's
- [ ] Approving a Stock Take posts `StockTakeAdjustment` Inventory Transactions against that Stock Take's Store
- [ ] Two Stock Takes for the same scope (Brand/Category filter) but different Stores produce independent Expected Quantities and independent adjustments
- [ ] Existing Stock Take lifecycle (start/count/complete/reopen/approve/cancel) and role-based permissions are otherwise unchanged
