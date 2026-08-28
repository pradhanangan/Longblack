namespace Longblack.Domain.Catalogue;

public class Product
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Status { get; set; } = ReferenceDataStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public ICollection<ProductVariant> Variants { get; set; } = [];
}
