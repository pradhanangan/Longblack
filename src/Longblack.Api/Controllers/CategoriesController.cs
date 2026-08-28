using Longblack.Application.Catalogue.Categories;
using Longblack.Application.Common.Exceptions;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class CategoriesController(ICategoryService categoryService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var categories = await categoryService.ListAsync(status, ct);
        return Ok(categories.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var category = await categoryService.GetByIdAsync(id, ct);
        return category is null ? NotFound() : Ok(ToResponse(category));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await categoryService.CreateAsync(
                new CreateCategoryDto(request.Name, request.ParentCategoryId), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await categoryService.UpdateAsync(
                id, new UpdateCategoryDto(request.Name, request.ParentCategoryId), CurrentUser, ct);
            return Ok(ToResponse(category));
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
            var category = await categoryService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(category));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static CategoryResponse ToResponse(CategoryDto dto) =>
        new(dto.Id, dto.ParentCategoryId, dto.Name, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
