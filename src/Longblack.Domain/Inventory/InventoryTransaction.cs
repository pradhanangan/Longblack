namespace Longblack.Domain.Inventory;

// An immutable ledger entry — never updated after creation, so unlike other transactional
// entities in this codebase it intentionally has no UpdatedAt/UpdatedBy.
public class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public Catalogue.ProductVariant? ProductVariant { get; set; }
    public string Type { get; set; } = string.Empty;
    public int QuantityDelta { get; set; }

    // Points back to the record that caused this change, e.g. "GoodsReceiptLine" + its Id.
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
