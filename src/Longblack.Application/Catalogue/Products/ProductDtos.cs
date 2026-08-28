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
    string UpdatedBy);

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
