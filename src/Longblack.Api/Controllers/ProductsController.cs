using Longblack.Application.Catalogue.ProductImages;
using Longblack.Application.Catalogue.Products;
using Longblack.Application.Common.Paging;
using Longblack.Api.Models.Catalogue;
using Longblack.Infrastructure.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class ProductsController(IProductService productService, IProductImageService imageService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var filter = new ListProductsFilter(brandId, categoryId, status, q, PageRequest.Create(page, pageSize));
        var result = await productService.ListAsync(filter, ct);
        return Ok(new PagedResult<ProductResponse>(
            result.Items.Select(ToResponse).ToList(), result.Total, result.Page, result.PageSize));
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

    [HttpGet("{id:guid}/images")]
    public async Task<IActionResult> ListImages(Guid id, CancellationToken ct) =>
        Ok(await imageService.ListAsync(id, ct));

    [HttpPut("{id:guid}/image")]
    [Authorize(Roles = "Manager,Admin")]
    [RequestSizeLimit(ProductImageService.MaxUploadBytes + 1024 * 1024)]
    public Task<IActionResult> SetImage(Guid id, IFormFile file, CancellationToken ct) =>
        SetImageCore(id, null, file, ct);

    [HttpPut("{id:guid}/colours/{colourId:guid}/image")]
    [Authorize(Roles = "Manager,Admin")]
    [RequestSizeLimit(ProductImageService.MaxUploadBytes + 1024 * 1024)]
    public Task<IActionResult> SetColourImage(Guid id, Guid colourId, IFormFile file, CancellationToken ct) =>
        SetImageCore(id, colourId, file, ct);

    [HttpDelete("{id:guid}/image")]
    [Authorize(Roles = "Manager,Admin")]
    public Task<IActionResult> DeleteImage(Guid id, CancellationToken ct) =>
        DeleteImageCore(id, null, ct);

    [HttpDelete("{id:guid}/colours/{colourId:guid}/image")]
    [Authorize(Roles = "Manager,Admin")]
    public Task<IActionResult> DeleteColourImage(Guid id, Guid colourId, CancellationToken ct) =>
        DeleteImageCore(id, colourId, ct);

    private async Task<IActionResult> SetImageCore(Guid id, Guid? colourId, IFormFile file, CancellationToken ct)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await imageService.SetAsync(id, colourId, stream, CurrentUser, ct));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private async Task<IActionResult> DeleteImageCore(Guid id, Guid? colourId, CancellationToken ct)
    {
        try
        {
            await imageService.DeleteAsync(id, colourId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static ProductResponse ToResponse(ProductDto dto) =>
        new(dto.Id, dto.ProductCode, dto.Name, dto.Description,
            dto.BrandId, dto.BrandName, dto.CategoryId, dto.CategoryName,
            dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy,
            dto.ImageUrl, dto.ThumbnailUrl)
        {
            Variants = dto.Variants?.Select(v => new ProductVariantResponse(
                v.Id, v.ProductId, v.Sku, v.Barcode,
                v.ColourId, v.ColourName, v.SizeId, v.SizeName,
                v.SellingPrice, v.Status, v.CreatedAt, v.UpdatedAt, v.CreatedBy, v.UpdatedBy,
                v.ImageUrl, v.ThumbnailUrl)).ToList()
        };
}
