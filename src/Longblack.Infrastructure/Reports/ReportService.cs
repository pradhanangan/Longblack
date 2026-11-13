using Longblack.Application.Common.Exceptions;
using Longblack.Application.Common.Paging;
using Longblack.Application.Reports;
using Longblack.Application.Settings;
using Longblack.Domain.Catalogue;
using Longblack.Domain.Sales;
using Longblack.Domain.StockTake;
using Longblack.Domain.Stores;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Reports;

public class ReportService(AppDbContext db, IAppSettingsService settingsService) : IReportService
{
    public async Task<StockOnHandReportDto> GetStockOnHandAsync(StockOnHandFilter filter, PageRequest? page, CancellationToken ct = default)
    {
        var threshold = await settingsService.GetLowStockThresholdAsync(ct);

        var variants = db.ProductVariants.Where(v => v.Status == ReferenceDataStatus.Active);
        if (filter.BrandId is not null)
            variants = variants.Where(v => v.Product!.BrandId == filter.BrandId);
        if (filter.CategoryId is not null)
            variants = variants.Where(v => v.Product!.CategoryId == filter.CategoryId);

        var storeId = filter.StoreId;
        var stores = db.Stores.Where(s => storeId == null ? s.Status == StoreStatus.Active : s.Id == storeId);

        var rows =
            from v in variants
            from s in stores
            select new
            {
                v.Id,
                v.Sku,
                ProductName = v.Product!.Name,
                ColourName = v.Colour != null ? v.Colour.Name : null,
                SizeName = v.Size != null ? v.Size.Name : null,
                StoreId = s.Id,
                StoreName = s.Name,
                Quantity = db.Inventory
                    .Where(i => i.ProductVariantId == v.Id && i.StoreId == s.Id)
                    .Select(i => (int?)i.Quantity)
                    .FirstOrDefault() ?? 0
            };

        if (filter.LowStockOnly)
            rows = rows.Where(r => r.Quantity <= threshold);

        var result = await rows.OrderBy(r => r.Sku).ThenBy(r => r.StoreName).ThenBy(r => r.Id)
            .ToPagedResultAsync(page, r => new StockOnHandRowDto(
                r.Id, r.Sku, r.ProductName, r.ColourName, r.SizeName,
                r.StoreId, r.StoreName, r.Quantity, r.Quantity <= threshold), ct);

        return new(threshold, result);
    }

    public async Task<PagedResult<StockMovementRowDto>> GetStockMovementAsync(StockMovementFilter filter, PageRequest? page, CancellationToken ct = default)
    {
        var query = db.InventoryTransactions.AsQueryable();

        if (filter.StoreId is not null)
            query = query.Where(t => t.StoreId == filter.StoreId);
        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(t => t.Type == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            var user = filter.User.Trim().ToLower();
            query = query.Where(t => t.CreatedBy.ToLower().Contains(user));
        }
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var q = filter.SearchQuery.Trim().ToLower();
            query = query.Where(t => t.ProductVariant!.Sku.ToLower().Contains(q) || t.ProductVariant.Product!.Name.ToLower().Contains(q));
        }
        if (NzTime.StartOfDay(filter.From) is { } start)
            query = query.Where(t => t.CreatedAt >= start);
        if (NzTime.EndOfDay(filter.To) is { } end)
            query = query.Where(t => t.CreatedAt < end);

