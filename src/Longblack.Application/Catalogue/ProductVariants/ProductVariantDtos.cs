namespace Longblack.Application.Catalogue.ProductVariants;

public record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Barcode,
    Guid ColourId,
    string? ColourName,
    Guid SizeId,
    string? SizeName,
    decimal SellingPrice,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateProductVariantDto(
    string Sku,
    string? Barcode,
    Guid ColourId,
    Guid SizeId,
    decimal SellingPrice);

public record UpdateProductVariantDto(
    string? Barcode,
    Guid ColourId,
    Guid SizeId,
    decimal SellingPrice);
