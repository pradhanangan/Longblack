namespace Longblack.Domain.Inventory;

public class Inventory
{
    public Guid Id { get; set; }

    // Unique per variant — single-store MVP. A future location_id can be added without breaking this row shape.
    public Guid ProductVariantId { get; set; }
    public Catalogue.ProductVariant? ProductVariant { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
