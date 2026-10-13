namespace Longblack.Api.Models.StockTransfers;

public record CreateStockTransferRequest(Guid SourceStoreId, Guid DestinationStoreId);

public record AddStockTransferLineRequest(Guid ProductVariantId, int Quantity);

public record UpdateStockTransferLineRequest(int Quantity);

public record ReceiveStockTransferLineRequest(Guid LineId, int ReceivedQuantity);

public record ReceiveStockTransferRequest(IReadOnlyList<ReceiveStockTransferLineRequest> Lines);

public record StockTransferLineResponse(
    Guid Id,
    Guid StockTransferId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    int DispatchedQuantity,
    int? ReceivedQuantity,
    int? Variance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record StockTransferResponse(
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
    IReadOnlyList<StockTransferLineResponse> Lines);
