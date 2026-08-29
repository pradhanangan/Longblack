using Longblack.Application.Catalogue.Products;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class ProductsController(IProductService productService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct)
    {
        var filter = new ListProductsFilter(brandId, categoryId, status, q);
        var products = await productService.ListAsync(filter, ct);
        return Ok(products.Select(ToResponse));
    }

    [HttpGet("suggest-code")]
    public async Task<IActionResult> SuggestCode(
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
    {
        var code = await productService.SuggestCodeAsync(brandId, categoryId, ct);
        return Ok(new { suggestedCode = code });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await productService.CreateAsync(
                new CreateProductDto(request.ProductCode, request.Name, request.Description, request.BrandId, request.CategoryId),
                CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await productService.UpdateAsync(
                id, new UpdateProductDto(request.Name, request.Description, request.BrandId, request.CategoryId),
                CurrentUser, ct);
            return Ok(ToResponse(product));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        try
        {
            var product = await productService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(product));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static ProductResponse ToResponse(ProductDto dto) =>
        new(dto.Id, dto.ProductCode, dto.Name, dto.Description,
            dto.BrandId, dto.BrandName, dto.CategoryId, dto.CategoryName,
            dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
