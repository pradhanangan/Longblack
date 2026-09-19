# 06: Stock Transfer dispatch

**What to build:** A Manager or Admin can dispatch a Draft Stock Transfer. Dispatching reduces the source Store's Inventory for each line and records the transfer as in transit.

**Blocked by:** 05 (Stock Transfer drafting)

**Status:** ready-for-agent

- [ ] Manager/Admin can dispatch a Draft transfer, transitioning it to `Dispatched`
- [ ] Staff cannot dispatch a transfer (rejected/forbidden)
- [ ] Dispatching re-validates each line's quantity against the source Store's current Inventory at the moment of dispatch, and rejects the whole dispatch if any line fails
- [ ] Dispatching creates one `TransferOut` Inventory Transaction per line, reducing the source Store's Inventory for that variant by the dispatched quantity
- [ ] Once `Dispatched`, lines can no longer be added, edited, or removed
- [ ] Once `Dispatched`, the transfer can no longer be cancelled
- [ ] The destination Store's Inventory is unaffected until the transfer is received (ticket 07) — stock is "in transit" in between
