namespace Longblack.Api.Models.Sales;

public record CreateSaleRequest(Guid StoreId);

public record AddSaleLineRequest(Guid ProductVariantId, int Quantity, decimal Discount = 0);

public record UpdateSaleLineRequest(int Quantity, decimal Discount = 0);

public record CompleteSaleRequest(string PaymentType, decimal PaymentAmount);

public record SaleLineResponse(
    Guid Id,
    Guid SaleId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal,
    int? AvailableQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record SaleResponse(
    Guid Id,
    string SaleNumber,
    Guid StoreId,
    string StoreName,
    string Status,
    string? PaymentType,
    decimal? PaymentAmount,
    decimal Total,
    DateTimeOffset? CompletedDate,
    string? CompletedBy,
    DateTimeOffset? VoidedDate,
    string? VoidedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<SaleLineResponse> Lines);
