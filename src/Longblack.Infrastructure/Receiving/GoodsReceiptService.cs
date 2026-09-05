using Longblack.Application.Common.Exceptions;
using Longblack.Application.Inventory;
using Longblack.Application.Receiving.GoodsReceipts;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Domain.Receiving;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Receiving;

public class GoodsReceiptService(AppDbContext db, IInventoryService inventoryService) : IGoodsReceiptService
{
    public async Task<IReadOnlyList<GoodsReceiptDto>> ListAsync(ListGoodsReceiptsFilter filter, CancellationToken ct = default)
    {
        var query = db.GoodsReceipts
            .Include(g => g.Lines).ThenInclude(l => l.ProductVariant)
            .AsQueryable();

        if (filter.Status is not null)
            query = query.Where(g => g.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.SupplierCode))
            query = query.Where(g => g.SupplierCode == filter.SupplierCode);

        var receipts = await query.OrderByDescending(g => g.SequenceNumber).ToListAsync(ct);
        return receipts.Select(ToDto).ToList();
    }

    public async Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var receipt = await LoadAsync(id, ct);
        return receipt is null ? null : ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto dto, string createdBy, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var receipt = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            SupplierCode = dto.SupplierCode,
            ReceivedDate = dto.ReceivedDate ?? now,
            Status = GoodsReceiptStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> UpdateAsync(Guid id, UpdateGoodsReceiptDto dto, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        receipt.SupplierCode = dto.SupplierCode;
        receipt.ReceivedDate = dto.ReceivedDate ?? receipt.ReceivedDate;
        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> AddLineAsync(Guid id, AddGoodsReceiptLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        if (dto.Quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");
        if (dto.UnitCost < 0)
            throw new InvalidStateException("Unit cost cannot be negative.");

        var variant = await db.ProductVariants.FindAsync([dto.ProductVariantId], ct)
            ?? throw new InvalidReferenceException(nameof(ProductVariant), "productVariantId", dto.ProductVariantId);

        if (variant.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Product variant '{variant.Sku}' is inactive and cannot be added to a goods receipt.");

        var now = DateTimeOffset.UtcNow;
        db.GoodsReceiptLines.Add(new GoodsReceiptLine
        {
            Id = Guid.NewGuid(),
            GoodsReceiptId = receipt.Id,
            ProductVariantId = dto.ProductVariantId,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = updatedBy,
            UpdatedBy = updatedBy
        });

        receipt.UpdatedAt = now;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> UpdateLineAsync(Guid id, Guid lineId, UpdateGoodsReceiptLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        var line = await db.GoodsReceiptLines.FirstOrDefaultAsync(l => l.Id == lineId && l.GoodsReceiptId == id, ct)
            ?? throw new NotFoundException(nameof(GoodsReceiptLine), lineId);

        if (dto.Quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");
        if (dto.UnitCost < 0)
            throw new InvalidStateException("Unit cost cannot be negative.");

        line.Quantity = dto.Quantity;
        line.UnitCost = dto.UnitCost;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        line.UpdatedBy = updatedBy;

        receipt.UpdatedAt = line.UpdatedAt;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        var line = await db.GoodsReceiptLines.FirstOrDefaultAsync(l => l.Id == lineId && l.GoodsReceiptId == id, ct)
            ?? throw new NotFoundException(nameof(GoodsReceiptLine), lineId);

        db.GoodsReceiptLines.Remove(line);

        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> MarkReceivedAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await db.GoodsReceipts
            .Include(g => g.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(GoodsReceipt), id);

        RequireDraft(receipt);

        if (receipt.Lines.Count == 0)
            throw new InvalidStateException("A goods receipt must contain at least one line before it can be received.");

        var inactiveVariant = receipt.Lines.FirstOrDefault(l => l.ProductVariant?.Status != ReferenceDataStatus.Active);
        if (inactiveVariant is not null)
            throw new InvalidStateException(
                $"Product variant '{inactiveVariant.ProductVariant?.Sku}' is inactive and cannot be received.");

        var now = DateTimeOffset.UtcNow;

        foreach (var line in receipt.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    InventoryTransactionType.GoodsReceived,
                    line.Quantity,
                    nameof(GoodsReceiptLine),
                    line.Id,
                    updatedBy),
                ct);
        }

        receipt.Status = GoodsReceiptStatus.Received;
        receipt.ReceivedBy = updatedBy;
        receipt.UpdatedAt = now;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    public async Task<GoodsReceiptDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        receipt.Status = GoodsReceiptStatus.Cancelled;
        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt);
    }

    private async Task<GoodsReceipt?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.GoodsReceipts
            .Include(g => g.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    private async Task<GoodsReceipt> RequireAsync(Guid id, CancellationToken ct) =>
        await db.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(GoodsReceipt), id);

    private static void RequireDraft(GoodsReceipt receipt)
    {
        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidStateException(
                $"Goods receipt '{FormatReceiptNumber(receipt.SequenceNumber)}' is {receipt.Status} and cannot be modified.");
    }

    private static string FormatReceiptNumber(int sequenceNumber) => $"GR-{sequenceNumber:D4}";

    private static GoodsReceiptDto ToDto(GoodsReceipt g) =>
        new(g.Id,
            FormatReceiptNumber(g.SequenceNumber),
            g.SupplierCode,
            g.ReceivedDate,
            g.Status,
            g.ReceivedBy,
            g.CreatedAt,
            g.UpdatedAt,
            g.CreatedBy,
            g.UpdatedBy,
            g.Lines.Select(l => new GoodsReceiptLineDto(
                l.Id, l.GoodsReceiptId, l.ProductVariantId,
                l.ProductVariant?.Sku ?? string.Empty,
                l.ProductVariant?.Barcode,
                l.Quantity, l.UnitCost,
                l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy))
                .ToList());
}
