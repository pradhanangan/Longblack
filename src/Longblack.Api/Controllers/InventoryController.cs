using Longblack.Api.Models.Inventory;
using Longblack.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class InventoryController(IInventoryService inventoryService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var filter = new ListInventoryFilter(q, brandId, categoryId, status);
        var inventory = await inventoryService.ListAsync(filter, ct);
        return Ok(inventory.Select(ToResponse));
    }

    [HttpGet("{productVariantId:guid}")]
    public async Task<IActionResult> GetByVariantId(Guid productVariantId, CancellationToken ct)
    {
        var inventory = await inventoryService.GetByVariantIdAsync(productVariantId, ct);
        return inventory is null ? NotFound() : Ok(ToResponse(inventory));
    }

    [HttpGet("{productVariantId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid productVariantId, CancellationToken ct)
    {
        var transactions = await inventoryService.GetTransactionsAsync(productVariantId, ct);
        return Ok(transactions.Select(t => new InventoryTransactionResponse(
            t.Id, t.ProductVariantId, t.Type, t.QuantityDelta, t.SourceType, t.SourceId, t.CreatedAt, t.CreatedBy)));
    }

    private static InventoryResponse ToResponse(InventoryDto dto) =>
        new(dto.ProductVariantId, dto.Sku, dto.Barcode, dto.ProductName,
            dto.ColourName, dto.SizeName, dto.Status, dto.Quantity, dto.UpdatedAt);
}
