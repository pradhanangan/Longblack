using Longblack.Application.Catalogue.Colours;
using Longblack.Api.Models.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class ColoursController(IColourService colourService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var colours = await colourService.ListAsync(status, ct);
        return Ok(colours.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var colour = await colourService.GetByIdAsync(id, ct);
        return colour is null ? NotFound() : Ok(ToResponse(colour));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateColourRequest request, CancellationToken ct)
    {
        try
        {
            var colour = await colourService.CreateAsync(new CreateColourDto(request.Name, request.Code), CurrentUser, ct);
            return CreatedAtAction(nameof(GetById), new { id = colour.Id }, ToResponse(colour));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateColourRequest request, CancellationToken ct)
    {
        try
        {
            var colour = await colourService.UpdateAsync(id, new UpdateColourDto(request.Name, request.Code), CurrentUser, ct);
            return Ok(ToResponse(colour));
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
            var colour = await colourService.SetStatusAsync(id, request.Status, CurrentUser, ct);
            return Ok(ToResponse(colour));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private static ColourResponse ToResponse(ColourDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.CreatedBy, dto.UpdatedBy);
}
