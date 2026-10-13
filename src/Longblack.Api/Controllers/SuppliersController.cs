using Longblack.Application.Catalogue.Suppliers;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class SuppliersController(ISupplierService supplierService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var suppliers = await supplierService.ListAsync(status, ct);
        return Ok(suppliers.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var supplier = await supplierService.GetByIdAsync(id, ct);
        return supplier is null ? NotFound() : Ok(ToResponse(supplier));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        try
        {
            var supplier = await supplierService.CreateAsync(
                new CreateSupplierDto(request.Code, request.Name, request.Email, request.Phone), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, ToResponse(supplier));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        try
        {
            var supplier = await supplierService.UpdateAsync(
                id, new UpdateSupplierDto(request.Code, request.Name, request.Email, request.Phone), CurrentUser, ct);
            return Ok(ToResponse(supplier));
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
            var supplier = await supplierService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(supplier));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static SupplierResponse ToResponse(SupplierDto dto) =>
        new(dto.Id, dto.Code, dto.Name, dto.Email, dto.Phone, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
