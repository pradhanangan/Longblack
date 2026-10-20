namespace Longblack.Domain.Inventory;

public static class InventoryTransactionType
{
    public const string GoodsReceived = "GoodsReceived";
    public const string StockTakeAdjustment = "StockTakeAdjustment";
    public const string ManualAdjustment = "ManualAdjustment";

    // Posted at the source Store when a Stock Transfer is Dispatched.
    public const string TransferOut = "TransferOut";

    // Posted at the destination Store when a Stock Transfer is Received.
    public const string TransferIn = "TransferIn";

    // Posted at the Sale's Store when a Sale is Completed (negative delta).
    public const string Sale = "Sale";

    // Posted when a Completed Sale is Voided, reversing the original Sale transactions.
    public const string SaleVoid = "SaleVoid";
}
