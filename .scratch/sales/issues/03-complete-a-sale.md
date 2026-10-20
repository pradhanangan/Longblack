# 03: Complete a Sale

**What to build:** Completing a Draft Sale decreases the Store's Inventory through `Sale` Inventory Transactions and makes the Sale immutable. Selling below zero is allowed but flagged. See `.scratch/sales/spec.md`.

**Blocked by:** 02 (Draft a Sale).

**Status:** done

- [ ] Add `Sale` to the Inventory Transaction type set; SQL script updates any type constraint if one exists
- [ ] Complete action posts one negative-delta `Sale` transaction per line at the Sale's Store, sourced to the Sale Line, in a single database transaction
- [ ] Payment amount must equal the Sale total to complete
- [ ] Completion response flags variants taken below zero; the UI shows the warning; completion is not blocked
- [ ] Completed Sales cannot be edited or cancelled
- [ ] Complete button on the Sale screen; transactions show in Inventory Transaction history
- [ ] Builds and lint pass; verified manually
