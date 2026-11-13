using System.Text;
using Longblack.Application.Common.Paging;
using Longblack.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ReportsController(IReportService reportService) : ApiControllerBase
{
    [HttpGet("stock-on-hand")]
    public async Task<IActionResult> StockOnHand(
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool lowStockOnly,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var report = await reportService.GetStockOnHandAsync(
            new StockOnHandFilter(storeId, brandId, categoryId, lowStockOnly), PageRequest.Create(page, pageSize), ct);
        return Ok(report);
    }

    [HttpGet("stock-on-hand/export")]
    public async Task<IActionResult> ExportStockOnHand(
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool lowStockOnly,
        CancellationToken ct)
    {
        var report = await reportService.GetStockOnHandAsync(
            new StockOnHandFilter(storeId, brandId, categoryId, lowStockOnly), null, ct);

        var csv = new StringBuilder();
        AppendRow(csv, "SKU", "Product", "Colour", "Size", "Store", "Quantity", "Low Stock");
        foreach (var r in report.Rows.Items)
            AppendRow(csv, r.Sku, r.ProductName, r.ColourName, r.SizeName, r.StoreName, r.Quantity.ToString(), r.IsLowStock ? "Yes" : "No");

        return CsvFile(csv, "stock-on-hand");
    }

    [HttpGet("stock-movement")]
    public async Task<IActionResult> StockMovement(
        [FromQuery] Guid? storeId, [FromQuery] string? q, [FromQuery] string? type, [FromQuery] string? user,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        Ok(await reportService.GetStockMovementAsync(
            new StockMovementFilter(storeId, q, type, user, from, to), PageRequest.Create(page, pageSize), ct));

    [HttpGet("stock-movement/export")]
    public async Task<IActionResult> ExportStockMovement(
        [FromQuery] Guid? storeId, [FromQuery] string? q, [FromQuery] string? type, [FromQuery] string? user,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await reportService.GetStockMovementAsync(
            new StockMovementFilter(storeId, q, type, user, from, to), null, ct);

        var csv = new StringBuilder();
        AppendRow(csv, "Date (NZ)", "SKU", "Product", "Colour", "Size", "Store", "Type", "Quantity Change", "User");
        foreach (var r in result.Items)
            AppendRow(csv, NzTime.ToLocal(r.CreatedAt).ToString("yyyy-MM-dd HH:mm"), r.Sku, r.ProductName, r.ColourName,
                r.SizeName, r.StoreName, r.Type, r.QuantityDelta.ToString(), r.CreatedBy);

        return CsvFile(csv, "stock-movement");
    }

    [HttpGet("sales-summary")]
    public async Task<IActionResult> SalesSummary(
        [FromQuery] Guid? storeId, [FromQuery] Guid? brandId, [FromQuery] Guid? categoryId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? groupBy, CancellationToken ct)
    {
        var group = (groupBy ?? SalesSummaryGroupBy.Day).ToLowerInvariant();
        if (!SalesSummaryGroupBy.All.Contains(group))
            return BadRequest(new { message = $"groupBy must be one of: {string.Join(", ", SalesSummaryGroupBy.All)}." });

        return Ok(await reportService.GetSalesSummaryAsync(
            new SalesSummaryFilter(storeId, brandId, categoryId, from, to, group), ct));
    }

    [HttpGet("sales-summary/export")]
    public async Task<IActionResult> ExportSalesSummary(
        [FromQuery] Guid? storeId, [FromQuery] Guid? brandId, [FromQuery] Guid? categoryId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? groupBy, CancellationToken ct)
    {
        var group = (groupBy ?? SalesSummaryGroupBy.Day).ToLowerInvariant();
        if (!SalesSummaryGroupBy.All.Contains(group))
            return BadRequest(new { message = $"groupBy must be one of: {string.Join(", ", SalesSummaryGroupBy.All)}." });

        var summary = await reportService.GetSalesSummaryAsync(
            new SalesSummaryFilter(storeId, brandId, categoryId, from, to, group), ct);

        var csv = new StringBuilder();
        AppendRow(csv, group, "Sales", "Units", "Total (incl GST)", "GST", "Subtotal (excl GST)");
        foreach (var r in summary.Rows)
            AppendRow(csv, r.Label, r.SaleCount.ToString(), r.Units.ToString(), Money(r.Total), Money(r.Gst), Money(r.ExGst));
        var t = summary.Totals;
        AppendRow(csv, "Total", t.SaleCount.ToString(), t.Units.ToString(), Money(t.Total), Money(t.Gst), Money(t.ExGst));
        AppendRow(csv, "Voided (excluded from total)", summary.Voided.SaleCount.ToString(), "", Money(summary.Voided.Total), "", "");

        return CsvFile(csv, "sales-summary");
    }

    [HttpGet("stock-take-variance")]
    public async Task<IActionResult> StockTakeVariance(
        [FromQuery] Guid? storeId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct) =>
        Ok(await reportService.GetStockTakeVarianceAsync(
            new StockTakeVarianceFilter(storeId, from, to), PageRequest.Create(page, pageSize), ct));

    [HttpGet("stock-take-variance/export")]
    public async Task<IActionResult> ExportStockTakeVariance(
        [FromQuery] Guid? storeId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await reportService.GetStockTakeVarianceAsync(new StockTakeVarianceFilter(storeId, from, to), null, ct);

        var csv = new StringBuilder();
        AppendRow(csv, "Stock Take", "Store", "Status", "Completed (NZ)", "Counted Items", "Shrinkage", "Overage", "Net");
        foreach (var r in result.Items)
            AppendRow(csv, r.ReferenceNumber, r.StoreName, StatusLabel(r.Status),
                r.CompletionDate is { } d ? NzTime.ToLocal(d).ToString("yyyy-MM-dd HH:mm") : "",
                r.CountedItems.ToString(), r.Shrinkage.ToString(), r.Overage.ToString(), (r.Shrinkage + r.Overage).ToString());

        return CsvFile(csv, "stock-take-variance");
    }

    [HttpGet("stock-take-variance/{id:guid}")]
    public async Task<IActionResult> StockTakeVarianceDetail(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await reportService.GetStockTakeVarianceDetailAsync(id, ct));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("stock-take-variance/{id:guid}/export")]
    public async Task<IActionResult> ExportStockTakeVarianceDetail(Guid id, CancellationToken ct)
    {
        try
        {
            var detail = await reportService.GetStockTakeVarianceDetailAsync(id, ct);
            var csv = new StringBuilder();
            AppendRow(csv, "SKU", "Product", "Colour", "Size", "Expected", "Counted", "Variance");
            foreach (var i in detail.Items)
                AppendRow(csv, i.Sku, i.ProductName, i.ColourName, i.SizeName,
                    i.ExpectedQuantity.ToString(), i.CountedQuantity.ToString(), i.Variance.ToString());
            return CsvFile(csv, $"stock-take-variance-{detail.StockTake.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static string Money(decimal value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static string StatusLabel(string status) => status == "Completed" ? "Completed (pending approval)" : status;

    private FileContentResult CsvFile(StringBuilder csv, string name) =>
        File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", $"{name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");

    private static void AppendRow(StringBuilder sb, params string?[] cells) =>
        sb.Append(string.Join(',', cells.Select(Escape))).Append("\r\n");

    // Quotes cells and neutralises leading formula characters so spreadsheets don't execute them.
    private static string Escape(string? value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]) && !decimal.TryParse(value, out _))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
