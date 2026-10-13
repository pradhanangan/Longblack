using Longblack.Domain.Catalogue;

namespace Longblack.Domain.StockTransfers;

public class StockTransferLine
{
    public Guid Id { get; set; }
    public Guid StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }

    // Unique per (StockTransferId, ProductVariantId) — at most one line per variant per transfer.
    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int DispatchedQuantity { get; set; }

    // Nullable specifically to distinguish "not yet received" (Draft/Dispatched) from "received,
    // quantity confirmed" (Received) — never defaulted to DispatchedQuantity at creation time.
    public int? ReceivedQuantity { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
