namespace Longblack.Application.StockTake;

public interface IStockTakeService
{
    Task<IReadOnlyList<StockTakeDto>> ListAsync(ListStockTakesFilter filter, CancellationToken ct = default);
    Task<StockTakeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockTakeDto> CreateAsync(CreateStockTakeDto dto, string createdBy, CancellationToken ct = default);

    // Draft -> InProgress: generates StockTakeItem rows for every Active variant matching the
    // scope filter, snapshotting ExpectedQuantity from current Inventory (0 if never received).
    Task<StockTakeDto> StartAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Only allowed while the item is still Pending (not yet counted).
    Task<StockTakeDto> RemoveItemAsync(Guid id, Guid itemId, string updatedBy, CancellationToken ct = default);

    // Appends a new StockTakeCount (never edits/overwrites a previous one) and mirrors it onto
    // the item's CountedQuantity; moves the item's Status to Counted.
    Task<StockTakeDto> RecordCountAsync(Guid id, Guid itemId, RecordCountDto dto, string countedBy, CancellationToken ct = default);
    Task<IReadOnlyList<StockTakeCountDto>> GetCountsAsync(Guid id, Guid itemId, CancellationToken ct = default);

    // InProgress -> Completed: requires every item to be Counted (hard gate).
    Task<StockTakeDto> CompleteAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Completed -> InProgress: does not reset any item's Status/CountedQuantity.
    Task<StockTakeDto> ReopenAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Completed -> Approved: posts one StockTakeAdjustment InventoryTransaction per item with
    // nonzero Variance; zero-variance items get no transaction. Terminal and read-only after this.
    Task<StockTakeDto> ApproveAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Draft or InProgress -> Cancelled only.
    Task<StockTakeDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default);
}
