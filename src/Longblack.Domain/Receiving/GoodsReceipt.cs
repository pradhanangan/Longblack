namespace Longblack.Domain.Receiving;

public class GoodsReceipt
{
    public Guid Id { get; set; }

    // DB-generated via a Postgres sequence; ReceiptNumber (e.g. "GR-0001") is derived from this at read time.
    public int SequenceNumber { get; set; }

    // Set server-side to the Warehouse Store's id at creation — never user-selectable.
    public Guid StoreId { get; set; }
    public Stores.Store? Store { get; set; }

    public Guid SupplierId { get; set; }
    public Catalogue.Supplier? Supplier { get; set; }

    // Optional: direct receiving without a Purchase Order stays valid.
    public Guid? PurchaseOrderId { get; set; }
    public Purchasing.PurchaseOrder? PurchaseOrder { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
    public string Status { get; set; } = GoodsReceiptStatus.Draft;

    // Set only when the receipt transitions to Received; always the acting user, never user-selectable.
    public string? ReceivedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}
