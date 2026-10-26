using Longblack.Application.Common.Paging;

namespace Longblack.Application.Purchasing;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderDto>> ListAsync(ListPurchaseOrdersFilter filter, PageRequest? page = null, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto, string createdBy, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto, string updatedBy, CancellationToken ct = default);

    Task<PurchaseOrderDto> AddLineAsync(Guid id, AddPurchaseOrderLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateLineAsync(Guid id, Guid lineId, UpdatePurchaseOrderLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<PurchaseOrderDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default);

    // Draft -> Issued: requires >=1 line; assigns the PO number and locks the lines.
    Task<PurchaseOrderDto> IssueAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Draft -> Cancelled (any editor); Issued -> Cancelled only while nothing has been received and only if canCancelIssued (Admin).
    Task<PurchaseOrderDto> CancelAsync(Guid id, string updatedBy, bool canCancelIssued, CancellationToken ct = default);

    // Issued/PartiallyReceived -> Closed: force-close when a Supplier short-ships.
    Task<PurchaseOrderDto> CloseAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Records SentAt/SentBy without emailing (e.g. the order was phoned through).
    Task<PurchaseOrderDto> MarkSentAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Emails the PDF; every attempt is logged, and SentAt/SentBy are updated only on success.
    Task<PurchaseOrderDto> SendEmailAsync(Guid id, SendPurchaseOrderEmailDto dto, string updatedBy, CancellationToken ct = default);

    Task<PurchaseOrderPdfDto> GetPdfAsync(Guid id, CancellationToken ct = default);

    // Ordered quantity and Received-receipt quantity per variant, optionally excluding one Goods Receipt.
    Task<IReadOnlyDictionary<Guid, PurchaseOrderLineBalance>> GetLineBalancesAsync(
        Guid id, Guid? excludeGoodsReceiptId = null, CancellationToken ct = default);

    // Recomputes Issued/PartiallyReceived/Received from Received Goods Receipts. Does not touch Closed or Cancelled.
    Task RefreshStatusAsync(Guid id, string updatedBy, CancellationToken ct = default);
}

public record PurchaseOrderLineBalance(int Ordered, int Received, decimal ExpectedUnitCost)
{
    public int Outstanding => Math.Max(0, Ordered - Received);
}
