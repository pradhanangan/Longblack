using Longblack.Application.Common.Paging;

namespace Longblack.Application.Sales;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> ListAsync(ListSalesFilter filter, PageRequest? page = null, CancellationToken ct = default);
    Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Rejects Closed Stores. The Warehouse Store may sell.
    Task<SaleDto> CreateAsync(CreateSaleDto dto, string createdBy, CancellationToken ct = default);

    // Lines are only editable while Draft. Adding snapshots the variant's Selling Price, and
    // consolidates into the existing line if the variant is already present.
    Task<SaleDto> AddLineAsync(Guid id, AddSaleLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<SaleDto> UpdateLineAsync(Guid id, Guid lineId, UpdateSaleLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<SaleDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default);

    // Draft -> Completed: payment amount must equal the Sale total; re-validates every line against
    // the Store's available stock, then posts one negative Sale InventoryTransaction per line.
    // Quantities above available stock are rejected.
    Task<SaleDto> CompleteAsync(Guid id, CompleteSaleDto dto, string updatedBy, CancellationToken ct = default);

    // Completed -> Voided: posts a reversing SaleVoid InventoryTransaction per line.
    Task<SaleDto> VoidAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Draft -> Cancelled only.
    Task<SaleDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default);
}
