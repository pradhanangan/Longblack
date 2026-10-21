using Longblack.Application.Common.Exceptions;
using Longblack.Application.Purchasing;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Purchasing;
using Longblack.Domain.Receiving;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Longblack.Infrastructure.Purchasing;

public class PurchaseOrderService(
    AppDbContext db,
    IPurchaseOrderPdfGenerator pdfGenerator,
    IEmailSender emailSender,
    IOptions<CompanyOptions> companyOptions) : IPurchaseOrderService
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> ListAsync(ListPurchaseOrdersFilter filter, CancellationToken ct = default)
    {
        var query = db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.ProductVariant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.Status == filter.Status);

        if (filter.SupplierId is not null)
            query = query.Where(p => p.SupplierId == filter.SupplierId);

        if (filter.From is not null)
            query = query.Where(p => p.CreatedAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(p => p.CreatedAt <= filter.To);

        var orders = await query
            .OrderByDescending(p => p.SequenceNumber == null)
            .ThenByDescending(p => p.SequenceNumber)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(p => ToDto(p, new Dictionary<Guid, int>(), [])).ToList();
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, ct);
        if (order is null)
            return null;

        var received = await ReceivedByVariantAsync(id, null, ct);
        var receipts = (await db.GoodsReceipts
                .Where(g => g.PurchaseOrderId == id)
                .OrderByDescending(g => g.SequenceNumber)
                .Select(g => new { g.Id, g.SequenceNumber, g.Status, g.ReceivedDate })
                .ToListAsync(ct))
            .Select(g => new LinkedGoodsReceiptDto(g.Id, $"GR-{g.SequenceNumber:D4}", g.Status, g.ReceivedDate))
            .ToList();

        return ToDto(order, received, receipts);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto, string createdBy, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FindAsync([dto.SupplierId], ct)
            ?? throw new InvalidReferenceException(nameof(Supplier), "supplierId", dto.SupplierId);

        if (supplier.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Supplier '{supplier.Name}' is inactive and cannot be used for a new purchase order.");

        var now = DateTimeOffset.UtcNow;
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = dto.SupplierId,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            Notes = NormaliseNotes(dto.Notes),
            Status = PurchaseOrderStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);

        return await GetRequiredDtoAsync(order.Id, ct);
    }

    public async Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);
        RequireDraft(order);

        var supplier = await db.Suppliers.FindAsync([dto.SupplierId], ct)
            ?? throw new InvalidReferenceException(nameof(Supplier), "supplierId", dto.SupplierId);

        if (supplier.Id != order.SupplierId && supplier.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Supplier '{supplier.Name}' is inactive and cannot be used for a purchase order.");

        order.SupplierId = dto.SupplierId;
        order.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        order.Notes = NormaliseNotes(dto.Notes);
        Touch(order, updatedBy);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> AddLineAsync(Guid id, AddPurchaseOrderLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);
        RequireDraft(order);
        ValidateLine(dto.Quantity, dto.UnitCost);

        var variant = await db.ProductVariants.FindAsync([dto.ProductVariantId], ct)
            ?? throw new InvalidReferenceException(nameof(ProductVariant), "productVariantId", dto.ProductVariantId);

        if (variant.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Product variant '{variant.Sku}' is inactive and cannot be added to a purchase order.");

        var exists = await db.PurchaseOrderLines.AnyAsync(l => l.PurchaseOrderId == id && l.ProductVariantId == dto.ProductVariantId, ct);
        if (exists)
            throw new InvalidStateException($"Product variant '{variant.Sku}' is already on this purchase order. Edit its line instead.");

        var now = DateTimeOffset.UtcNow;
        db.PurchaseOrderLines.Add(new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = id,
            ProductVariantId = dto.ProductVariantId,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = updatedBy,
            UpdatedBy = updatedBy
        });
        Touch(order, updatedBy, now);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> UpdateLineAsync(Guid id, Guid lineId, UpdatePurchaseOrderLineDto dto, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);
        RequireDraft(order);
        ValidateLine(dto.Quantity, dto.UnitCost);

        var line = await db.PurchaseOrderLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PurchaseOrderId == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrderLine), lineId);

        var now = DateTimeOffset.UtcNow;
        line.Quantity = dto.Quantity;
        line.UnitCost = dto.UnitCost;
        line.UpdatedAt = now;
        line.UpdatedBy = updatedBy;
        Touch(order, updatedBy, now);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);
        RequireDraft(order);

        var line = await db.PurchaseOrderLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PurchaseOrderId == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrderLine), lineId);

        db.PurchaseOrderLines.Remove(line);
        Touch(order, updatedBy);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> IssueAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

        RequireDraft(order);

        if (order.Lines.Count == 0)
            throw new InvalidStateException("A purchase order must contain at least one line before it can be issued.");

        var inactiveVariant = order.Lines.FirstOrDefault(l => l.ProductVariant?.Status != ReferenceDataStatus.Active);
        if (inactiveVariant is not null)
            throw new InvalidStateException(
                $"Product variant '{inactiveVariant.ProductVariant?.Sku}' is inactive and cannot be ordered.");

        if (order.Supplier?.Status != ReferenceDataStatus.Active)
            throw new InvalidStateException($"Supplier '{order.Supplier?.Name}' is inactive and cannot be ordered from.");

        var sequence = await db.Database
            .SqlQuery<int>($"SELECT nextval('purchase_order_seq') AS \"Value\"")
            .SingleAsync(ct);

        var now = DateTimeOffset.UtcNow;
        order.SequenceNumber = sequence;
        order.Status = PurchaseOrderStatus.Issued;
        order.IssuedAt = now;
        order.IssuedBy = updatedBy;
        Touch(order, updatedBy, now);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, string updatedBy, bool canCancelIssued, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);

        if (order.Status == PurchaseOrderStatus.Issued)
        {
            if (!canCancelIssued)
                throw new InvalidStateException("Only an Admin can cancel an issued purchase order.");

            var anyReceived = await db.GoodsReceipts.AnyAsync(
                g => g.PurchaseOrderId == id && g.Status == GoodsReceiptStatus.Received, ct);
            if (anyReceived)
                throw new InvalidStateException(
                    $"Purchase order '{FormatNumber(order)}' has received goods and cannot be cancelled. Close it instead.");
        }
        else if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidStateException(
                $"Purchase order '{FormatNumber(order)}' is {order.Status} and cannot be cancelled.");
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        Touch(order, updatedBy);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> CloseAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);

        if (order.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived))
            throw new InvalidStateException(
                $"Purchase order '{FormatNumber(order)}' is {order.Status} and cannot be closed.");

        order.Status = PurchaseOrderStatus.Closed;
        Touch(order, updatedBy);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> MarkSentAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);
        RequireIssuedOrLater(order);

        var now = DateTimeOffset.UtcNow;
        order.SentAt = now;
        order.SentBy = updatedBy;
        Touch(order, updatedBy, now);

        await db.SaveChangesAsync(ct);
        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderDto> SendEmailAsync(Guid id, SendPurchaseOrderEmailDto dto, string updatedBy, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, ct) ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        RequireIssuedOrLater(order);

        var recipient = string.IsNullOrWhiteSpace(dto.Recipient) ? order.Supplier?.Email : dto.Recipient.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
            throw new InvalidStateException("A recipient email address is required.");

        var cc = string.IsNullOrWhiteSpace(dto.Cc) ? null : dto.Cc.Trim();
        var pdf = await BuildPdfAsync(order, ct);
        var company = companyOptions.Value;
        var number = FormatNumber(order);

        var message = new EmailMessage(
            recipient,
            cc,
            $"Purchase Order {number}" + (string.IsNullOrWhiteSpace(company.Name) ? string.Empty : $" from {company.Name}"),
            $"Hello {order.Supplier?.Name},\n\nPlease find attached purchase order {number}.\n\nKind regards,\n{company.Name}",
            [new EmailAttachment(pdf.FileName, "application/pdf", pdf.Content)]);

        var now = DateTimeOffset.UtcNow;
        string? error = null;
        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            error = ex.Message;
        }

        db.PurchaseOrderSends.Add(new PurchaseOrderSend
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = id,
            Recipient = recipient,
            Cc = cc,
            Succeeded = error is null,
            Error = error,
            SentAt = now,
            SentBy = updatedBy
        });

        if (error is null)
        {
            order.SentAt = now;
            order.SentBy = updatedBy;
        }

        Touch(order, updatedBy, now);
        await db.SaveChangesAsync(ct);

        if (error is not null)
            throw new InvalidStateException($"The email could not be sent: {error}");

        return await GetRequiredDtoAsync(id, ct);
    }

    public async Task<PurchaseOrderPdfDto> GetPdfAsync(Guid id, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, ct) ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        RequireIssuedOrLater(order);
        return await BuildPdfAsync(order, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, PurchaseOrderLineBalance>> GetLineBalancesAsync(
        Guid id, Guid? excludeGoodsReceiptId = null, CancellationToken ct = default)
    {
        var lines = await db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == id).ToListAsync(ct);
        var received = await ReceivedByVariantAsync(id, excludeGoodsReceiptId, ct);

        return lines.ToDictionary(
            l => l.ProductVariantId,
            l => new PurchaseOrderLineBalance(l.Quantity, received.GetValueOrDefault(l.ProductVariantId), l.UnitCost));
    }

    public async Task RefreshStatusAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var order = await RequireAsync(id, ct);

        if (order.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
            return;

        var balances = await GetLineBalancesAsync(id, null, ct);
        var anyReceived = balances.Values.Any(b => b.Received > 0);
        var allReceived = balances.Count > 0 && balances.Values.All(b => b.Received >= b.Ordered);

        var status = allReceived
            ? PurchaseOrderStatus.Received
            : anyReceived ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Issued;

        if (status == order.Status)
            return;

        order.Status = status;
        Touch(order, updatedBy);
        await db.SaveChangesAsync(ct);
    }

    private async Task<PurchaseOrderPdfDto> BuildPdfAsync(PurchaseOrder order, CancellationToken ct)
    {
        var variantIds = order.Lines.Select(l => l.ProductVariantId).ToList();
        var variants = await db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var company = companyOptions.Value;
        var lines = order.Lines
            .OrderBy(l => l.ProductVariant?.Sku)
            .Select(l =>
            {
                var v = variants[l.ProductVariantId];
                return new PurchaseOrderPdfLine(
                    v.Sku,
                    $"{v.Product?.Name} - {v.Colour?.Name} / {v.Size?.Name}",
                    l.Quantity,
                    l.UnitCost);
            })
            .ToList();

        var number = FormatNumber(order);
        var bytes = pdfGenerator.Generate(new PurchaseOrderPdfModel(
            company.Name,
            company.Address,
            number,
            order.IssuedAt ?? order.CreatedAt,
            order.ExpectedDeliveryDate,
            order.Supplier?.Name ?? string.Empty,
            order.Supplier?.Email ?? string.Empty,
            order.Supplier?.Phone ?? string.Empty,
            order.Notes,
            lines));

        return new PurchaseOrderPdfDto(bytes, $"{number}.pdf");
    }

    private async Task<Dictionary<Guid, int>> ReceivedByVariantAsync(Guid id, Guid? excludeGoodsReceiptId, CancellationToken ct) =>
        await db.GoodsReceiptLines
            .Where(l => l.GoodsReceipt!.PurchaseOrderId == id
                        && l.GoodsReceipt.Status == GoodsReceiptStatus.Received
                        && (excludeGoodsReceiptId == null || l.GoodsReceiptId != excludeGoodsReceiptId))
            .GroupBy(l => l.ProductVariantId)
            .Select(g => new { VariantId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Quantity, ct);

    private async Task<PurchaseOrder?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Lines).ThenInclude(l => l.ProductVariant)
            .Include(p => p.Sends)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    private async Task<PurchaseOrder> RequireAsync(Guid id, CancellationToken ct) =>
        await db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

    private async Task<PurchaseOrderDto> GetRequiredDtoAsync(Guid id, CancellationToken ct) =>
        await GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(PurchaseOrder), id);

    private static void RequireDraft(PurchaseOrder order)
    {
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidStateException(
                $"Purchase order '{FormatNumber(order)}' is {order.Status} and cannot be modified.");
    }

    private static void RequireIssuedOrLater(PurchaseOrder order)
    {
        if (order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled)
            throw new InvalidStateException(
                $"Purchase order is {order.Status}; it must be issued first.");
    }

    private static void ValidateLine(int quantity, decimal unitCost)
    {
        if (quantity <= 0)
            throw new InvalidStateException("Quantity must be greater than zero.");
        if (unitCost < 0)
            throw new InvalidStateException("Unit cost cannot be negative.");
    }

    private static void Touch(PurchaseOrder order, string updatedBy, DateTimeOffset? now = null)
    {
        order.UpdatedAt = now ?? DateTimeOffset.UtcNow;
        order.UpdatedBy = updatedBy;
    }

    private static string? NormaliseNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static string FormatNumber(PurchaseOrder order) =>
        order.SequenceNumber is { } n ? $"PO-{n:D4}" : "Draft";

    private static PurchaseOrderDto ToDto(
        PurchaseOrder p,
        IReadOnlyDictionary<Guid, int> receivedByVariant,
        IReadOnlyList<LinkedGoodsReceiptDto> receipts)
    {
        var lines = p.Lines
            .OrderBy(l => l.CreatedAt)
            .Select(l => new PurchaseOrderLineDto(
                l.Id, l.PurchaseOrderId, l.ProductVariantId,
                l.ProductVariant?.Sku ?? string.Empty,
                l.ProductVariant?.Barcode,
                l.Quantity, l.UnitCost,
                receivedByVariant.GetValueOrDefault(l.ProductVariantId),
                l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy))
            .ToList();

        var overdue = p.ExpectedDeliveryDate is { } due
                      && due.UtcDateTime.Date < DateTime.UtcNow.Date
                      && p.Status is PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived;

        return new PurchaseOrderDto(
            p.Id,
            p.SequenceNumber is { } n ? $"PO-{n:D4}" : null,
            p.SupplierId,
            p.Supplier?.Name ?? string.Empty,
            p.Supplier?.Email ?? string.Empty,
            p.ExpectedDeliveryDate,
            p.Notes,
            p.Status,
            overdue,
            p.IssuedAt,
            p.IssuedBy,
            p.SentAt,
            p.SentBy,
            lines.Sum(l => l.Quantity * l.UnitCost),
            p.CreatedAt,
            p.UpdatedAt,
            p.CreatedBy,
            p.UpdatedBy,
            lines,
            p.Sends
                .OrderByDescending(s => s.SentAt)
                .Select(s => new PurchaseOrderSendDto(s.Id, s.Recipient, s.Cc, s.Succeeded, s.Error, s.SentAt, s.SentBy))
                .ToList(),
            receipts);
    }
}
