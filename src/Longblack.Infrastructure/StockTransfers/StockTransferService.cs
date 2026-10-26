using Longblack.Application.Common.Exceptions;
using Longblack.Application.Common.Paging;
using Longblack.Application.Inventory;
using Longblack.Application.StockTransfers;
using Longblack.Application.Stores;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Domain.StockTransfers;
using Longblack.Domain.Stores;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.StockTransfers;

public class StockTransferService(AppDbContext db, IInventoryService inventoryService, IStoreService storeService) : IStockTransferService
{
    public async Task<PagedResult<StockTransferDto>> ListAsync(ListStockTransfersFilter filter, PageRequest? page = null, CancellationToken ct = default)
    {
        var query = db.StockTransfers
            .Include(t => t.SourceStore)
            .Include(t => t.DestinationStore)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(t => t.Status == filter.Status);

        if (filter.SourceStoreId is not null)
            query = query.Where(t => t.SourceStoreId == filter.SourceStoreId);

        if (filter.DestinationStoreId is not null)
            query = query.Where(t => t.DestinationStoreId == filter.DestinationStoreId);

        return await query.OrderByDescending(t => t.SequenceNumber).ThenBy(t => t.Id)
            .ToPagedResultAsync(page, t => ToDto(t, []), ct);
    }

