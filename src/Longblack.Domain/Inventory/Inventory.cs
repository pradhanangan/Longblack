namespace Longblack.Domain.Inventory;

public class Inventory
{
    public Guid Id { get; set; }

    // Unique per (ProductVariantId, StoreId) — one row per variant per Store.
    public Guid ProductVariantId { get; set; }
    public Catalogue.ProductVariant? ProductVariant { get; set; }
    public Guid StoreId { get; set; }
    public Stores.Store? Store { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
