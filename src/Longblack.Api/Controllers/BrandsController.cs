using Longblack.Application.Catalogue.Brands;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class BrandsController(IBrandService brandService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var brands = await brandService.ListAsync(status, ct);
        return Ok(brands.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var brand = await brandService.GetByIdAsync(id, ct);
        return brand is null ? NotFound() : Ok(ToResponse(brand));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request, CancellationToken ct)
    {
        try
        {
            var brand = await brandService.CreateAsync(new CreateBrandDto(request.Name, request.Code), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = brand.Id }, ToResponse(brand));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken ct)
    {
        try
        {
            var brand = await brandService.UpdateAsync(id, new UpdateBrandDto(request.Name, request.Code), CurrentUser, ct);
            return Ok(ToResponse(brand));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        try
        {
            var brand = await brandService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(brand));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static BrandResponse ToResponse(BrandDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
