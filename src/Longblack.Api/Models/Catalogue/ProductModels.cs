namespace Longblack.Api.Models.Catalogue;

public record CreateProductRequest(
    string ProductCode,
    string Name,
    string? Description,
    Guid? BrandId,
    Guid? CategoryId);

public record UpdateProductRequest(
    string Name,
    string? Description,
    Guid? BrandId,
    Guid? CategoryId);

public record ProductResponse(
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
