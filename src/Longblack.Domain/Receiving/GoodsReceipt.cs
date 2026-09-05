namespace Longblack.Domain.Receiving;

public class GoodsReceipt
{
    public Guid Id { get; set; }

    // DB-generated via a Postgres sequence; ReceiptNumber (e.g. "GR-0001") is derived from this at read time.
    public int SequenceNumber { get; set; }

    // Free-text supplier identifier — no Supplier entity/FK for MVP. See ADR-0001.
    public string SupplierCode { get; set; } = string.Empty;
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
