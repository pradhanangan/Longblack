namespace Longblack.Application.StockTake;

public record StockTakeItemDto(
    Guid Id,
    Guid StockTakeId,
    Guid ProductVariantId,
    string Sku,
    string? Barcode,
    string ProductName,
    string? ColourName,
    string? SizeName,
    int ExpectedQuantity,
    int? CountedQuantity,
    int? Variance,
    string Status);

public record StockTakeDto(
    Guid Id,
    string ReferenceNumber,
    Guid? BrandId,
    string? BrandName,
    Guid? CategoryId,
    string? CategoryName,
    string Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? CompletionDate,
    string? CompletedBy,
    DateTimeOffset? ApprovedDate,
    string? ApprovedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    IReadOnlyList<StockTakeItemDto> Items);

public record StockTakeCountDto(
    Guid Id,
    Guid StockTakeItemId,
    int Quantity,
    DateTimeOffset CountedAt,
    string CountedBy);

public record CreateStockTakeDto(
    Guid? BrandId,
    Guid? CategoryId);

public record RecordCountDto(
    int Quantity);

public record ListStockTakesFilter(
    string? Status);
