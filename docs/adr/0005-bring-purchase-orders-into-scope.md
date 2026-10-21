# Bring Purchase Orders into scope

The PRD (§2.2, §10) and repo instructions list Purchase Orders as out of scope for the MVP, a "future capability" with only an optional `purchase_order_id` reserved on `GoodsReceipt`. We are deliberately bringing them into scope.

Decisions:

- `PurchaseOrder` and `PurchaseOrderLine` become real entities. A PO targets exactly one Supplier and implicitly the Warehouse Store (no store field, so it cannot drift from the rule that Goods Receipts only target the Warehouse).
- Statuses: `Draft` → `Issued` → `PartiallyReceived` → `Received`, plus `Closed` and `Cancelled`. `PartiallyReceived`/`Received` are derived from linked `Received` Goods Receipts; `Closed` is a manual force-close for short shipments.
- The PO number (`PO-0001`) is assigned on `Issued`, and lines are locked from then on. Corrections are cancel-and-recreate, mirroring the immutable `Received` Goods Receipt rule.
- "Sent" is a timestamp (`SentAt`/`SentBy`) on an `Issued` PO, not a status, so receiving never depends on whether the email went out. Each send is logged. This brings PDF generation (server-side, QuestPDF) and outbound email (an `IEmailSender` abstraction, SMTP/provider config via secrets) into scope. Neither exists in the system today.
- `GoodsReceipt.PurchaseOrderId` is optional. Direct receiving without a PO stays fully supported. When set, the Supplier must match, lines are limited to variants on the PO, and over-receipt warns rather than blocks.
- Purchase cost stays on `GoodsReceiptLine`. The PO line's unit cost is expected cost only and pre-fills the receipt line.
- Manager/Admin create and issue; only Admin cancels an issued PO; Staff read-only. No approval workflow.

Still out of scope: reorder suggestions, tax, freight, multi-currency, supplier-specific cost lists.

Why: the business wants to order stock and email suppliers from the system, and receive against what was ordered. Alternatives considered were keeping POs as a pure future placeholder (rejected: the business needs them now) and a `Sent` status (rejected: couples receiving to an email side effect).

Hard to reverse: new tables, a nullable FK on `goods_receipts`, a new outbound email dependency, and a PO-number sequence that, once issued to suppliers, cannot be renumbered.
