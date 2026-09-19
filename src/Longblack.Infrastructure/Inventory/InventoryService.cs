using Longblack.Application.Inventory;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Inventory;

public class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<IReadOnlyList<InventoryDto>> ListAsync(ListInventoryFilter filter, CancellationToken ct = default)
    {
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

        var variants = await query.OrderBy(v => v.Sku).ToListAsync(ct);

        // Left-join semantics: fetch existing Inventory rows for these variants and default to
        // zero for any variant that has never had one created (never received — see CONTEXT.md).
        var variantIds = variants.Select(v => v.Id).ToList();
        var inventoryByVariantId = await db.Inventory
            .Where(i => variantIds.Contains(i.ProductVariantId))
            .ToDictionaryAsync(i => i.ProductVariantId, i => i, ct);

        return variants
            .Select(v => ToDto(v, inventoryByVariantId.GetValueOrDefault(v.Id)))
            .ToList();
    }

    public async Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, CancellationToken ct = default)
    {
        var variant = await db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == productVariantId, ct);

        if (variant is null)
            return null;

        var inventory = await db.Inventory.FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId, ct);
        return ToDto(variant, inventory);
    }

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid productVariantId, CancellationToken ct = default)
    {
        return await db.InventoryTransactions
            .Where(t => t.ProductVariantId == productVariantId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new InventoryTransactionDto(
                t.Id, t.ProductVariantId, t.Type, t.QuantityDelta,
                t.SourceType, t.SourceId, t.CreatedAt, t.CreatedBy))
            .ToListAsync(ct);
    }

    public async Task PostTransactionAsync(PostInventoryTransactionDto dto, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var inventory = await db.Inventory
            .FirstOrDefaultAsync(i => i.ProductVariantId == dto.ProductVariantId, ct);

        if (inventory is null)
        {
            inventory = new Domain.Inventory.Inventory
            {
                Id = Guid.NewGuid(),
                ProductVariantId = dto.ProductVariantId,
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

    private static InventoryDto ToDto(ProductVariant v, Domain.Inventory.Inventory? inventory) =>
        new(v.Id, v.Sku, v.Barcode, v.Product?.Name ?? string.Empty,
            v.Colour?.Name, v.Size?.Name, v.Status,
            inventory?.Quantity ?? 0, inventory?.UpdatedAt);
}