    public async Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await LoadAsync(id, ct);
        return transfer is null ? null : ToDto(transfer, transfer.Lines);
    }

    public async Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto, string createdBy, CancellationToken ct = default)
    {
        var sourceStore = await db.Stores.FirstOrDefaultAsync(s => s.Id == dto.SourceStoreId, ct)
            ?? throw new InvalidReferenceException(nameof(Store), "sourceStoreId", dto.SourceStoreId);

        var destinationStore = await db.Stores.FirstOrDefaultAsync(s => s.Id == dto.DestinationStoreId, ct)
            ?? throw new InvalidReferenceException(nameof(Store), "destinationStoreId", dto.DestinationStoreId);

        if (dto.SourceStoreId == dto.DestinationStoreId)
            throw new InvalidStateException("A Stock Transfer's source and destination Store must differ.");

        var warehouseStoreId = await storeService.GetWarehouseStoreIdAsync(ct);
        if (dto.SourceStoreId != warehouseStoreId && dto.DestinationStoreId != warehouseStoreId)
            throw new InvalidStateException(
                "A Stock Transfer must have the Warehouse Store as either its source or destination.");

        var now = DateTimeOffset.UtcNow;
        var transfer = new StockTransfer
        {
            Id = Guid.NewGuid(),
            SourceStoreId = sourceStore.Id,
            DestinationStoreId = destinationStore.Id,
            Status = StockTransferStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.StockTransfers.Add(transfer);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, []);
    }

    public async Task<StockTransferDto> AddLineAsync(Guid id, AddStockTransferLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await RequireAsync(id, ct);
        RequireDraft(transfer);

        if (dto.Quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");

        var variant = await db.ProductVariants.FindAsync([dto.ProductVariantId], ct)
            ?? throw new InvalidReferenceException(nameof(ProductVariant), "productVariantId", dto.ProductVariantId);

        var alreadyPresent = await db.StockTransferLines
            .AnyAsync(l => l.StockTransferId == id && l.ProductVariantId == dto.ProductVariantId, ct);
        if (alreadyPresent)
            throw new DuplicateException(nameof(StockTransferLine), "productVariantId", dto.ProductVariantId.ToString());

        var availableQuantity = await inventoryService.GetQuantityAsync(dto.ProductVariantId, transfer.SourceStoreId, ct);
        if (dto.Quantity > availableQuantity)
            throw new InvalidStateException(
                $"Quantity {dto.Quantity} exceeds the source Store's current Inventory of {availableQuantity} for '{variant.Sku}'.");

        var now = DateTimeOffset.UtcNow;
        db.StockTransferLines.Add(new StockTransferLine
        {
            Id = Guid.NewGuid(),
            StockTransferId = transfer.Id,
            ProductVariantId = dto.ProductVariantId,
            DispatchedQuantity = dto.Quantity,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = updatedBy,
            UpdatedBy = updatedBy
        });

        transfer.UpdatedAt = now;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, []);
    }

    public async Task<StockTransferDto> UpdateLineAsync(Guid id, Guid lineId, UpdateStockTransferLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await RequireAsync(id, ct);
        RequireDraft(transfer);

        var line = await db.StockTransferLines.FirstOrDefaultAsync(l => l.Id == lineId && l.StockTransferId == id, ct)
            ?? throw new NotFoundException(nameof(StockTransferLine), lineId);

        if (dto.Quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");

        var availableQuantity = await inventoryService.GetQuantityAsync(line.ProductVariantId, transfer.SourceStoreId, ct);
        if (dto.Quantity > availableQuantity)
            throw new InvalidStateException(
                $"Quantity {dto.Quantity} exceeds the source Store's current Inventory of {availableQuantity}.");

        line.DispatchedQuantity = dto.Quantity;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        line.UpdatedBy = updatedBy;

        transfer.UpdatedAt = line.UpdatedAt;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, []);
    }

    public async Task<StockTransferDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await RequireAsync(id, ct);
        RequireDraft(transfer);

        var line = await db.StockTransferLines.FirstOrDefaultAsync(l => l.Id == lineId && l.StockTransferId == id, ct)
            ?? throw new NotFoundException(nameof(StockTransferLine), lineId);

        db.StockTransferLines.Remove(line);

        transfer.UpdatedAt = DateTimeOffset.UtcNow;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, []);
    }

    public async Task<StockTransferDto> DispatchAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await db.StockTransfers
            .Include(t => t.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(StockTransfer), id);

        RequireDraft(transfer);

        if (transfer.Lines.Count == 0)
            throw new InvalidStateException("A Stock Transfer must contain at least one line before it can be dispatched.");

        // Re-validate every line against current Inventory at the moment of dispatch — reject the
        // whole dispatch if any line fails, before posting any transactions.
        foreach (var line in transfer.Lines)
        {
            var availableQuantity = await inventoryService.GetQuantityAsync(line.ProductVariantId, transfer.SourceStoreId, ct);
            if (line.DispatchedQuantity > availableQuantity)
                throw new InvalidStateException(
                    $"Quantity {line.DispatchedQuantity} for '{line.ProductVariant?.Sku}' exceeds the source Store's current Inventory of {availableQuantity}.");
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var line in transfer.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    transfer.SourceStoreId,
                    InventoryTransactionType.TransferOut,
                    -line.DispatchedQuantity,
                    nameof(StockTransferLine),
                    line.Id,
                    updatedBy),
                ct);
        }

        transfer.Status = StockTransferStatus.Dispatched;
        transfer.DispatchedDate = now;
        transfer.DispatchedBy = updatedBy;
        transfer.UpdatedAt = now;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, transfer.Lines);
    }

    public async Task<StockTransferDto> ReceiveAsync(Guid id, ReceiveStockTransferDto dto, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await db.StockTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(StockTransfer), id);

        if (transfer.Status != StockTransferStatus.Dispatched)
            throw new InvalidStateException(
                $"Stock Transfer '{FormatTransferNumber(transfer.SequenceNumber)}' is {transfer.Status}, expected {StockTransferStatus.Dispatched}.");

        var linesById = transfer.Lines.ToDictionary(l => l.Id);

        foreach (var receivedLine in dto.Lines)
        {
            if (!linesById.TryGetValue(receivedLine.LineId, out var line))
                throw new NotFoundException(nameof(StockTransferLine), receivedLine.LineId);

            if (receivedLine.ReceivedQuantity < 0)
                throw new InvalidStateException("Received quantity cannot be negative.");

            line.ReceivedQuantity = receivedLine.ReceivedQuantity;
        }

        var missingLine = transfer.Lines.FirstOrDefault(l => l.ReceivedQuantity is null);
        if (missingLine is not null)
            throw new InvalidStateException("Every line must have a received quantity before the transfer can be received.");

        var now = DateTimeOffset.UtcNow;

        foreach (var line in transfer.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    transfer.DestinationStoreId,
                    InventoryTransactionType.TransferIn,
                    line.ReceivedQuantity!.Value,
                    nameof(StockTransferLine),
                    line.Id,
                    updatedBy),
                ct);

            line.UpdatedAt = now;
            line.UpdatedBy = updatedBy;
        }

        transfer.Status = StockTransferStatus.Received;
        transfer.ReceivedDate = now;
        transfer.ReceivedBy = updatedBy;
        transfer.UpdatedAt = now;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, transfer.Lines);
    }

    public async Task<StockTransferDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var transfer = await RequireAsync(id, ct);
        RequireDraft(transfer);

        transfer.Status = StockTransferStatus.Cancelled;
        transfer.UpdatedAt = DateTimeOffset.UtcNow;
        transfer.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct) ?? ToDto(transfer, []);
    }

    private async Task<StockTransfer?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.StockTransfers
            .Include(t => t.SourceStore)
            .Include(t => t.DestinationStore)
            .Include(t => t.Lines).ThenInclude(l => l.ProductVariant).ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    private async Task<StockTransfer> RequireAsync(Guid id, CancellationToken ct) =>
        await db.StockTransfers.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(StockTransfer), id);

    private static void RequireDraft(StockTransfer transfer)
    {
        if (transfer.Status != StockTransferStatus.Draft)
            throw new InvalidStateException(
                $"Stock Transfer '{FormatTransferNumber(transfer.SequenceNumber)}' is {transfer.Status} and cannot be modified.");
    }

    private static string FormatTransferNumber(int sequenceNumber) => $"TR-{sequenceNumber:D4}";

    private static StockTransferLineDto ToLineDto(StockTransferLine l) =>
        new(l.Id, l.StockTransferId, l.ProductVariantId,
            l.ProductVariant?.Sku ?? string.Empty,
            l.ProductVariant?.Barcode,
            l.ProductVariant?.Product?.Name ?? string.Empty,
            l.DispatchedQuantity,
            l.ReceivedQuantity,
            l.ReceivedQuantity.HasValue ? l.DispatchedQuantity - l.ReceivedQuantity.Value : null,
            l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy);

    private static StockTransferDto ToDto(StockTransfer t, IEnumerable<StockTransferLine> lines) =>
        new(t.Id,
            FormatTransferNumber(t.SequenceNumber),
            t.SourceStoreId, t.SourceStore?.Name ?? string.Empty,
            t.DestinationStoreId, t.DestinationStore?.Name ?? string.Empty,
            t.Status,
            t.DispatchedDate, t.DispatchedBy,
            t.ReceivedDate, t.ReceivedBy,
            t.CreatedAt, t.UpdatedAt, t.CreatedBy, t.UpdatedBy,
            lines.Select(ToLineDto).ToList());
}
