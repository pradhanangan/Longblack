# 07: Stock Transfer receive

**What to build:** A Manager or Admin can receive a Dispatched Stock Transfer, recording the actual received quantity per line — which may differ from what was dispatched (e.g. loss or damage in transit). Receiving increases the destination Store's Inventory by the received quantity and exposes any variance.

**Blocked by:** 06 (Stock Transfer dispatch)

**Status:** ready-for-agent

- [ ] Manager/Admin can receive a Dispatched transfer, supplying a received quantity per line
- [ ] Staff cannot receive a transfer (rejected/forbidden)
- [ ] Receiving creates one `TransferIn` Inventory Transaction per line, increasing the destination Store's Inventory for that variant by the received quantity (not the dispatched quantity)
- [ ] A received quantity lower than the dispatched quantity is accepted (not rejected) and the resulting variance (dispatched − received) is visible when viewing the transfer
- [ ] A received quantity equal to the dispatched quantity is the common case and produces zero variance
- [ ] Receiving transitions the transfer to `Received`
- [ ] A `Received` transfer is immutable — no further edits, dispatch, receive, or cancel actions are possible
- [ ] Viewing a Received transfer shows both dispatched and received quantities per line
