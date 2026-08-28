using Longblack.Application.Catalogue.ProductVariants;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/products/{productId:guid}/variants")]
public class ProductVariantsController(IProductVariantService variantService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid productId, [FromQuery] string? status, CancellationToken ct)
    {
        var variants = await variantService.ListAsync(productId, status, ct);
        return Ok(variants.Select(ToResponse));
    }

    [HttpGet("{variantId:guid}")]
    public async Task<IActionResult> GetById(Guid productId, Guid variantId, CancellationToken ct)
    {
        var variant = await variantService.GetByIdAsync(productId, variantId, ct);
        return variant is null ? NotFound() : Ok(ToResponse(variant));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductVariantRequest request, CancellationToken ct)
    {
        try
        {
            var variant = await variantService.CreateAsync(
                productId,
                new CreateProductVariantDto(request.Sku, request.Barcode, request.ColourId, request.SizeId, request.SellingPrice),
                CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { productId, variantId = variant.Id }, ToResponse(variant));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{variantId:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Update(Guid productId, Guid variantId, [FromBody] UpdateProductVariantRequest request, CancellationToken ct)
    {
        try
        {
            var variant = await variantService.UpdateAsync(
                productId, variantId,
                new UpdateProductVariantDto(request.Barcode, request.ColourId, request.SizeId, request.SellingPrice),
                CurrentUser, ct);
            return Ok(ToResponse(variant));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPatch("{variantId:guid}/status")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> SetStatus(Guid productId, Guid variantId, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        try
        {
            var variant = await variantService.SetStatusAsync(productId, variantId, request.Status, CurrentUser, ct);
            return Ok(ToResponse(variant));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static ProductVariantResponse ToResponse(ProductVariantDto dto) =>
        new(dto.Id, dto.ProductId, dto.Sku, dto.Barcode,
            dto.ColourId, dto.ColourName, dto.SizeId, dto.SizeName,
            dto.SellingPrice, dto.Status,
            dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
