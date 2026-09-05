namespace Longblack.Application.Inventory;

public record InventoryDto(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    int Quantity,
    DateTimeOffset UpdatedAt);

public record InventoryTransactionDto(
    Guid Id,
    Guid ProductVariantId,
    string Type,
    int QuantityDelta,
    string SourceType,
    Guid SourceId,
    DateTimeOffset CreatedAt,
    string CreatedBy);

// Raised internally by other services (e.g. Goods Receiving, and later Stock Take) to post
// a single ledger entry and keep the derived Inventory.Quantity in sync in the same operation.
public record PostInventoryTransactionDto(
    Guid ProductVariantId,
    string Type,
    int QuantityDelta,
    string SourceType,
    Guid SourceId,
    string PerformedBy);
