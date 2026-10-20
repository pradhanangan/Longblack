# 03: Complete a Sale

**What to build:** Completing a Draft Sale decreases the Store's Inventory through `Sale` Inventory Transactions and makes the Sale immutable. Selling more than the available stock is rejected. See `.scratch/sales/spec.md`.

**Blocked by:** 02 (Draft a Sale).

**Status:** done

- [ ] Add `Sale` to the Inventory Transaction type set; SQL script updates any type constraint if one exists
- [ ] Complete action posts one negative-delta `Sale` transaction per line at the Sale's Store, sourced to the Sale Line, in a single database transaction
- [ ] Payment amount must equal the Sale total to complete
- [ ] Line quantity cannot exceed the Store's available Inventory (checked on add/edit and re-checked on completion); the variant search offers only in-stock variants
- [ ] Completed Sales cannot be edited or cancelled
- [ ] Complete button on the Sale screen; transactions show in Inventory Transaction history
- [ ] Builds and lint pass; verified manually
