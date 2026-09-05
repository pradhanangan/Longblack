namespace Longblack.Application.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryDto>> ListAsync(CancellationToken ct = default);
    Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid productVariantId, CancellationToken ct = default);

    // Creates the Inventory row lazily on first use, applies the delta, and records the transaction.
    // Does not call SaveChangesAsync — callers compose this within their own unit of work.
    Task PostTransactionAsync(PostInventoryTransactionDto dto, CancellationToken ct = default);
}
