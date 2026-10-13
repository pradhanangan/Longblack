namespace Longblack.Application.StockTransfers;

public record StockTransferLineDto(
    Guid Id,
    Guid StockTransferId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    int DispatchedQuantity,
    int? ReceivedQuantity,
    // Dispatched - Received; null until Received.
    int? Variance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record StockTransferDto(
    Guid Id,
    string TransferNumber,
    Guid SourceStoreId,
    string SourceStoreName,
    Guid DestinationStoreId,
    string DestinationStoreName,
    string Status,
    DateTimeOffset? DispatchedDate,
    string? DispatchedBy,
    DateTimeOffset? ReceivedDate,
    string? ReceivedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<StockTransferLineDto> Lines);

public record CreateStockTransferDto(Guid SourceStoreId, Guid DestinationStoreId);

public record AddStockTransferLineDto(Guid ProductVariantId, int Quantity);

public record UpdateStockTransferLineDto(int Quantity);

public record ReceiveStockTransferLineDto(Guid LineId, int ReceivedQuantity);

public record ReceiveStockTransferDto(IReadOnlyList<ReceiveStockTransferLineDto> Lines);

public record ListStockTransfersFilter(
    string? Status,
    Guid? SourceStoreId,
    Guid? DestinationStoreId);
