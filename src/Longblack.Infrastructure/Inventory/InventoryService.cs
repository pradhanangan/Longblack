using Longblack.Application.Inventory;
using Longblack.Application.Common.Paging;
using Longblack.Application.Stores;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Inventory;

public class InventoryService(AppDbContext db, IStoreService storeService) : IInventoryService
{
    public async Task<PagedResult<InventoryDto>> ListAsync(ListInventoryFilter filter, PageRequest? page = null, CancellationToken ct = default)
    {
        var storeId = filter.StoreId ?? await storeService.GetWarehouseStoreIdAsync(ct);

        var query = db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .AsQueryable();

        if (filter.Status == "All")
        {
            // no status filter
        }
        else if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(v => v.Status == filter.Status);
        }
        else
        {
            query = query.Where(v => v.Status == ReferenceDataStatus.Active);
        }

        if (filter.BrandId is not null)
            query = query.Where(v => v.Product!.BrandId == filter.BrandId);

        if (filter.CategoryId is not null)
            query = query.Where(v => v.Product!.CategoryId == filter.CategoryId);

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var q = filter.SearchQuery.ToLower();
            query = query.Where(v =>
                v.Sku.ToLower().Contains(q) ||
                (v.Barcode != null && v.Barcode.ToLower().Contains(q)) ||
                v.Product!.Name.ToLower().Contains(q) ||
                v.Product!.ProductCode.ToLower().Contains(q));
        }

        var variants = await query.OrderBy(v => v.Sku).ThenBy(v => v.Id)
            .ToPagedResultAsync(page, v => v, ct);

        var store = await db.Stores.FirstAsync(s => s.Id == storeId, ct);

        // Left-join semantics: fetch existing Inventory rows for these variants at this Store and
        // default to zero for any variant that has never had one created there.
        var variantIds = variants.Items.Select(v => v.Id).ToList();
        var inventoryByVariantId = await db.Inventory
            .Where(i => i.StoreId == storeId && variantIds.Contains(i.ProductVariantId))
            .ToDictionaryAsync(i => i.ProductVariantId, i => i, ct);

        return new PagedResult<InventoryDto>(variants.Items
            .Select(v => ToDto(v, store, inventoryByVariantId.GetValueOrDefault(v.Id)))
            .ToList(), variants.Total, variants.Page, variants.PageSize);
    }

    public async Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, Guid? storeId = null, CancellationToken ct = default)
    {
        var resolvedStoreId = storeId ?? await storeService.GetWarehouseStoreIdAsync(ct);

        var variant = await db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == productVariantId, ct);

        if (variant is null)
            return null;

        var store = await db.Stores.FirstAsync(s => s.Id == resolvedStoreId, ct);
        var inventory = await db.Inventory
            .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId && i.StoreId == resolvedStoreId, ct);

        return ToDto(variant, store, inventory);
    }

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid productVariantId, CancellationToken ct = default)
    {
        return await db.InventoryTransactions
            .Include(t => t.Store)
            .Where(t => t.ProductVariantId == productVariantId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new InventoryTransactionDto(
                t.Id, t.ProductVariantId, t.StoreId, t.Store!.Name, t.Type, t.QuantityDelta,
                t.SourceType, t.SourceId, t.CreatedAt, t.CreatedBy))
            .ToListAsync(ct);
    }

    public async Task<int> GetQuantityAsync(Guid productVariantId, Guid storeId, CancellationToken ct = default)
    {
        var inventory = await db.Inventory
            .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId && i.StoreId == storeId, ct);

        return inventory?.Quantity ?? 0;
    }

    public async Task PostTransactionAsync(PostInventoryTransactionDto dto, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var inventory = await db.Inventory
            .FirstOrDefaultAsync(i => i.ProductVariantId == dto.ProductVariantId && i.StoreId == dto.StoreId, ct);

        if (inventory is null)
        {
            inventory = new Domain.Inventory.Inventory
            {
                Id = Guid.NewGuid(),
                ProductVariantId = dto.ProductVariantId,
                StoreId = dto.StoreId,
                Quantity = 0,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = dto.PerformedBy,
                UpdatedBy = dto.PerformedBy
            };
            db.Inventory.Add(inventory);
        }

        inventory.Quantity += dto.QuantityDelta;
        inventory.UpdatedAt = now;
        inventory.UpdatedBy = dto.PerformedBy;

        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductVariantId = dto.ProductVariantId,
            StoreId = dto.StoreId,
            Type = dto.Type,
            QuantityDelta = dto.QuantityDelta,
            SourceType = dto.SourceType,
            SourceId = dto.SourceId,
            CreatedAt = now,
            CreatedBy = dto.PerformedBy
        });

        // Intentionally no SaveChangesAsync — caller (e.g. GoodsReceiptService.MarkReceivedAsync)
        // commits this together with its own changes in a single unit of work.
    }

    private static InventoryDto ToDto(ProductVariant v, Domain.Stores.Store store, Domain.Inventory.Inventory? inventory) =>
        new(v.Id, v.Sku, v.Barcode, v.Product?.Name ?? string.Empty,
            v.Colour?.Name, v.Size?.Name, v.Status,
            store.Id, store.Name,
            inventory?.Quantity ?? 0, inventory?.UpdatedAt);
}
