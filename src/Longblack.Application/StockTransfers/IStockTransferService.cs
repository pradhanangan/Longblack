using Longblack.Application.Common.Paging;

namespace Longblack.Application.StockTransfers;

public interface IStockTransferService
{
    Task<PagedResult<StockTransferDto>> ListAsync(ListStockTransfersFilter filter, PageRequest? page = null, CancellationToken ct = default);
    Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Rejects: neither side is the Warehouse Store, or source == destination.
    Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto, string createdBy, CancellationToken ct = default);

    // Add/edit/remove lines are only allowed while Draft. Adding rejects a quantity exceeding the
    // source Store's current Inventory, and rejects a second line for an already-present variant.
    Task<StockTransferDto> AddLineAsync(Guid id, AddStockTransferLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<StockTransferDto> UpdateLineAsync(Guid id, Guid lineId, UpdateStockTransferLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<StockTransferDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default);

    // Draft -> Dispatched: re-validates each line against the source Store's current Inventory,
    // then posts one TransferOut InventoryTransaction per line, reducing source Inventory.
    Task<StockTransferDto> DispatchAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Dispatched -> Received: posts one TransferIn InventoryTransaction per line for the received
    // quantity (which may be less than dispatched), increasing destination Inventory.
    Task<StockTransferDto> ReceiveAsync(Guid id, ReceiveStockTransferDto dto, string updatedBy, CancellationToken ct = default);

    // Draft -> Cancelled only.
    Task<StockTransferDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default);
}
