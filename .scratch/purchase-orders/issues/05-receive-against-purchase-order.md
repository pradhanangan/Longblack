# 05: Receive against a Purchase Order

**What to build:** A Goods Receipt can optionally reference one Issued or PartiallyReceived Purchase Order. It can be created from the Purchase Order with outstanding quantities and expected costs pre-filled. Marking it `Received` updates the Purchase Order to `PartiallyReceived` or `Received`. Direct receiving is unchanged. See `.scratch/purchase-orders/spec.md`.

**Blocked by:** 02.

**Status:** done

- [ ] Goods Receipt create/edit accepts an optional Purchase Order reference; Supplier must match
- [ ] Create-from-Purchase-Order pre-fills lines with outstanding quantity and expected cost; actual cost on the line stays authoritative
- [ ] Lines limited to variants on the Purchase Order; over-receipt returns a warning, not an error
- [ ] Marking a linked receipt `Received` recomputes Purchase Order status in the same database transaction, counting only `Received` receipts; safe under concurrent receipts
- [ ] Purchase Order detail shows ordered, received and outstanding per line and its linked Goods Receipts
- [ ] Goods Receipt screen has a PO picker and shows over-receipt warnings
- [ ] Direct receiving without a Purchase Order still works
- [ ] Builds and lint pass; verified manually
