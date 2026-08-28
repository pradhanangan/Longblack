namespace Longblack.Api.Models.Catalogue;

public record CreateProductVariantRequest(
    string Sku,
    string? Barcode,
    Guid ColourId,
    Guid SizeId,
    decimal SellingPrice);

public record UpdateProductVariantRequest(
    string? Barcode,
    Guid ColourId,
    Guid SizeId,
    decimal SellingPrice);

public record ProductVariantResponse(
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