        return await query
            .Select(t => new
            {
                t.Id,
                t.CreatedAt,
                t.ProductVariant!.Sku,
                ProductName = t.ProductVariant.Product!.Name,
                ColourName = t.ProductVariant.Colour != null ? t.ProductVariant.Colour.Name : null,
                SizeName = t.ProductVariant.Size != null ? t.ProductVariant.Size.Name : null,
                t.StoreId,
                StoreName = t.Store!.Name,
                t.Type,
                t.QuantityDelta,
                t.CreatedBy
            })
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToPagedResultAsync(page, r => new StockMovementRowDto(
                r.Id, r.CreatedAt, r.Sku, r.ProductName, r.ColourName, r.SizeName,
                r.StoreId, r.StoreName, r.Type, r.QuantityDelta, r.CreatedBy), ct);
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(SalesSummaryFilter filter, CancellationToken ct = default)
    {
        var lines = db.SaleLines.Where(l => l.Sale!.Status == SaleStatus.Completed || l.Sale.Status == SaleStatus.Voided);

        if (filter.StoreId is not null)
            lines = lines.Where(l => l.Sale!.StoreId == filter.StoreId);
        if (filter.BrandId is not null)
            lines = lines.Where(l => l.ProductVariant!.Product!.BrandId == filter.BrandId);
        if (filter.CategoryId is not null)
            lines = lines.Where(l => l.ProductVariant!.Product!.CategoryId == filter.CategoryId);
        if (NzTime.StartOfDay(filter.From) is { } start)
            lines = lines.Where(l => l.Sale!.CompletedDate >= start);
        if (NzTime.EndOfDay(filter.To) is { } end)
            lines = lines.Where(l => l.Sale!.CompletedDate < end);

        // Lines are projected to plain values and grouped in memory because day buckets use NZ local time.
        var data = await lines.Select(l => new
        {
            l.SaleId,
            Voided = l.Sale!.Status == SaleStatus.Voided,
            CompletedDate = l.Sale.CompletedDate,
            l.Sale.StoreId,
            StoreName = l.Sale.Store!.Name,
            ProductId = l.ProductVariant!.ProductId,
            ProductName = l.ProductVariant.Product!.Name,
            CategoryName = l.ProductVariant.Product.Category != null ? l.ProductVariant.Product.Category.Name : null,
            BrandName = l.ProductVariant.Product.Brand != null ? l.ProductVariant.Product.Brand.Name : null,
            l.Quantity,
            Gross = l.Quantity * l.UnitPrice - l.Discount,
            Gst = l.GstAmount ?? 0m
        }).ToListAsync(ct);

        var completed = data.Where(d => !d.Voided).ToList();
        var voided = data.Where(d => d.Voided).ToList();

        static SalesSummaryRowDto Row(string key, string label, IEnumerable<(Guid SaleId, int Quantity, decimal Gross, decimal Gst)> rows)
        {
            var list = rows.ToList();
            var total = list.Sum(r => r.Gross);
            var gst = list.Sum(r => r.Gst);
            return new(key, label, list.Select(r => r.SaleId).Distinct().Count(), list.Sum(r => r.Quantity), total, gst, total - gst);
        }

        static IReadOnlyList<SalesSummaryRowDto> Group<T>(
            IEnumerable<T> source,
            Func<T, (string Key, string Label)> keyOf,
            Func<T, (Guid SaleId, int Quantity, decimal Gross, decimal Gst)> valuesOf) =>
            source.GroupBy(keyOf)
                .Select(g => Row(g.Key.Key, g.Key.Label, g.Select(valuesOf)))
                .OrderBy(r => r.Key, StringComparer.Ordinal)
                .ToList();

        var byDay = Group(completed,
            d => (NzTime.LocalDate(d.CompletedDate!.Value).ToString("yyyy-MM-dd"), NzTime.LocalDate(d.CompletedDate!.Value).ToString("yyyy-MM-dd")),
            d => (d.SaleId, d.Quantity, d.Gross, d.Gst));

        var rows = filter.GroupBy switch
        {
            SalesSummaryGroupBy.Product => Group(completed, d => (d.ProductId.ToString(), d.ProductName), d => (d.SaleId, d.Quantity, d.Gross, d.Gst))
                .OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase).ToList(),
            SalesSummaryGroupBy.Category => Group(completed, d => (d.CategoryName ?? "", d.CategoryName ?? "Uncategorised"), d => (d.SaleId, d.Quantity, d.Gross, d.Gst)),
            SalesSummaryGroupBy.Brand => Group(completed, d => (d.BrandName ?? "", d.BrandName ?? "No brand"), d => (d.SaleId, d.Quantity, d.Gross, d.Gst)),
            SalesSummaryGroupBy.Store => Group(completed, d => (d.StoreId.ToString(), d.StoreName), d => (d.SaleId, d.Quantity, d.Gross, d.Gst))
                .OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => byDay
        };

