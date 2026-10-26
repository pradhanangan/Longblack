using Longblack.Api.Models.Inventory;
using Longblack.Application.Inventory;
using Longblack.Application.Common.Paging;
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
        [FromQuery] Guid? storeId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var filter = new ListInventoryFilter(q, brandId, categoryId, status, storeId);
        var paging = PageRequest.CreateOptional(page, pageSize);
        var inventory = await inventoryService.ListAsync(filter, paging, ct);
        if (paging is null)
            return Ok(inventory.Items.Select(ToResponse));
        return Ok(new PagedResult<InventoryResponse>(
            inventory.Items.Select(ToResponse).ToList(), inventory.Total, inventory.Page, inventory.PageSize));
    }

    [HttpGet("{productVariantId:guid}")]
    public async Task<IActionResult> GetByVariantId(Guid productVariantId, [FromQuery] Guid? storeId, CancellationToken ct)
    {
        var inventory = await inventoryService.GetByVariantIdAsync(productVariantId, storeId, ct);
        return inventory is null ? NotFound() : Ok(ToResponse(inventory));
    }

    [HttpGet("{productVariantId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid productVariantId, CancellationToken ct)
    {
        var transactions = await inventoryService.GetTransactionsAsync(productVariantId, ct);
        return Ok(transactions.Select(t => new InventoryTransactionResponse(
            t.Id, t.ProductVariantId, t.StoreId, t.StoreName, t.Type, t.QuantityDelta, t.SourceType, t.SourceId, t.CreatedAt, t.CreatedBy)));
    }

    private static InventoryResponse ToResponse(InventoryDto dto) =>
        new(dto.ProductVariantId, dto.Sku, dto.Barcode, dto.ProductName,
            dto.ColourName, dto.SizeName, dto.Status, dto.StoreId, dto.StoreName, dto.Quantity, dto.UpdatedAt);
}
