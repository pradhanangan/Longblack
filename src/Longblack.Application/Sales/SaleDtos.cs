namespace Longblack.Application.Sales;

public record SaleLineDto(
    Guid Id,
    Guid SaleId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    // Quantity * UnitPrice - Discount.
    decimal LineTotal,
    // Current stock at the Sale's Store; only populated when fetching a single Sale.
    int? AvailableQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record SaleDto(
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
    IReadOnlyList<SaleLineDto> Lines);

public record CreateSaleDto(Guid StoreId);

public record AddSaleLineDto(Guid ProductVariantId, int Quantity, decimal Discount);

public record UpdateSaleLineDto(int Quantity, decimal Discount);

public record CompleteSaleDto(string PaymentType, decimal PaymentAmount);

public record ListSalesFilter(
    string? Status,
    Guid? StoreId,
    DateTimeOffset? From,
    DateTimeOffset? To);
