namespace Longblack.Application.Inventory;

public record InventoryDto(
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    string? ColourName,
    string? SizeName,
    string Status,
    int Quantity,
    DateTimeOffset? UpdatedAt);

public record ListInventoryFilter(
    string? SearchQuery,
    Guid? BrandId,
    Guid? CategoryId,
    // "All" means no status filter; null/empty defaults to Active; anything else is an exact match.
    string? Status);

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
