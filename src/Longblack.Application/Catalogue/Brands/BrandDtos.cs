namespace Longblack.Application.Catalogue.Brands;

public record BrandDto(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateBrandDto(string Name);

public record UpdateBrandDto(string Name);
