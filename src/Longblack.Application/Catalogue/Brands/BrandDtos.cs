namespace Longblack.Application.Catalogue.Brands;

public record BrandDto(
    Guid Id,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateBrandDto(string Name, string Code);

public record UpdateBrandDto(string Name, string Code);
