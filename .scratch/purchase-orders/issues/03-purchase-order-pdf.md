# 03: Purchase Order PDF

**What to build:** Download a PDF of an Issued Purchase Order, generated server-side with QuestPDF behind an abstraction so the same bytes can be emailed later. It shows company name and address (from a `Company` config section), PO number, date, Supplier details, lines (SKU, description, quantity, unit cost, line total), total and notes. No logo. See `.scratch/purchase-orders/spec.md`.

**Blocked by:** 02.

**Status:** done

- [ ] Check QuestPDF licence terms for this use and note the outcome
- [ ] PDF generator abstraction and QuestPDF implementation; `Company` settings in `appsettings` with placeholder values
- [ ] `GET` PDF endpoint, available only for non-Draft Purchase Orders
- [ ] Download PDF button on the detail screen
- [ ] Builds and lint pass; verified manually
