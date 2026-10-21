using Longblack.Application.Common.Exceptions;
using Longblack.Application.Inventory;
using Longblack.Application.Purchasing;
using Longblack.Application.Receiving.GoodsReceipts;
using Longblack.Application.Stores;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Domain.Purchasing;
using Longblack.Domain.Receiving;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Receiving;

public class GoodsReceiptService(
    AppDbContext db,
    IInventoryService inventoryService,
    IStoreService storeService,
    IPurchaseOrderService purchaseOrderService) : IGoodsReceiptService
{
    public async Task<IReadOnlyList<GoodsReceiptDto>> ListAsync(ListGoodsReceiptsFilter filter, CancellationToken ct = default)
    {
        var query = db.GoodsReceipts
            .Include(g => g.Store)
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines).ThenInclude(l => l.ProductVariant)
            .AsQueryable();

        if (filter.Status is not null)
            query = query.Where(g => g.Status == filter.Status);

        if (filter.SupplierId is not null)
            query = query.Where(g => g.SupplierId == filter.SupplierId);

        var receipts = await query.OrderByDescending(g => g.SequenceNumber).ToListAsync(ct);
        return receipts.Select(g => ToDto(g, [])).ToList();
    }

    public async Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var receipt = await LoadAsync(id, ct);
        return receipt is null ? null : ToDto(receipt, await ComputeWarningsAsync(receipt, ct));
    }

    public async Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto dto, string createdBy, CancellationToken ct = default)
    {
        var warehouseStoreId = await storeService.GetWarehouseStoreIdAsync(ct);

        var supplier = await db.Suppliers.FindAsync([dto.SupplierId], ct)
            ?? throw new InvalidReferenceException(nameof(Supplier), "supplierId", dto.SupplierId);

        if (supplier.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Supplier '{supplier.Name}' is inactive and cannot be used for a new goods receipt.");

        if (dto.PurchaseOrderId is { } purchaseOrderId)
            await RequireReceivablePurchaseOrderAsync(purchaseOrderId, dto.SupplierId, ct);

        var now = DateTimeOffset.UtcNow;
        var receipt = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            StoreId = warehouseStoreId,
            SupplierId = dto.SupplierId,
            PurchaseOrderId = dto.PurchaseOrderId,
            ReceivedDate = dto.ReceivedDate ?? now,
            Status = GoodsReceiptStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt, []);
    }

    public async Task<GoodsReceiptDto> CreateFromPurchaseOrderAsync(Guid purchaseOrderId, string createdBy, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), purchaseOrderId);

        // Header and pre-filled lines commit together so a failure never leaves an empty orphan draft.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var created = await CreateAsync(
            new CreateGoodsReceiptDto(order.SupplierId, null, purchaseOrderId), createdBy, ct);

        var balances = await purchaseOrderService.GetLineBalancesAsync(purchaseOrderId, null, ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var (variantId, balance) in balances.Where(b => b.Value.Outstanding > 0))
        {
            db.GoodsReceiptLines.Add(new GoodsReceiptLine
            {
                Id = Guid.NewGuid(),
                GoodsReceiptId = created.Id,
                ProductVariantId = variantId,
                Quantity = balance.Outstanding,
                UnitCost = balance.ExpectedUnitCost,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetByIdAsync(created.Id, ct) ?? created;
    }

    public async Task<GoodsReceiptDto> UpdateAsync(Guid id, UpdateGoodsReceiptDto dto, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await RequireAsync(id, ct);
        RequireDraft(receipt);

        var supplierExists = await db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId, ct);
        if (!supplierExists)
            throw new InvalidReferenceException(nameof(Supplier), "supplierId", dto.SupplierId);

        if (dto.PurchaseOrderId is { } purchaseOrderId)
        {
            if (purchaseOrderId == receipt.PurchaseOrderId)
                await RequireSupplierMatchesAsync(purchaseOrderId, dto.SupplierId, ct);
            else
                await RequireReceivablePurchaseOrderAsync(purchaseOrderId, dto.SupplierId, ct);

            var orderedVariants = (await purchaseOrderService.GetLineBalancesAsync(purchaseOrderId, null, ct)).Keys.ToHashSet();
            var offPo = await db.GoodsReceiptLines
                .Where(l => l.GoodsReceiptId == id && !orderedVariants.Contains(l.ProductVariantId))
                .Select(l => l.ProductVariant!.Sku)
                .FirstOrDefaultAsync(ct);
            if (offPo is not null)
                throw new InvalidStateException($"Product variant '{offPo}' is not on the purchase order.");
        }

        receipt.SupplierId = dto.SupplierId;
        receipt.PurchaseOrderId = dto.PurchaseOrderId;
        receipt.ReceivedDate = dto.ReceivedDate ?? receipt.ReceivedDate;
        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(receipt.Id, ct) ?? ToDto(receipt, []);
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

        if (receipt.PurchaseOrderId is { } linkedOrderId)
        {
            var balances = await purchaseOrderService.GetLineBalancesAsync(linkedOrderId, null, ct);
            if (!balances.ContainsKey(dto.ProductVariantId))
                throw new InvalidStateException(
                    $"Product variant '{variant.Sku}' is not on the purchase order. Receive it directly without a purchase order instead.");
        }

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
            .Include(g => g.Store)
            .Include(g => g.Supplier)
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

        // Serialise concurrent receipts against the same Purchase Order so its derived status is never corrupted.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (receipt.PurchaseOrderId is { } purchaseOrderId)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM purchase_orders WHERE id = {purchaseOrderId} FOR UPDATE", ct);

            var orderStatus = await db.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.Id == purchaseOrderId)
                .Select(p => p.Status)
                .FirstAsync(ct);

            if (orderStatus is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived))
                throw new InvalidStateException(
                    $"The linked purchase order is {orderStatus} and can no longer be received against.");
        }

        foreach (var line in receipt.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    receipt.StoreId,
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

        if (receipt.PurchaseOrderId is { } linkedOrderId)
            await purchaseOrderService.RefreshStatusAsync(linkedOrderId, updatedBy, ct);

        await transaction.CommitAsync(ct);
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
            .Include(g => g.Store)
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    private async Task RequireSupplierMatchesAsync(Guid purchaseOrderId, Guid supplierId, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            ?? throw new InvalidReferenceException(nameof(PurchaseOrder), "purchaseOrderId", purchaseOrderId);

        if (order.SupplierId != supplierId)
            throw new InvalidStateException("The goods receipt's supplier must match the purchase order's supplier.");
    }

    private async Task RequireReceivablePurchaseOrderAsync(Guid purchaseOrderId, Guid supplierId, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            ?? throw new InvalidReferenceException(nameof(PurchaseOrder), "purchaseOrderId", purchaseOrderId);

        if (order.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived))
            throw new InvalidStateException(
                $"Purchase order is {order.Status}; goods can only be received against an issued or partially received purchase order.");

        if (order.SupplierId != supplierId)
            throw new InvalidStateException("The goods receipt's supplier must match the purchase order's supplier.");
    }

    // Over-receipt is a warning, not an error: suppliers over-ship.
    private async Task<IReadOnlyList<string>> ComputeWarningsAsync(GoodsReceipt receipt, CancellationToken ct)
    {
        if (receipt.PurchaseOrderId is not { } purchaseOrderId || receipt.Status != GoodsReceiptStatus.Draft)
            return [];

        var balances = await purchaseOrderService.GetLineBalancesAsync(purchaseOrderId, receipt.Id, ct);
        var number = receipt.PurchaseOrder?.SequenceNumber is { } n ? $"PO-{n:D4}" : "the purchase order";

        return receipt.Lines
            .GroupBy(l => l.ProductVariantId)
            .Select(g => (Sku: g.First().ProductVariant?.Sku ?? string.Empty, Quantity: g.Sum(l => l.Quantity), VariantId: g.Key))
            .Where(x => balances.TryGetValue(x.VariantId, out var b) && x.Quantity > b.Outstanding)
            .Select(x => $"{x.Sku}: receiving {x.Quantity} exceeds the {balances[x.VariantId].Outstanding} still outstanding on {number}.")
            .ToList();
    }

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

    private static GoodsReceiptDto ToDto(GoodsReceipt g) => ToDto(g, []);

    private static GoodsReceiptDto ToDto(GoodsReceipt g, IReadOnlyList<string> warnings) =>
        new(g.Id,
            FormatReceiptNumber(g.SequenceNumber),
            g.StoreId,
            g.Store?.Name ?? string.Empty,
            g.SupplierId,
            g.Supplier?.Name ?? string.Empty,
            g.PurchaseOrderId,
            g.PurchaseOrder?.SequenceNumber is { } po ? $"PO-{po:D4}" : null,
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
                .ToList(),
            warnings);
}
