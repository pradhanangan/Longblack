namespace Longblack.Api.Models.Receiving;

public record CreateGoodsReceiptRequest(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate,
    Guid? PurchaseOrderId = null);

public record UpdateGoodsReceiptRequest(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate,
    Guid? PurchaseOrderId = null);

public record AddGoodsReceiptLineRequest(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitCost);

public record UpdateGoodsReceiptLineRequest(
    int Quantity,
    decimal UnitCost);

public record GoodsReceiptLineResponse(
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

public record GoodsReceiptResponse(
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
    IReadOnlyList<GoodsReceiptLineResponse> Lines,
    IReadOnlyList<string> Warnings);
