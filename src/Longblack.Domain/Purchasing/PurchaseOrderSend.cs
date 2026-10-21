namespace Longblack.Domain.Purchasing;

// Immutable log of one email send attempt for a Purchase Order.
public class PurchaseOrderSend
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string SentBy { get; set; } = string.Empty;
}
