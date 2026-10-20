using Longblack.Api.Models.Sales;
using Longblack.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/sales")]
public class SalesController(ISaleService saleService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] Guid? storeId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var sales = await saleService.ListAsync(new ListSalesFilter(status, storeId, from, to), ct);
        return Ok(sales.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sale = await saleService.GetByIdAsync(id, ct);
        return sale is null ? NotFound() : Ok(ToResponse(sale));
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.CreateAsync(new CreateSaleDto(request.StoreId), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddSaleLineRequest request, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.AddLineAsync(
                id, new AddSaleLineDto(request.ProductVariantId, request.Quantity, request.Discount), CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, [FromBody] UpdateSaleLineRequest request, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.UpdateLineAsync(
                id, lineId, new UpdateSaleLineDto(request.Quantity, request.Discount), CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.RemoveLineAsync(id, lineId, CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteSaleRequest request, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.CompleteAsync(
                id, new CompleteSaleDto(request.PaymentType, request.PaymentAmount), CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.VoidAsync(id, CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            var sale = await saleService.CancelAsync(id, CurrentUser, ct);
            return Ok(ToResponse(sale));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static SaleResponse ToResponse(SaleDto dto) =>
        new(dto.Id, dto.SaleNumber, dto.StoreId, dto.StoreName, dto.Status,
            dto.PaymentType, dto.PaymentAmount, dto.Total,
            dto.CompletedDate, dto.CompletedBy, dto.VoidedDate, dto.VoidedBy,
            dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.Lines.Select(l => new SaleLineResponse(
                l.Id, l.SaleId, l.ProductVariantId, l.Sku, l.Barcode, l.ProductName,
                l.Quantity, l.UnitPrice, l.Discount, l.LineTotal,
                l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy)).ToList());
}
