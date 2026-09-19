using Longblack.Application.Common.Exceptions;
using Longblack.Application.Inventory;
using Longblack.Application.StockTake;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StockTakeEntity = Longblack.Domain.StockTake.StockTake;
using StockTakeItemEntity = Longblack.Domain.StockTake.StockTakeItem;

namespace Longblack.Infrastructure.StockTake;

public class StockTakeService(AppDbContext db, IInventoryService inventoryService) : IStockTakeService
{
    public async Task<IReadOnlyList<StockTakeDto>> ListAsync(ListStockTakesFilter filter, CancellationToken ct = default)
    {
        var query = db.StockTakes
            .Include(s => s.Brand)
            .Include(s => s.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(s => s.Status == filter.Status);

        var stockTakes = await query.OrderByDescending(s => s.SequenceNumber).ToListAsync(ct);
        return stockTakes.Select(s => ToDto(s, [])).ToList();
    }

    public async Task<StockTakeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var stockTake = await LoadAsync(id, ct);
        return stockTake is null ? null : ToDto(stockTake, stockTake.Items);
    }

    public async Task<StockTakeDto> CreateAsync(CreateStockTakeDto dto, string createdBy, CancellationToken ct = default)
    {
        if (dto.BrandId is not null)
        {
            var brandExists = await db.Brands.AnyAsync(b => b.Id == dto.BrandId, ct);
            if (!brandExists)
                throw new InvalidReferenceException(nameof(Brand), "brandId", dto.BrandId);
        }

        if (dto.CategoryId is not null)
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                throw new InvalidReferenceException(nameof(Category), "categoryId", dto.CategoryId);
        }

