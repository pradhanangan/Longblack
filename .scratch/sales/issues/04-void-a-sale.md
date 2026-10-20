# 04: Void a Sale

**What to build:** A Manager or Admin can void a Completed Sale, restoring stock via reversing `SaleVoid` Inventory Transactions. Staff cannot void. See `.scratch/sales/spec.md`.

**Blocked by:** 03 (Complete a Sale).

**Status:** done

- [ ] Add `SaleVoid` to the Inventory Transaction type set; SQL script updated if needed
- [ ] Void action moves the Sale to `Voided`, records who and when, and posts a positive reversing transaction per line, in a single database transaction
- [ ] Only Manager and Admin can void (Staff gets 403); only Completed Sales can be voided; a Voided Sale is terminal
- [ ] Void button on the Sale screen is shown only to Manager and Admin
- [ ] Builds and lint pass; verified manually
