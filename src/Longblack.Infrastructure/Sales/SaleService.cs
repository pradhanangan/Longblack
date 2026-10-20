using Longblack.Application.Common.Exceptions;
using Longblack.Application.Inventory;
using Longblack.Application.Sales;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Inventory;
using Longblack.Domain.Sales;
using Longblack.Domain.Stores;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Sales;

public class SaleService(AppDbContext db, IInventoryService inventoryService) : ISaleService
{
    public async Task<IReadOnlyList<SaleDto>> ListAsync(ListSalesFilter filter, CancellationToken ct = default)
    {
        var query = db.Sales
            .Include(s => s.Store)
            .Include(s => s.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(s => s.Status == filter.Status);

        if (filter.StoreId is not null)
            query = query.Where(s => s.StoreId == filter.StoreId);

        if (filter.From is not null)
            query = query.Where(s => s.CreatedAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(s => s.CreatedAt <= filter.To);

        var sales = await query.OrderByDescending(s => s.SequenceNumber).ToListAsync(ct);
        return sales.Select(s => ToDto(s)).ToList();
    }

    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await LoadAsync(id, ct);
        if (sale is null)
            return null;

        var dto = ToDto(sale);
        var variantIds = sale.Lines.Select(l => l.ProductVariantId).ToList();
        var stock = await db.Inventory
            .Where(i => i.StoreId == sale.StoreId && variantIds.Contains(i.ProductVariantId))
            .ToDictionaryAsync(i => i.ProductVariantId, i => i.Quantity, ct);

        return dto with
        {
            Lines = dto.Lines
                .Select(l => l with { AvailableQuantity = stock.GetValueOrDefault(l.ProductVariantId) })
                .ToList()
        };
    }

    public async Task<SaleDto> CreateAsync(CreateSaleDto dto, string createdBy, CancellationToken ct = default)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == dto.StoreId, ct)
            ?? throw new InvalidReferenceException(nameof(Store), "storeId", dto.StoreId);

        if (store.Status != StoreStatus.Active)
            throw new InvalidStateException($"Store '{store.Name}' is {store.Status} and cannot make sales.");

        var now = DateTimeOffset.UtcNow;
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Status = SaleStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> AddLineAsync(Guid id, AddSaleLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var sale = await RequireAsync(id, ct);
        RequireDraft(sale);

        var variant = await db.ProductVariants.FindAsync([dto.ProductVariantId], ct)
            ?? throw new InvalidReferenceException(nameof(ProductVariant), "productVariantId", dto.ProductVariantId);

        if (variant.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Variant '{variant.Sku}' is inactive and cannot be sold.");

        if (variant.SellingPrice <= 0)
            throw new InvalidStateException($"Variant '{variant.Sku}' has no Selling Price and cannot be sold.");

        var now = DateTimeOffset.UtcNow;
        var existing = await db.SaleLines
            .FirstOrDefaultAsync(l => l.SaleId == id && l.ProductVariantId == dto.ProductVariantId, ct);

        if (existing is not null)
        {
            // Consolidate repeated variants into the one existing line at its snapshotted price.
            var quantity = existing.Quantity + dto.Quantity;
            var discount = existing.Discount + dto.Discount;
            ValidateLine(quantity, discount, existing.UnitPrice);
            await RequireStockAsync(sale, variant.Sku, dto.ProductVariantId, quantity, ct);

            existing.Quantity = quantity;
            existing.Discount = discount;
            existing.UpdatedAt = now;
            existing.UpdatedBy = updatedBy;
        }
        else
        {
            ValidateLine(dto.Quantity, dto.Discount, variant.SellingPrice);
            await RequireStockAsync(sale, variant.Sku, dto.ProductVariantId, dto.Quantity, ct);

            db.SaleLines.Add(new SaleLine
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = variant.SellingPrice,
                Discount = dto.Discount,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy
            });
        }

        sale.UpdatedAt = now;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> UpdateLineAsync(Guid id, Guid lineId, UpdateSaleLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var sale = await RequireAsync(id, ct);
        RequireDraft(sale);

        var line = await db.SaleLines.FirstOrDefaultAsync(l => l.Id == lineId && l.SaleId == id, ct)
            ?? throw new NotFoundException(nameof(SaleLine), lineId);

        ValidateLine(dto.Quantity, dto.Discount, line.UnitPrice);

        var sku = await db.ProductVariants.Where(v => v.Id == line.ProductVariantId).Select(v => v.Sku).FirstAsync(ct);
        await RequireStockAsync(sale, sku, line.ProductVariantId, dto.Quantity, ct);

        line.Quantity = dto.Quantity;
        line.Discount = dto.Discount;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        line.UpdatedBy = updatedBy;

        sale.UpdatedAt = line.UpdatedAt;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default)
    {
        var sale = await RequireAsync(id, ct);
        RequireDraft(sale);

        var line = await db.SaleLines.FirstOrDefaultAsync(l => l.Id == lineId && l.SaleId == id, ct)
            ?? throw new NotFoundException(nameof(SaleLine), lineId);

        db.SaleLines.Remove(line);

        sale.UpdatedAt = DateTimeOffset.UtcNow;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> CompleteAsync(Guid id, CompleteSaleDto dto, string updatedBy, CancellationToken ct = default)
    {
        var sale = await db.Sales
            .Include(s => s.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Sale), id);

        RequireDraft(sale);

        if (sale.Lines.Count == 0)
            throw new InvalidStateException("A Sale must contain at least one line before it can be completed.");

        if (!PaymentType.All.Contains(dto.PaymentType))
            throw new InvalidStateException($"Payment type must be one of: {string.Join(", ", PaymentType.All)}.");

        var total = sale.Lines.Sum(LineTotal);
        if (dto.PaymentAmount != total)
            throw new InvalidStateException($"Payment amount {dto.PaymentAmount:0.00} must equal the Sale total of {total:0.00}.");

        var store = await db.Stores.FirstAsync(s => s.Id == sale.StoreId, ct);
        if (store.Status != StoreStatus.Active)
            throw new InvalidStateException($"Store '{store.Name}' is {store.Status} and cannot make sales.");

        var now = DateTimeOffset.UtcNow;

        // Re-validate every line against current Inventory before posting anything.
        foreach (var line in sale.Lines)
            await RequireStockAsync(sale, line.ProductVariant?.Sku ?? string.Empty, line.ProductVariantId, line.Quantity, ct);

        foreach (var line in sale.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    sale.StoreId,
                    InventoryTransactionType.Sale,
                    -line.Quantity,
                    nameof(SaleLine),
                    line.Id,
                    updatedBy),
                ct);
        }

