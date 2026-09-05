using Longblack.Api.Models.Receiving;
using Longblack.Application.Receiving.GoodsReceipts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/goods-receipts")]
public class GoodsReceiptsController(IGoodsReceiptService goodsReceiptService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? supplierCode,
        CancellationToken ct)
    {
        var filter = new ListGoodsReceiptsFilter(status, supplierCode);
        var receipts = await goodsReceiptService.ListAsync(filter, ct);
        return Ok(receipts.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var receipt = await goodsReceiptService.GetByIdAsync(id, ct);
        return receipt is null ? NotFound() : Ok(ToResponse(receipt));
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptRequest request, CancellationToken ct)
    {
        try
        {
            var receipt = await goodsReceiptService.CreateAsync(
                new CreateGoodsReceiptDto(request.SupplierCode, request.ReceivedDate),
                CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, ToResponse(receipt));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoodsReceiptRequest request, CancellationToken ct)
    {
        try
        {
            var receipt = await goodsReceiptService.UpdateAsync(
                id, new UpdateGoodsReceiptDto(request.SupplierCode, request.ReceivedDate),
                CurrentUser, ct);
            return Ok(ToResponse(receipt));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddGoodsReceiptLineRequest request, CancellationToken ct)
    {
        try
        {
            var receipt = await goodsReceiptService.AddLineAsync(
                id, new AddGoodsReceiptLineDto(request.ProductVariantId, request.Quantity, request.UnitCost),
                CurrentUser, ct);
            return Ok(ToResponse(receipt));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, [FromBody] UpdateGoodsReceiptLineRequest request, CancellationToken ct)
    {
        try
        {
            var receipt = await goodsReceiptService.UpdateLineAsync(
                id, lineId, new UpdateGoodsReceiptLineDto(request.Quantity, request.UnitCost),
                CurrentUser, ct);
            return Ok(ToResponse(receipt));
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
            var receipt = await goodsReceiptService.RemoveLineAsync(id, lineId, CurrentUser, ct);
            return Ok(ToResponse(receipt));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> MarkReceived(Guid id, CancellationToken ct)
    {
        try
        {
            var receipt = await goodsReceiptService.MarkReceivedAsync(id, CurrentUser, ct);
            return Ok(ToResponse(receipt));
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
            var receipt = await goodsReceiptService.CancelAsync(id, CurrentUser, ct);
            return Ok(ToResponse(receipt));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static GoodsReceiptResponse ToResponse(GoodsReceiptDto dto) =>
        new(dto.Id, dto.ReceiptNumber, dto.SupplierCode, dto.ReceivedDate,
            dto.Status, dto.ReceivedBy, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.Lines.Select(l => new GoodsReceiptLineResponse(
                l.Id, l.GoodsReceiptId, l.ProductVariantId, l.Sku, l.Barcode,
                l.Quantity, l.UnitCost, l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy)).ToList());
}
