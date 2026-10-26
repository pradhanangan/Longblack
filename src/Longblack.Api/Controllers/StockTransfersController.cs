using Longblack.Api.Models.StockTransfers;
using Longblack.Application.StockTransfers;
using Longblack.Application.Common.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/stock-transfers")]
public class StockTransfersController(IStockTransferService stockTransferService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] Guid? sourceStoreId,
        [FromQuery] Guid? destinationStoreId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var filter = new ListStockTransfersFilter(status, sourceStoreId, destinationStoreId);
        var paging = PageRequest.CreateOptional(page, pageSize);
        var transfers = await stockTransferService.ListAsync(filter, paging, ct);
        if (paging is null)
            return Ok(transfers.Items.Select(ToResponse));
        return Ok(new PagedResult<StockTransferResponse>(
            transfers.Items.Select(ToResponse).ToList(), transfers.Total, transfers.Page, transfers.PageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var transfer = await stockTransferService.GetByIdAsync(id, ct);
        return transfer is null ? NotFound() : Ok(ToResponse(transfer));
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        try
        {
            var transfer = await stockTransferService.CreateAsync(
                new CreateStockTransferDto(request.SourceStoreId, request.DestinationStoreId), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = transfer.Id }, ToResponse(transfer));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddStockTransferLineRequest request, CancellationToken ct)
    {
        try
        {
            var transfer = await stockTransferService.AddLineAsync(
                id, new AddStockTransferLineDto(request.ProductVariantId, request.Quantity), CurrentUser, ct);
            return Ok(ToResponse(transfer));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Staff,Manager,Admin")]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, [FromBody] UpdateStockTransferLineRequest request, CancellationToken ct)
    {
        try
        {
            var transfer = await stockTransferService.UpdateLineAsync(
                id, lineId, new UpdateStockTransferLineDto(request.Quantity), CurrentUser, ct);
            return Ok(ToResponse(transfer));
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
            var transfer = await stockTransferService.RemoveLineAsync(id, lineId, CurrentUser, ct);
            return Ok(ToResponse(transfer));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Dispatch(Guid id, CancellationToken ct)
    {
        try
        {
            var transfer = await stockTransferService.DispatchAsync(id, CurrentUser, ct);
            return Ok(ToResponse(transfer));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceiveStockTransferRequest request, CancellationToken ct)
    {
        try
        {
            var dto = new ReceiveStockTransferDto(
                request.Lines.Select(l => new ReceiveStockTransferLineDto(l.LineId, l.ReceivedQuantity)).ToList());
            var transfer = await stockTransferService.ReceiveAsync(id, dto, CurrentUser, ct);
            return Ok(ToResponse(transfer));
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
            var transfer = await stockTransferService.CancelAsync(id, CurrentUser, ct);
            return Ok(ToResponse(transfer));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static StockTransferResponse ToResponse(StockTransferDto dto) =>
        new(dto.Id, dto.TransferNumber, dto.SourceStoreId, dto.SourceStoreName,
            dto.DestinationStoreId, dto.DestinationStoreName, dto.Status,
            dto.DispatchedDate, dto.DispatchedBy, dto.ReceivedDate, dto.ReceivedBy,
            dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.Lines.Select(l => new StockTransferLineResponse(
                l.Id, l.StockTransferId, l.ProductVariantId, l.Sku, l.Barcode, l.ProductName,
                l.DispatchedQuantity, l.ReceivedQuantity, l.Variance,
                l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy)).ToList());
}
