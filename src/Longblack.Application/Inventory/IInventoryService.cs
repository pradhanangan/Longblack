namespace Longblack.Application.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryDto>> ListAsync(ListInventoryFilter filter, CancellationToken ct = default);
    // Returns null only if the variant itself doesn't exist; a variant with no Inventory row yet
    // (never received/transferred in) still returns a DTO with Quantity 0. storeId null defaults
    // to the Warehouse Store.
    Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, Guid? storeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid productVariantId, CancellationToken ct = default);

    // Returns the current quantity at a specific Store (0 if no Inventory row exists yet there).
    Task<int> GetQuantityAsync(Guid productVariantId, Guid storeId, CancellationToken ct = default);

    // Creates the Inventory row lazily on first use, applies the delta, and records the transaction.
    // Does not call SaveChangesAsync — callers compose this within their own unit of work.
    Task PostTransactionAsync(PostInventoryTransactionDto dto, CancellationToken ct = default);
}
