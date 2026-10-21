namespace Longblack.Domain.Purchasing;

public class PurchaseOrder
{
    public Guid Id { get; set; }

    // Null while Draft; assigned from purchase_order_seq on issue. PoNumber (e.g. "PO-0001") is derived from this at read time.
    public int? SequenceNumber { get; set; }

    public Guid SupplierId { get; set; }
    public Catalogue.Supplier? Supplier { get; set; }
    public DateTimeOffset? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }

    // Latest successful email send or manual mark-as-sent.
    public DateTimeOffset? SentAt { get; set; }
    public string? SentBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
    public ICollection<PurchaseOrderSend> Sends { get; set; } = [];
}
