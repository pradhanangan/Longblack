using Longblack.Application.Common.Paging;

namespace Longblack.Application.Reports;

// Read-only report queries over live data; no pre-aggregation.
public interface IReportService
{
    // One row per Active variant per Store (zero when no Inventory row exists). A null page returns every row (CSV export).
    Task<StockOnHandReportDto> GetStockOnHandAsync(StockOnHandFilter filter, PageRequest? page, CancellationToken ct = default);

    // Inventory Transaction ledger, newest first. A null page returns every row (CSV export).
    Task<PagedResult<StockMovementRowDto>> GetStockMovementAsync(StockMovementFilter filter, PageRequest? page, CancellationToken ct = default);

    // Completed Sales by completion time (NZ local days); Voided Sales are reported separately, never in the totals.
    Task<SalesSummaryDto> GetSalesSummaryAsync(SalesSummaryFilter filter, CancellationToken ct = default);

    // Approved and Completed Stock Takes by completion date, newest first. A null page returns every row.
    Task<PagedResult<StockTakeVarianceRowDto>> GetStockTakeVarianceAsync(StockTakeVarianceFilter filter, PageRequest? page, CancellationToken ct = default);

    // Counted items with nonzero Variance for one Stock Take.
    Task<StockTakeVarianceDetailDto> GetStockTakeVarianceDetailAsync(Guid stockTakeId, CancellationToken ct = default);
}
