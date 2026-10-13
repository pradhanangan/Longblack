namespace Longblack.Application.Dashboard;

public record DashboardDto(
    int TotalUnitsOnHand,
    int VariantsAtZeroOrBelow,
    int DraftGoodsReceipts,
    int TransfersInTransit,
    int StockTakesOpen,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions);

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
