using Longblack.Application.Catalogue.ProductVariants;

namespace Longblack.Application.Catalogue.Products;

public record ProductDto(
    Guid Id,
    string ProductCode,
    string Name,
    string? Description,
    Guid? BrandId,
    string? BrandName,
    Guid? CategoryId,
    string? CategoryName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy)
{
    // Populated only during search (q=) so the caller can identify which variant matched.
    public IReadOnlyList<ProductVariantDto>? Variants { get; init; }
}

public record CreateProductDto(
    string ProductCode,
    string Name,
    string? Description,
    Guid? BrandId,
    Guid? CategoryId);

public record UpdateProductDto(
    string Name,
    string? Description,
    Guid? BrandId,
    Guid? CategoryId);

public record ListProductsFilter(
    Guid? BrandId,
    Guid? CategoryId,
    string? Status,
    string? SearchQuery);
