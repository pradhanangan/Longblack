namespace Longblack.Domain.Catalogue;

// A null ColourId marks the Product Image (primary photo); otherwise it is the Colour Image
// shared by every Size of that Product and Colour.
public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? ColourId { get; set; }
    public Colour? Colour { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ThumbnailFileName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
