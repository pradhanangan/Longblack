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
    DateTimeOffset ReceivedDate,
    string Status,
    string? ReceivedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<GoodsReceiptLineDto> Lines);

public record CreateGoodsReceiptDto(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate);

// Header fields are only editable while the receipt is Draft.
public record UpdateGoodsReceiptDto(
    Guid SupplierId,
    DateTimeOffset? ReceivedDate);

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
