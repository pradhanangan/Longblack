namespace Longblack.Api.Models.StockTake;

public record CreateStockTakeRequest(
    Guid? BrandId,
    Guid? CategoryId);

public record RecordCountRequest(
    int Quantity);

public record StockTakeItemResponse(
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

public record StockTakeResponse(
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
    IReadOnlyList<StockTakeItemResponse> Items);

public record StockTakeCountResponse(
    Guid Id,
    Guid StockTakeItemId,
    int Quantity,
    DateTimeOffset CountedAt,
    string CountedBy);
