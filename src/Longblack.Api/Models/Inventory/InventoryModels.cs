namespace Longblack.Api.Models.Inventory;

public record InventoryResponse(
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    string? ColourName,
    string? SizeName,
    string Status,
    Guid StoreId,
    string StoreName,
    int Quantity,
    DateTimeOffset? UpdatedAt);

public record InventoryTransactionResponse(
    Guid Id,
    Guid ProductVariantId,
    Guid StoreId,
    string StoreName,
    string Type,
    int QuantityDelta,
    string SourceType,
    Guid SourceId,
    DateTimeOffset CreatedAt,
    string CreatedBy);
