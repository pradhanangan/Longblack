using Longblack.Application.Dashboard;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Receiving;
using Longblack.Domain.StockTake;
using Longblack.Domain.StockTransfers;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Dashboard;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(Guid? storeId, CancellationToken ct = default)
    {
        var inventory = db.Inventory.AsQueryable();
        if (storeId is not null)
            inventory = inventory.Where(i => i.StoreId == storeId);

        var totalUnits = await inventory.SumAsync(i => (int?)i.Quantity, ct) ?? 0;

        // Variants with no Inventory row count as zero, so count from variants rather than Inventory.
        var variantsAtZeroOrBelow = await db.ProductVariants
            .Where(v => v.Status == ReferenceDataStatus.Active)
            .CountAsync(v => inventory.Where(i => i.ProductVariantId == v.Id).Sum(i => i.Quantity) <= 0, ct);

        var draftReceipts = await db.GoodsReceipts.CountAsync(r => r.Status == GoodsReceiptStatus.Draft, ct);
        var inTransit = await db.StockTransfers.CountAsync(t => t.Status == StockTransferStatus.Dispatched, ct);
        var openStockTakes = await db.StockTakes.CountAsync(
            s => s.Status == StockTakeStatus.InProgress || s.Status == StockTakeStatus.Completed, ct);

        var recent = await db.InventoryTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new DashboardTransactionDto(
                t.Id, t.ProductVariantId, t.ProductVariant!.Sku, t.ProductVariant.Product!.Name,
                t.Store!.Name, t.Type, t.QuantityDelta, t.CreatedAt, t.CreatedBy))
            .ToListAsync(ct);

        return new DashboardDto(totalUnits, variantsAtZeroOrBelow, draftReceipts, inTransit, openStockTakes, recent);
    }
}
