using Longblack.Api.Models.StockTake;
using Longblack.Application.StockTake;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/stock-takes")]
public class StockTakesController(IStockTakeService stockTakeService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var filter = new ListStockTakesFilter(status);
        var stockTakes = await stockTakeService.ListAsync(filter, ct);
        return Ok(stockTakes.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var stockTake = await stockTakeService.GetByIdAsync(id, ct);
        return stockTake is null ? NotFound() : Ok(ToResponse(stockTake));
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStockTakeRequest request, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.CreateAsync(
                new CreateStockTakeDto(request.BrandId, request.CategoryId), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = stockTake.Id }, ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.StartAsync(id, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.RemoveItemAsync(id, itemId, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/counts")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> RecordCount(Guid id, Guid itemId, [FromBody] RecordCountRequest request, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.RecordCountAsync(
                id, itemId, new RecordCountDto(request.Quantity), CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:guid}/items/{itemId:guid}/counts")]
    public async Task<IActionResult> GetCounts(Guid id, Guid itemId, CancellationToken ct)
    {
        try
        {
            var counts = await stockTakeService.GetCountsAsync(id, itemId, ct);
            return Ok(counts.Select(c => new StockTakeCountResponse(c.Id, c.StockTakeItemId, c.Quantity, c.CountedAt, c.CountedBy)));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.CompleteAsync(id, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.ReopenAsync(id, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.ApproveAsync(id, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            var stockTake = await stockTakeService.CancelAsync(id, CurrentUser, ct);
            return Ok(ToResponse(stockTake));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static StockTakeResponse ToResponse(StockTakeDto dto) =>
        new(dto.Id, dto.ReferenceNumber, dto.BrandId, dto.BrandName, dto.CategoryId, dto.CategoryName,
            dto.Status, dto.StartDate, dto.CompletionDate, dto.CompletedBy, dto.ApprovedDate, dto.ApprovedBy,
            dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.Items.Select(i => new StockTakeItemResponse(
                i.Id, i.StockTakeId, i.ProductVariantId, i.Sku, i.Barcode, i.ProductName, i.ColourName, i.SizeName,
                i.ExpectedQuantity, i.CountedQuantity, i.Variance, i.Status)).ToList());
}
