using Longblack.Domain.Catalogue;

namespace Longblack.Domain.Sales;

public class SaleLine
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    // Unique per (SaleId, ProductVariantId) — at most one line per variant per sale.
    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int Quantity { get; set; }

    // Snapshot of ProductVariant.SellingPrice (GST-inclusive) at the time the line was added.
    public decimal UnitPrice { get; set; }

    // Manual discount for the whole line (not per unit); never exceeds Quantity * UnitPrice.
    public decimal Discount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
