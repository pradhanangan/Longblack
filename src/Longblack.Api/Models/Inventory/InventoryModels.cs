namespace Longblack.Api.Models.Inventory;

public record InventoryResponse(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    int Quantity,
    DateTimeOffset UpdatedAt);

public record InventoryTransactionResponse(
    Guid Id,
    Guid ProductVariantId,
    string Type,
    int QuantityDelta,
    string SourceType,
    Guid SourceId,
    DateTimeOffset CreatedAt,
    string CreatedBy);