        sale.Status = SaleStatus.Completed;
        sale.PaymentType = dto.PaymentType;
        sale.PaymentAmount = dto.PaymentAmount;
        sale.CompletedDate = now;
        sale.CompletedBy = updatedBy;
        sale.UpdatedAt = now;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> VoidAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var sale = await db.Sales
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Sale), id);

        if (sale.Status != SaleStatus.Completed)
            throw new InvalidStateException(
                $"Sale '{FormatSaleNumber(sale.SequenceNumber)}' is {sale.Status}, expected {SaleStatus.Completed}.");

        var now = DateTimeOffset.UtcNow;

        foreach (var line in sale.Lines)
        {
            await inventoryService.PostTransactionAsync(
                new PostInventoryTransactionDto(
                    line.ProductVariantId,
                    sale.StoreId,
                    InventoryTransactionType.SaleVoid,
                    line.Quantity,
                    nameof(SaleLine),
                    line.Id,
                    updatedBy),
                ct);
        }

        sale.Status = SaleStatus.Voided;
        sale.VoidedDate = now;
        sale.VoidedBy = updatedBy;
        sale.UpdatedAt = now;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    public async Task<SaleDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var sale = await RequireAsync(id, ct);
        RequireDraft(sale);

        sale.Status = SaleStatus.Cancelled;
        sale.UpdatedAt = DateTimeOffset.UtcNow;
        sale.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct) ?? ToDto(sale);
    }

    private async Task<Sale?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Sales
            .Include(s => s.Store)
            .Include(s => s.Lines).ThenInclude(l => l.ProductVariant).ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private async Task<Sale> RequireAsync(Guid id, CancellationToken ct) =>
        await db.Sales.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Sale), id);

    private static void RequireDraft(Sale sale)
    {
        if (sale.Status != SaleStatus.Draft)
            throw new InvalidStateException(
                $"Sale '{FormatSaleNumber(sale.SequenceNumber)}' is {sale.Status} and cannot be modified.");
    }

    private async Task RequireStockAsync(Sale sale, string sku, Guid productVariantId, int quantity, CancellationToken ct)
    {
        var available = await inventoryService.GetQuantityAsync(productVariantId, sale.StoreId, ct);
        if (quantity > available)
            throw new InvalidStateException(
                $"Quantity {quantity} for '{sku}' exceeds the available stock of {available} at this Store.");
    }

    private static void ValidateLine(int quantity, decimal discount, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");

        if (discount < 0)
            throw new InvalidStateException("Discount cannot be negative.");

        if (discount > quantity * unitPrice)
            throw new InvalidStateException("Discount cannot exceed the line total.");
    }

    private static decimal LineTotal(SaleLine l) => l.Quantity * l.UnitPrice - l.Discount;

    private static string FormatSaleNumber(int sequenceNumber) => $"SL-{sequenceNumber:D4}";

    private static SaleLineDto ToLineDto(SaleLine l) =>
        new(l.Id, l.SaleId, l.ProductVariantId,
            l.ProductVariant?.Sku ?? string.Empty,
            l.ProductVariant?.Barcode,
            l.ProductVariant?.Product?.Name ?? string.Empty,
            l.Quantity, l.UnitPrice, l.Discount, LineTotal(l), null,
            l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy);

    private static SaleDto ToDto(Sale s) =>
        new(s.Id,
            FormatSaleNumber(s.SequenceNumber),
            s.StoreId, s.Store?.Name ?? string.Empty,
            s.Status,
            s.PaymentType, s.PaymentAmount,
            s.Lines.Sum(LineTotal),
            s.CompletedDate, s.CompletedBy,
            s.VoidedDate, s.VoidedBy,
            s.CreatedAt, s.UpdatedAt, s.CreatedBy, s.UpdatedBy,
            s.Lines.Select(ToLineDto).ToList());
}
