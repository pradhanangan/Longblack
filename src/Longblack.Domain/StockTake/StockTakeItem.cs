using Longblack.Domain.Catalogue;

namespace Longblack.Domain.StockTake;

public class StockTakeItem
{
    public Guid Id { get; set; }
    public Guid StockTakeId { get; set; }
    public Longblack.Domain.StockTake.StockTake? StockTake { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Snapshotted from Inventory.Quantity at the Draft -> InProgress transition (see ADR-0002).
    public int ExpectedQuantity { get; set; }

    // Mirrors the latest StockTakeCount for this item, kept in sync each time a count is recorded.
    // Null until the item has been counted at least once.
    public int? CountedQuantity { get; set; }

    public string Status { get; set; } = StockTakeItemStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<StockTakeCount> Counts { get; set; } = [];
}
