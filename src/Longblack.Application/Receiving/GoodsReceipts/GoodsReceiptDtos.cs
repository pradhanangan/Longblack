namespace Longblack.Application.Receiving.GoodsReceipts;

public record GoodsReceiptLineDto(
    Guid Id,
    Guid GoodsReceiptId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    int Quantity,
    decimal UnitCost,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record GoodsReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid StoreId,
    string StoreName,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    DateTimeOffset ReceivedDate,
    string Status,
    string? ReceivedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<GoodsReceiptLineDto> Lines,
    IReadOnlyList<string> Warnings);

public record CreateGoodsReceiptDto(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate,
    Guid? PurchaseOrderId = null);

// Header fields are only editable while the receipt is Draft.
public record UpdateGoodsReceiptDto(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate,
    Guid? PurchaseOrderId = null);

public record AddGoodsReceiptLineDto(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitCost);

public record UpdateGoodsReceiptLineDto(
    int Quantity,
    decimal UnitCost);

public record ListGoodsReceiptsFilter(
    string? Status,
    Guid? SupplierId);
