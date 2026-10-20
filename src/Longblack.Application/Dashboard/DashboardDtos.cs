namespace Longblack.Application.Dashboard;

public record DashboardDto(
    int TotalUnitsOnHand,
    int VariantsAtZeroOrBelow,
    int DraftGoodsReceipts,
    int TransfersInTransit,
    int StockTakesOpen,
    // Completed Sales over the last 7 days, respecting the Store selector.
    decimal SalesTotalLast7Days,
    IReadOnlyList<DashboardSaleDto> RecentSales,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions);

public record DashboardSaleDto(
    Guid Id,
    string SaleNumber,
    string StoreName,
    decimal Total,
    DateTimeOffset CompletedDate,
    string CompletedBy);

public record DashboardTransactionDto(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string StoreName,
    string Type,
    int QuantityDelta,
    DateTimeOffset CreatedAt,
    string CreatedBy);
