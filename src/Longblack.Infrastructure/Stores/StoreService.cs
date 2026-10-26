using Longblack.Application.Common.Exceptions;
using Longblack.Application.Common.Paging;
using Longblack.Application.Stores;
using Longblack.Domain.Stores;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Stores;

public class StoreService(AppDbContext db) : IStoreService
{
    public async Task<PagedResult<StoreDto>> ListAsync(string? status = null, PageRequest? page = null, CancellationToken ct = default)
    {
        var query = db.Stores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        return await query.OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToPagedResultAsync(page, ToDto, ct);
    }

    public async Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
        return store is null ? null : ToDto(store);
    }

    public async Task<StoreDto> CreateAsync(CreateStoreDto dto, string createdBy, CancellationToken ct = default)
    {
        if (dto.Type == StoreType.Warehouse)
        {
            var warehouseExists = await db.Stores.AnyAsync(s => s.Type == StoreType.Warehouse, ct);
            if (warehouseExists)
                throw new DuplicateException(nameof(Store), "type", StoreType.Warehouse);
        }

        var now = DateTimeOffset.UtcNow;
        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type,
            Status = StoreStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);

        return ToDto(store);
    }

    public async Task<StoreDto> UpdateAsync(Guid id, UpdateStoreDto dto, string updatedBy, CancellationToken ct = default)
    {
        var store = await RequireAsync(id, ct);

        store.Name = dto.Name;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(store);
    }

    public async Task<StoreDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var store = await RequireAsync(id, ct);

        if (status == StoreStatus.Closed && store.Status != StoreStatus.Closed)
        {
            // No special-casing for the Warehouse Store — same rule applies to every Store.
            var hasNonzeroInventory = await db.Inventory
                .AnyAsync(i => i.StoreId == id && i.Quantity != 0, ct);

            if (hasNonzeroInventory)
                throw new InvalidStateException(
                    $"Store '{store.Name}' still holds nonzero Inventory and cannot be closed.");
        }

        store.Status = status;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(store);
    }

    public async Task<Guid> GetWarehouseStoreIdAsync(CancellationToken ct = default)
    {
        var warehouse = await db.Stores.FirstOrDefaultAsync(s => s.Type == StoreType.Warehouse, ct)
            ?? throw new InvalidStateException("No Warehouse store exists — this should never happen after seeding.");

        return warehouse.Id;
    }

    private async Task<Store> RequireAsync(Guid id, CancellationToken ct) =>
        await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Store), id);

    private static StoreDto ToDto(Store s) =>
        new(s.Id, s.Name, s.Type, s.Status, s.CreatedAt, s.UpdatedAt, s.CreatedBy, s.UpdatedBy);
}
