using Longblack.Application.Common.Paging;

namespace Longblack.Application.Reports;

public record StockOnHandFilter(
    // Null means every Active Store.
    Guid? StoreId,
    Guid? BrandId,
    Guid? CategoryId,
    bool LowStockOnly);

public record StockOnHandRowDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string? ColourName,
    string? SizeName,
    Guid StoreId,
    string StoreName,
    int Quantity,
    bool IsLowStock);

public record StockOnHandReportDto(int LowStockThreshold, PagedResult<StockOnHandRowDto> Rows);

public record StockMovementFilter(
    Guid? StoreId,
    // Matches SKU or product name.
    string? SearchQuery,
    // An InventoryTransactionType value.
    string? Type,
    // Matches the user who made the movement (contains, case-insensitive).
    string? User,
    DateOnly? From,
    DateOnly? To);

public record StockMovementRowDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Sku,
    string ProductName,
    string? ColourName,
    string? SizeName,
    Guid StoreId,
    string StoreName,
    string Type,
    int QuantityDelta,
    string CreatedBy);

public static class SalesSummaryGroupBy
{
    public const string Day = "day";
    public const string Product = "product";
    public const string Category = "category";
    public const string Brand = "brand";
    public const string Store = "store";

    public static readonly string[] All = [Day, Product, Category, Brand, Store];
}

public record SalesSummaryFilter(
    Guid? StoreId,
    Guid? BrandId,
    Guid? CategoryId,
    DateOnly? From,
    DateOnly? To,
    string GroupBy);

// All amounts are GST-inclusive except ExGst.
public record SalesSummaryRowDto(string Key, string Label, int SaleCount, int Units, decimal Total, decimal Gst, decimal ExGst);

public record SalesSummaryTotalsDto(int SaleCount, int Units, decimal Total, decimal Gst, decimal ExGst);

public record VoidedSalesDto(int SaleCount, decimal Total);

public record SalesSummaryDto(
    SalesSummaryTotalsDto Totals,
    VoidedSalesDto Voided,
    IReadOnlyList<SalesSummaryRowDto> Rows,
    // Always grouped by NZ local day, regardless of GroupBy, to drive the sales-over-time chart.
    IReadOnlyList<SalesSummaryRowDto> ByDay);

public record StockTakeVarianceFilter(Guid? StoreId, DateOnly? From, DateOnly? To);

public record StockTakeVarianceRowDto(
    Guid StockTakeId,
    string ReferenceNumber,
    Guid StoreId,
    string StoreName,
    // Approved, or Completed (pending approval).
    string Status,
    DateTimeOffset? CompletionDate,
    DateTimeOffset? ApprovedDate,
    int CountedItems,
    // Sum of negative Variances (zero or negative).
    int Shrinkage,
    // Sum of positive Variances (zero or positive).
    int Overage);

public record StockTakeVarianceItemDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string? ColourName,
    string? SizeName,
    int ExpectedQuantity,
    int CountedQuantity,
    int Variance);

public record StockTakeVarianceDetailDto(
    StockTakeVarianceRowDto StockTake,
    IReadOnlyList<StockTakeVarianceItemDto> Items);
