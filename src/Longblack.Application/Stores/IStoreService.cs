namespace Longblack.Application.Stores;

public interface IStoreService
{
    Task<IReadOnlyList<StoreDto>> ListAsync(string? status = null, CancellationToken ct = default);
    Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Rejects a second Type = Warehouse Store (exactly one must ever exist).
    Task<StoreDto> CreateAsync(CreateStoreDto dto, string createdBy, CancellationToken ct = default);
    Task<StoreDto> UpdateAsync(Guid id, UpdateStoreDto dto, string updatedBy, CancellationToken ct = default);

    // Active -> Closed: rejected if any Inventory row for this Store is nonzero (no special-casing
    // for the Warehouse Store — same rule applies).
    Task<StoreDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);

    // Resolves the single seeded Warehouse Store's id — used by Goods Receiving, Inventory
    // defaulting, and the Stock Transfer hub-and-spoke rule.
    Task<Guid> GetWarehouseStoreIdAsync(CancellationToken ct = default);
}
