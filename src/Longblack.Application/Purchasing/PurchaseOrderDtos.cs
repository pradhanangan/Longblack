namespace Longblack.Application.Purchasing;

public record PurchaseOrderLineDto(
    Guid Id,
    Guid PurchaseOrderId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    int Quantity,
    decimal UnitCost,
    int ReceivedQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record PurchaseOrderSendDto(
    Guid Id,
    string Recipient,
    string? Cc,
    bool Succeeded,
    string? Error,
    DateTimeOffset SentAt,
    string SentBy);

public record LinkedGoodsReceiptDto(
    Guid Id,
    string ReceiptNumber,
    string Status,
    DateTimeOffset ReceivedDate);

public record PurchaseOrderDto(
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
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    IReadOnlyList<PurchaseOrderSendDto> Sends,
    IReadOnlyList<LinkedGoodsReceiptDto> GoodsReceipts);

public record CreatePurchaseOrderDto(
    Guid SupplierId,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Notes);

// Header fields are only editable while the Purchase Order is Draft.
public record UpdatePurchaseOrderDto(
    Guid SupplierId,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Notes);

public record AddPurchaseOrderLineDto(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitCost);

public record UpdatePurchaseOrderLineDto(
    int Quantity,
    decimal UnitCost);

public record SendPurchaseOrderEmailDto(
    string? Recipient,
    string? Cc);

public record PurchaseOrderPdfDto(
    byte[] Content,
    string FileName);

public record ListPurchaseOrdersFilter(
    string? Status,
    Guid? SupplierId,
    DateTimeOffset? From,
    DateTimeOffset? To);
