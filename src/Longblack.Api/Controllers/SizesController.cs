using Longblack.Application.Catalogue.Sizes;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class SizesController(ISizeService sizeService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var sizes = await sizeService.ListAsync(status, ct);
        return Ok(sizes.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var size = await sizeService.GetByIdAsync(id, ct);
        return size is null ? NotFound() : Ok(ToResponse(size));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSizeRequest request, CancellationToken ct)
    {
        try
        {
            var size = await sizeService.CreateAsync(new CreateSizeDto(request.Name, request.Code, request.SortOrder), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = size.Id }, ToResponse(size));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSizeRequest request, CancellationToken ct)
    {
        try
        {
            var size = await sizeService.UpdateAsync(id, new UpdateSizeDto(request.Name, request.Code, request.SortOrder), CurrentUser, ct);
            return Ok(ToResponse(size));
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
            var size = await sizeService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(size));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static SizeResponse ToResponse(SizeDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.SortOrder, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
