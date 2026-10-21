namespace Longblack.Domain.Purchasing;

public static class PurchaseOrderStatus
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Received = "Received";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}