        var totalRow = Row("", "", completed.Select(d => (d.SaleId, d.Quantity, d.Gross, d.Gst)));
        return new(
            new(totalRow.SaleCount, totalRow.Units, totalRow.Total, totalRow.Gst, totalRow.ExGst),
            new(voided.Select(d => d.SaleId).Distinct().Count(), voided.Sum(d => d.Gross)),
            rows,
            byDay);
    }

    public async Task<PagedResult<StockTakeVarianceRowDto>> GetStockTakeVarianceAsync(StockTakeVarianceFilter filter, PageRequest? page, CancellationToken ct = default) =>
        await VarianceRows(filter, null).ToPagedResultAsync(page, ToRowDto, ct);

    public async Task<StockTakeVarianceDetailDto> GetStockTakeVarianceDetailAsync(Guid stockTakeId, CancellationToken ct = default)
    {
        var header = await VarianceRows(new(null, null, null), stockTakeId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("StockTake", stockTakeId);

        var items = await db.StockTakes.Where(s => s.Id == stockTakeId)
            .SelectMany(s => s.Items)
            .Where(i => i.CountedQuantity != null && i.CountedQuantity != i.ExpectedQuantity)
            .OrderBy(i => i.ProductVariant!.Sku)
            .Select(i => new StockTakeVarianceItemDto(
                i.ProductVariantId, i.ProductVariant!.Sku, i.ProductVariant.Product!.Name,
                i.ProductVariant.Colour != null ? i.ProductVariant.Colour.Name : null,
                i.ProductVariant.Size != null ? i.ProductVariant.Size.Name : null,
                i.ExpectedQuantity, i.CountedQuantity!.Value, i.CountedQuantity.Value - i.ExpectedQuantity))
            .ToListAsync(ct);

        return new(ToRowDto(header), items);
    }

    private sealed class VarianceRow
    {
        public Guid Id { get; init; }
        public int SequenceNumber { get; init; }
        public Guid StoreId { get; init; }
        public string StoreName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset? CompletionDate { get; init; }
        public DateTimeOffset? ApprovedDate { get; init; }
        public int CountedItems { get; init; }
        public int Shrinkage { get; init; }
        public int Overage { get; init; }
    }

    private static StockTakeVarianceRowDto ToRowDto(VarianceRow r) =>
        new(r.Id, $"ST-{r.SequenceNumber:D4}", r.StoreId, r.StoreName, r.Status,
            r.CompletionDate, r.ApprovedDate, r.CountedItems, r.Shrinkage, r.Overage);

    private IOrderedQueryable<VarianceRow> VarianceRows(StockTakeVarianceFilter filter, Guid? id)
    {
        var query = db.StockTakes.Where(s => s.Status == StockTakeStatus.Approved || s.Status == StockTakeStatus.Completed);

        if (id is not null)
            query = query.Where(s => s.Id == id);
        if (filter.StoreId is not null)
            query = query.Where(s => s.StoreId == filter.StoreId);
        if (NzTime.StartOfDay(filter.From) is { } start)
            query = query.Where(s => s.CompletionDate >= start);
        if (NzTime.EndOfDay(filter.To) is { } end)
            query = query.Where(s => s.CompletionDate < end);

        return query
            .Select(s => new VarianceRow
            {
                Id = s.Id,
                SequenceNumber = s.SequenceNumber,
                StoreId = s.StoreId,
                StoreName = s.Store!.Name,
                Status = s.Status,
                CompletionDate = s.CompletionDate,
                ApprovedDate = s.ApprovedDate,
                CountedItems = s.Items.Count(i => i.CountedQuantity != null),
                Shrinkage = s.Items
                    .Where(i => i.CountedQuantity != null && i.CountedQuantity < i.ExpectedQuantity)
                    .Sum(i => i.CountedQuantity!.Value - i.ExpectedQuantity),
                Overage = s.Items
                    .Where(i => i.CountedQuantity != null && i.CountedQuantity > i.ExpectedQuantity)
                    .Sum(i => i.CountedQuantity!.Value - i.ExpectedQuantity)
            })
            .OrderByDescending(r => r.CompletionDate).ThenBy(r => r.Id);
    }
}
