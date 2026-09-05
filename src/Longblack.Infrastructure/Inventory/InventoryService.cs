using Longblack.Application.Inventory;
using Longblack.Domain.Inventory;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Inventory;

public class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<IReadOnlyList<InventoryDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.Inventory
            .Include(i => i.ProductVariant)
            .OrderBy(i => i.ProductVariant!.Sku)
            .Select(i => ToDto(i))
            .ToListAsync(ct);
    }

    public async Task<InventoryDto?> GetByVariantIdAsync(Guid productVariantId, CancellationToken ct = default)
    {
        var inventory = await db.Inventory
            .Include(i => i.ProductVariant)
            .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId, ct);

        return inventory is null ? null : ToDto(inventory);
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

    private static InventoryDto ToDto(Domain.Inventory.Inventory i) =>
        new(i.Id, i.ProductVariantId, i.ProductVariant?.Sku ?? string.Empty, i.Quantity, i.UpdatedAt);
}