        var now = DateTimeOffset.UtcNow;
        var stockTake = new StockTakeEntity
        {
            Id = Guid.NewGuid(),
            BrandId = dto.BrandId,
            CategoryId = dto.CategoryId,
            Status = Domain.StockTake.StockTakeStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.StockTakes.Add(stockTake);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, []);
    }

    public async Task<StockTakeDto> StartAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.Draft);

        var variantQuery = db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Status == ReferenceDataStatus.Active)
            .AsQueryable();

        if (stockTake.BrandId is not null)
            variantQuery = variantQuery.Where(v => v.Product!.BrandId == stockTake.BrandId);

        if (stockTake.CategoryId is not null)
            variantQuery = variantQuery.Where(v => v.Product!.CategoryId == stockTake.CategoryId);

        var variants = await variantQuery.ToListAsync(ct);

        if (variants.Count == 0)
            throw new InvalidStateException("No active product variants match this Stock Take's scope — nothing to count.");

        var variantIds = variants.Select(v => v.Id).ToList();
        var inventoryByVariantId = await db.Inventory
            .Where(i => variantIds.Contains(i.ProductVariantId))
            .ToDictionaryAsync(i => i.ProductVariantId, i => i.Quantity, ct);

        var now = DateTimeOffset.UtcNow;
        var items = variants.Select(v => new StockTakeItemEntity
        {
            Id = Guid.NewGuid(),
            StockTakeId = stockTake.Id,
            ProductVariantId = v.Id,
            ExpectedQuantity = inventoryByVariantId.GetValueOrDefault(v.Id, 0),
            Status = Domain.StockTake.StockTakeItemStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = updatedBy,
            UpdatedBy = updatedBy
        }).ToList();

        db.StockTakeItems.AddRange(items);

        stockTake.Status = Domain.StockTake.StockTakeStatus.InProgress;
        stockTake.StartDate = now;
        stockTake.UpdatedAt = now;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, items);
    }

    public async Task<StockTakeDto> RemoveItemAsync(Guid id, Guid itemId, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.InProgress);

        var item = await db.StockTakeItems.FirstOrDefaultAsync(i => i.Id == itemId && i.StockTakeId == id, ct)
            ?? throw new NotFoundException(nameof(StockTakeItemEntity), itemId);

        if (item.Status != Domain.StockTake.StockTakeItemStatus.Pending)
            throw new InvalidStateException("Only items that have not yet been counted can be removed.");

        db.StockTakeItems.Remove(item);

        stockTake.UpdatedAt = DateTimeOffset.UtcNow;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, []);
    }

    public async Task<StockTakeDto> RecordCountAsync(Guid id, Guid itemId, RecordCountDto dto, string countedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.InProgress);

        if (dto.Quantity < 0)
            throw new InvalidStateException("Counted quantity cannot be negative.");

        var item = await db.StockTakeItems.FirstOrDefaultAsync(i => i.Id == itemId && i.StockTakeId == id, ct)
            ?? throw new NotFoundException(nameof(StockTakeItemEntity), itemId);

        var now = DateTimeOffset.UtcNow;

        db.StockTakeCounts.Add(new Domain.StockTake.StockTakeCount
        {
            Id = Guid.NewGuid(),
            StockTakeItemId = item.Id,
            Quantity = dto.Quantity,
            CountedAt = now,
            CountedBy = countedBy
        });

        item.CountedQuantity = dto.Quantity;
        item.Status = Domain.StockTake.StockTakeItemStatus.Counted;
        item.UpdatedAt = now;
        item.UpdatedBy = countedBy;

        stockTake.UpdatedAt = now;
        stockTake.UpdatedBy = countedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, []);
    }

    public async Task<IReadOnlyList<StockTakeCountDto>> GetCountsAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        var itemExists = await db.StockTakeItems.AnyAsync(i => i.Id == itemId && i.StockTakeId == id, ct);
        if (!itemExists)
            throw new NotFoundException(nameof(StockTakeItemEntity), itemId);

        return await db.StockTakeCounts
            .Where(c => c.StockTakeItemId == itemId)
            .OrderBy(c => c.CountedAt)
            .Select(c => new StockTakeCountDto(c.Id, c.StockTakeItemId, c.Quantity, c.CountedAt, c.CountedBy))
            .ToListAsync(ct);
    }

    public async Task<StockTakeDto> CompleteAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.InProgress);

        var items = await db.StockTakeItems.Where(i => i.StockTakeId == id).ToListAsync(ct);

        if (items.Count == 0)
            throw new InvalidStateException("A Stock Take must contain at least one item before it can be completed.");

        if (items.Any(i => i.Status != Domain.StockTake.StockTakeItemStatus.Counted))
            throw new InvalidStateException("Every item must be counted before this Stock Take can be completed.");

        var now = DateTimeOffset.UtcNow;
        stockTake.Status = Domain.StockTake.StockTakeStatus.Completed;
        stockTake.CompletionDate = now;
        stockTake.CompletedBy = updatedBy;
        stockTake.UpdatedAt = now;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, items);
    }

    public async Task<StockTakeDto> ReopenAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.Completed);

        // Deliberately does not touch any item's Status/CountedQuantity — see ADR discussion in
        // CONTEXT.md: only a genuine new count should move an item, never a bulk reset.
        stockTake.Status = Domain.StockTake.StockTakeStatus.InProgress;
        stockTake.UpdatedAt = DateTimeOffset.UtcNow;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, []);
    }

    public async Task<StockTakeDto> ApproveAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);
        RequireStatus(stockTake, Domain.StockTake.StockTakeStatus.Completed);

        var items = await db.StockTakeItems.Where(i => i.StockTakeId == id).ToListAsync(ct);

        foreach (var item in items)
        {
            var variance = (item.CountedQuantity ?? item.ExpectedQuantity) - item.ExpectedQuantity;
            if (variance == 0)
                continue;

            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    item.ProductVariantId,
                    InventoryTransactionType.StockTakeAdjustment,
                    variance,
                    nameof(StockTakeItemEntity),
                    item.Id,
                    updatedBy),
                ct);
        }

        var now = DateTimeOffset.UtcNow;
        stockTake.Status = Domain.StockTake.StockTakeStatus.Approved;
        stockTake.ApprovedDate = now;
        stockTake.ApprovedBy = updatedBy;
        stockTake.UpdatedAt = now;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, items);
    }

    public async Task<StockTakeDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var stockTake = await RequireAsync(id, ct);

        if (stockTake.Status != Domain.StockTake.StockTakeStatus.Draft &&
            stockTake.Status != Domain.StockTake.StockTakeStatus.InProgress)
            throw new InvalidStateException(
                $"Stock Take '{FormatReferenceNumber(stockTake.SequenceNumber)}' is {stockTake.Status} and cannot be cancelled.");

        stockTake.Status = Domain.StockTake.StockTakeStatus.Cancelled;
        stockTake.UpdatedAt = DateTimeOffset.UtcNow;
        stockTake.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(stockTake.Id, ct) ?? ToDto(stockTake, []);
    }

    private async Task<StockTakeEntity?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.StockTakes
            .Include(s => s.Brand)
            .Include(s => s.Category)
            .Include(s => s.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Product)
            .Include(s => s.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Colour)
            .Include(s => s.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v!.Size)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private async Task<StockTakeEntity> RequireAsync(Guid id, CancellationToken ct) =>
        await db.StockTakes.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(StockTakeEntity), id);

    private static void RequireStatus(StockTakeEntity stockTake, string expectedStatus)
    {
        if (stockTake.Status != expectedStatus)
            throw new InvalidStateException(
                $"Stock Take '{FormatReferenceNumber(stockTake.SequenceNumber)}' is {stockTake.Status}, expected {expectedStatus}.");
    }

    private static string FormatReferenceNumber(int sequenceNumber) => $"ST-{sequenceNumber:D4}";

    private static StockTakeItemDto ToItemDto(StockTakeItemEntity i) =>
        new(i.Id, i.StockTakeId, i.ProductVariantId,
            i.ProductVariant?.Sku ?? string.Empty,
            i.ProductVariant?.Barcode,
            i.ProductVariant?.Product?.Name ?? string.Empty,
            i.ProductVariant?.Colour?.Name,
            i.ProductVariant?.Size?.Name,
            i.ExpectedQuantity,
            i.CountedQuantity,
            i.CountedQuantity.HasValue ? i.CountedQuantity.Value - i.ExpectedQuantity : null,
            i.Status);

    private static StockTakeDto ToDto(StockTakeEntity s, IEnumerable<StockTakeItemEntity> items) =>
        new(s.Id,
            FormatReferenceNumber(s.SequenceNumber),
            s.BrandId, s.Brand?.Name,
            s.CategoryId, s.Category?.Name,
            s.Status,
            s.StartDate, s.CompletionDate, s.CompletedBy,
            s.ApprovedDate, s.ApprovedBy,
            s.CreatedAt, s.UpdatedAt, s.CreatedBy, s.UpdatedBy,
            items.Select(ToItemDto).ToList());
}
