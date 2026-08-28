namespace Longblack.Domain.Catalogue;

public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public Guid ColourId { get; set; }
    public Colour? Colour { get; set; }
    public Guid SizeId { get; set; }
    public Size? Size { get; set; }
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = ReferenceDataStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
