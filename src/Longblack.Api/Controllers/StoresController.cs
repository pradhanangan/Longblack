using Longblack.Api.Models.Stores;
using Longblack.Application.Stores;
using Longblack.Application.Common.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class StoresController(IStoreService storeService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var paging = PageRequest.CreateOptional(page, pageSize);
        var stores = await storeService.ListAsync(status, paging, ct);
        if (paging is null)
            return Ok(stores.Items.Select(ToResponse));
        return Ok(new PagedResult<StoreResponse>(
            stores.Items.Select(ToResponse).ToList(), stores.Total, stores.Page, stores.PageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var store = await storeService.GetByIdAsync(id, ct);
        return store is null ? NotFound() : Ok(ToResponse(store));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken ct)
    {
        try
        {
            var store = await storeService.CreateAsync(new CreateStoreDto(request.Name, request.Type), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = store.Id }, ToResponse(store));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
    {
        try
        {
            var store = await storeService.UpdateAsync(id, new UpdateStoreDto(request.Name), CurrentUser, ct);
            return Ok(ToResponse(store));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStoreStatusRequest request, CancellationToken ct)
    {
        try
        {
            var store = await storeService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(store));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static StoreResponse ToResponse(StoreDto dto) =>
        new(dto.Id, dto.Name, dto.Type, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
