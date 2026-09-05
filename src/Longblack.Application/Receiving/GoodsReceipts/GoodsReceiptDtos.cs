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
    string SupplierCode,
    DateTimeOffset ReceivedDate,
    string Status,
    string? ReceivedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<GoodsReceiptLineDto> Lines);

public record CreateGoodsReceiptDto(
    string SupplierCode,
    DateTimeOffset? ReceivedDate);

// Header fields are only editable while the receipt is Draft.
public record UpdateGoodsReceiptDto(
    string SupplierCode,
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
    string? SupplierCode);
