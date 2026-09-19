# 03: Store closing guard

**What to build:** Closing a Store (`Active` → `Closed`) is rejected if it still holds nonzero Inventory for any product variant, so stock can never be silently stranded at a closed location.

**Blocked by:** 02 (Store-aware Inventory & Goods Receiving)

**Status:** ready-for-agent

- [ ] Admin can close a Store whose Inventory is entirely zero across all variants
- [ ] Admin attempting to close a Store with any nonzero Inventory row receives a clear rejection (not a silent no-op or a generic error)
- [ ] A closed Store's status is reflected in list/view responses
- [ ] Closing the Warehouse Store is allowed by the same rule (no special-casing) — in practice this will almost always be rejected since the Warehouse typically holds stock
