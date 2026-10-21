namespace Longblack.Api.Models.Purchasing;

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Notes);

public record UpdatePurchaseOrderRequest(
    Guid SupplierId,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Notes);

public record AddPurchaseOrderLineRequest(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitCost);

public record UpdatePurchaseOrderLineRequest(
    int Quantity,
    decimal UnitCost);

public record SendPurchaseOrderEmailRequest(
    string? Recipient,
    string? Cc);

public record PurchaseOrderLineResponse(
    Guid Id,
    Guid PurchaseOrderId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    int Quantity,
    decimal UnitCost,
    int ReceivedQuantity,
    int OutstandingQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record PurchaseOrderSendResponse(
    Guid Id,
    string Recipient,
    string? Cc,
    bool Succeeded,
    string? Error,
    DateTimeOffset SentAt,
    string SentBy);

public record LinkedGoodsReceiptResponse(
    Guid Id,
    string ReceiptNumber,
    string Status,
    DateTimeOffset ReceivedDate);

public record PurchaseOrderResponse(
    Guid Id,
    string? PoNumber,
    Guid SupplierId,
    string SupplierName,
    string SupplierEmail,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Notes,
    string Status,
    bool IsOverdue,
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    DateTimeOffset? SentAt,
    string? SentBy,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<PurchaseOrderLineResponse> Lines,
    IReadOnlyList<PurchaseOrderSendResponse> Sends,
    IReadOnlyList<LinkedGoodsReceiptResponse> GoodsReceipts);
