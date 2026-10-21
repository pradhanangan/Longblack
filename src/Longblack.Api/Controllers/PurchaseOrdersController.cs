using Longblack.Api.Models.Purchasing;
using Longblack.Application.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService) : ApiControllerBase
{
    private const string Writers = "Manager,Admin";

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var orders = await purchaseOrderService.ListAsync(new ListPurchaseOrdersFilter(status, supplierId, from, to), ct);
        return Ok(orders.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await purchaseOrderService.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(ToResponse(order));
    }

    [HttpPost]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        try
        {
            var order = await purchaseOrderService.CreateAsync(
                new CreatePurchaseOrderDto(request.SupplierId, request.ExpectedDeliveryDate, request.Notes), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct) =>
        await Run(() => purchaseOrderService.UpdateAsync(
            id, new UpdatePurchaseOrderDto(request.SupplierId, request.ExpectedDeliveryDate, request.Notes), CurrentUser, ct));

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddPurchaseOrderLineRequest request, CancellationToken ct) =>
        await Run(() => purchaseOrderService.AddLineAsync(
            id, new AddPurchaseOrderLineDto(request.ProductVariantId, request.Quantity, request.UnitCost), CurrentUser, ct));

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, [FromBody] UpdatePurchaseOrderLineRequest request, CancellationToken ct) =>
        await Run(() => purchaseOrderService.UpdateLineAsync(
            id, lineId, new UpdatePurchaseOrderLineDto(request.Quantity, request.UnitCost), CurrentUser, ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct) =>
        await Run(() => purchaseOrderService.RemoveLineAsync(id, lineId, CurrentUser, ct));

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct) =>
        await Run(() => purchaseOrderService.IssueAsync(id, CurrentUser, ct));

    // Managers may cancel a Draft; only an Admin may cancel an Issued Purchase Order (enforced in the service).
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        await Run(() => purchaseOrderService.CancelAsync(id, CurrentUser, User.IsInRole("Admin"), ct));

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct) =>
        await Run(() => purchaseOrderService.CloseAsync(id, CurrentUser, ct));

    [HttpPost("{id:guid}/mark-sent")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> MarkSent(Guid id, CancellationToken ct) =>
        await Run(() => purchaseOrderService.MarkSentAsync(id, CurrentUser, ct));

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendPurchaseOrderEmailRequest request, CancellationToken ct) =>
        await Run(() => purchaseOrderService.SendEmailAsync(
            id, new SendPurchaseOrderEmailDto(request.Recipient, request.Cc), CurrentUser, ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        try
        {
            var pdf = await purchaseOrderService.GetPdfAsync(id, ct);
            return File(pdf.Content, "application/pdf", pdf.FileName);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private async Task<IActionResult> Run(Func<Task<PurchaseOrderDto>> action)
    {
        try
        {
            return Ok(ToResponse(await action()));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static PurchaseOrderResponse ToResponse(PurchaseOrderDto dto) =>
        new(dto.Id, dto.PoNumber, dto.SupplierId, dto.SupplierName, dto.SupplierEmail, dto.ExpectedDeliveryDate,
            dto.Notes, dto.Status, dto.IsOverdue, dto.IssuedAt, dto.IssuedBy, dto.SentAt, dto.SentBy, dto.Total,
            dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.Lines.Select(l => new PurchaseOrderLineResponse(
                l.Id, l.PurchaseOrderId, l.ProductVariantId, l.Sku, l.Barcode, l.Quantity, l.UnitCost,
                l.ReceivedQuantity, Math.Max(0, l.Quantity - l.ReceivedQuantity),
                l.CreatedAt, l.UpdatedAt, l.CreatedBy, l.UpdatedBy)).ToList(),
            dto.Sends.Select(s => new PurchaseOrderSendResponse(
                s.Id, s.Recipient, s.Cc, s.Succeeded, s.Error, s.SentAt, s.SentBy)).ToList(),
            dto.GoodsReceipts.Select(g => new LinkedGoodsReceiptResponse(
                g.Id, g.ReceiptNumber, g.Status, g.ReceivedDate)).ToList());
}
