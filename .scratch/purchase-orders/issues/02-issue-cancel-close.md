# 02: Issue, cancel and close a Purchase Order

**What to build:** A Manager or Admin can issue a Draft Purchase Order: it needs at least one line, receives its `PO-0001`-style number and its lines lock. An Admin can cancel an Issued Purchase Order while nothing has been received; Manager/Admin can force-close an Issued or PartiallyReceived one. See `.scratch/purchase-orders/spec.md`.

**Blocked by:** 01.

**Status:** done

- [ ] Issue action: rejects empty Purchase Orders, assigns the number (sequence, following the Goods Receipt numbering approach), locks lines and header edits
- [ ] Cancel Issued: Admin only, rejected once anything is received
- [ ] Close: Manager/Admin, from Issued or PartiallyReceived
- [ ] Invalid transitions return the existing invalid-state error
- [ ] Detail screen shows status and role-gated Issue, Cancel and Close actions
- [ ] Builds and lint pass; verified manually
