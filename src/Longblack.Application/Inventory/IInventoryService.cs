namespace Longblack.Application.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryDto>> ListAsync(ListInventoryFilter filter, CancellationToken ct = default);
    // Returns null only if the variant itself doesn't exist; a variant with no Inventory row yet
    // (never received) still returns a DTO with Quantity 0.
    Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid productVariantId, CancellationToken ct = default);

    // Creates the Inventory row lazily on first use, applies the delta, and records the transaction.
    // Does not call SaveChangesAsync — callers compose this within their own unit of work.
    Task PostTransactionAsync(PostInventoryTransactionDto dto, CancellationToken ct = default);
}
