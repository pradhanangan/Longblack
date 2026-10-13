namespace Longblack.Application.Inventory;

public record InventoryDto(
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

public record ListInventoryFilter(
    string? SearchQuery,
    Guid? BrandId,
    Guid? CategoryId,
    // "All" means no status filter; null/empty defaults to Active; anything else is an exact match.
    string? Status,
    // Null defaults to the Warehouse Store — preserves pre-multi-store behaviour for existing callers.
    Guid? StoreId);

public record InventoryTransactionDto(
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

// Raised internally by other services (Goods Receiving, Stock Take, Stock Transfer) to post
// a single ledger entry and keep the derived Inventory.Quantity in sync in the same operation.
public record PostInventoryTransactionDto(
    Guid ProductVariantId,
    Guid StoreId,
    string Type,
    int QuantityDelta,
    string SourceType,
    Guid SourceId,
    string PerformedBy);
